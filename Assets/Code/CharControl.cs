using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharControl : MonoBehaviour
{

    public const float lookSpeed = 3f;
    private const float runAcceleration = 100f;
    private const float airAcceleration = 10f;
    private const float maxRunSpeed = 8f;
    private const float maxAirSpeed = 3f;
    private const float groundFriction = 10f;
    private const float stopspeed = 0.1f;
    private const float jumpHeight = 1f;
    private const float walkUpRampAngle = 30f;

    private Vector2 rotation = Vector2.zero;

    private Vector3 velocityDifference;
    private Vector3 friction;

    private Vector3 moveInput;

    private BoxCollider bCollider;
    private Rigidbody rb;
    private Camera playerCam;

    private bool grounded;
    private Collider groundCollider;
    private Vector3 rampNormal;
    private bool onRamp;

    private bool jump_pending;
    private bool jumping;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        bCollider = GetComponent<BoxCollider>();
        playerCam = Camera.main;
        jump_pending = false;
        jumping = false;
    }

    // Update is called once per frame
    void Update()
    {
        //first person view
        rotation.y += Input.GetAxis("Mouse X");
        rotation.x += -Input.GetAxis("Mouse Y");
        rotation.x = Mathf.Clamp(rotation.x, -20f, 20f);

        playerCam.transform.eulerAngles = new Vector3(rotation.x, rotation.y, 0) * lookSpeed;
        //transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
        //playerCam.transform.localRotation = Quaternion.Euler(rotation.x * lookSpeed, 0, 0);
        //Debug.Log("x: " + rotation.x + "    y: " + rotation.y);

        //movement
        Vector2 axisInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        axisInput = Vector2.ClampMagnitude(axisInput, 1);

        float rot = (360 - playerCam.transform.eulerAngles.y) * Mathf.Deg2Rad;
        axisInput = RotateVector2(axisInput, rot);

        moveInput = new Vector3(axisInput.x, 0, axisInput.y);

        //jump
        if (grounded && Input.GetButtonDown("Jump"))
        {
            jump_pending = true;
        }


        Debug.DrawRay(transform.position, rb.velocity, Color.green);
        //Debug.DrawRay(transform.position, moveInput * 10, Color.blue);
        Debug.DrawRay(transform.position, velocityDifference * 5, Color.yellow);
        Debug.DrawRay(transform.position, friction * 5, Color.red);
    }

    private void FixedUpdate()
    {
        if (grounded)
        {
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
        }
        if (jump_pending && grounded)
        {
            Jump();
            return;
        }

        Vector3 v_current = rb.velocity;
        Vector3 v_current_2D = Velocity2D();

        Vector3 v_desired = rb.velocity;

        if (grounded)
        {
            v_desired += Friction(v_current); //subtract friction from v_desired
            MoveGround(v_current, v_desired);
        }
        else
        {
            friction = Vector3.zero;
            MoveAir(v_current_2D);
        }
    }

    

    private Vector3 Friction(Vector3 v_current)
    {
        if (v_current.magnitude < stopspeed)
        {
            rb.velocity = Vector3.zero;
        }

        Vector3 frictionDirection = -(v_current.normalized);
        float friction_magnitude = Mathf.Max(groundFriction * v_current.magnitude, 10f);
        friction = frictionDirection * friction_magnitude * Time.fixedDeltaTime; //friction in reverse of current direction of movement
        
        return friction;
    }


    
    private void MoveGround(Vector3 v_current, Vector3 v_desired)
    {
        Vector3 movement = Vector3.zero;
        if (!onRamp)
        {
            movement = moveInput * runAcceleration * Time.fixedDeltaTime;
        }
        else
        {
            //projection of xz movement onto ramp plane
            Vector3 ramp_proj = Vector3.ProjectOnPlane(moveInput, rampNormal);
            ramp_proj = ramp_proj.normalized;
            movement = ramp_proj * runAcceleration * Time.fixedDeltaTime;
        }

        v_desired += movement;

        if (v_desired.magnitude > maxRunSpeed)
        {
            v_desired = Vector3.ClampMagnitude(v_desired, maxRunSpeed);
        }
        Move(v_current, v_desired);
    }

    private void MoveAir(Vector3 v_current_2D)
    {
        Vector3 v_desired = v_current_2D + moveInput * airAcceleration * Time.fixedDeltaTime;

        if (v_desired.magnitude > maxAirSpeed)
        {
            v_desired = Vector3.ClampMagnitude(v_desired, Mathf.Max(maxAirSpeed, v_current_2D.magnitude));
        }

        Move(v_current_2D, v_desired);
    }

    private void Move(Vector3 v_current, Vector3 v_desired)
    {
        velocityDifference = v_desired - v_current;
        rb.AddForce(velocityDifference, ForceMode.VelocityChange);
        //Debug.Log("desired: " + v_desired.magnitude + "    actual: " + rb.velocity.magnitude + "   grd?: " + grounded);
    }

    //player jumps
    private void Jump()
    {
        //Debug.Log("jumping");
        float jump_v_change = Mathf.Sqrt(-2.1f * Physics.gravity.y * jumpHeight) - Mathf.Clamp(rb.velocity.y, 0, 3f);
        rb.AddForce(Vector3.up * jump_v_change, ForceMode.VelocityChange);
        rb.useGravity = true;
        grounded = false;
        jump_pending = false;
        jumping = true;
    }

    //called when player comes in contact with another collider
    private void OnCollisionEnter(Collision collision)
    {
        UpdateGrounded(collision, 0);
        //Debug.Log("entered " + collision.collider.gameObject.name + ". grounded: " + grounded + "   groundcollider: " + groundCollider.gameObject.name);
    }

    //called when player remains in contact with another collider
    private void OnCollisionStay(Collision collision)
    {
        UpdateGrounded(collision, 1);
        //Debug.Log("still on " + collision.collider.gameObject.name + ". grounded: " + grounded + "   groundcollider: " + groundCollider.gameObject.name);
    }

    //called when player leaves contact with another collider
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == groundCollider)
        {
            grounded = false;
            groundCollider = null;
            onRamp = false;
            rampNormal = Vector3.zero;
        }
        //Debug.Log("left " + collision.collider.gameObject.name + ". grounded: " + grounded);
    }



    //checks if player can stand on collision surface
    //type: OnCollisionEnter = 0, OnCollisionStay = 1
    private void UpdateGrounded(Collision collision, int type)
    {
        
        if (type == 1 && groundCollider != null) //if OnCollisionStay and the player was standing on something
        {
            if (groundCollider != collision.collider) //if the contacted collider isn't the collider the player is standing on, ignore OnCollisionStay calls
            {
                Debug.Log(collision.collider.gameObject.name + " not ground collider, returned. grounded: " + grounded);
                return;
            }
            else if (jumping == true) //otherwise, if jumping and hasn't left the ground yet, ignore OnCollisionStay calls
            {
                Debug.Log("jumping, have not yet left " + collision.collider.gameObject.name + ", returned. grounded: " + grounded);
                return;
            }
        }
        
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);
        ContactPoint c1 = contacts[0];
        

        //check if there's ambiguity in the collision surface
        for (int i = 1; i < collision.contactCount; i++)
        {
            if (contacts[i].normal != c1.normal)
            {
                Debug.Log("contacts mismatch");
                return;
            }
        }

        //check if we're walking on the surface we collided with
        int surfaceCheck = SurfaceType(c1.normal);
        if (surfaceCheck == -1) //angle too steep
        {
            return;
        }
        else if (surfaceCheck == 0) //flat surface
        {
            onRamp = false;
        }
        else //walkeable angle
        {
            rampNormal = c1.normal;
            onRamp = true;
        }

        //haven't returned yet, so player is standing on something
        grounded = true;
        jumping = false;

        groundCollider = collision.collider;

        StickToSurface(c1);
    }



    //returns -1 if surface can't be stood on, 0 if flat, 1 if angled
    private int SurfaceType(Vector3 surfaceNormal)
    {
        if (surfaceNormal == Vector3.up)
        {
            return 0;
        }

        //projection of collision.normal onto xz plane
        Vector3 contact_normal_plane_proj = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);

        //if angle between contact.normal and xz plane < 45 (slope too steep to stand on), return
        float angle = Vector3.Angle(contact_normal_plane_proj, surfaceNormal);
        if (angle < 90f - walkUpRampAngle)
        {
            return -1;
        }
        else //Otherwise, player is standing on a ramp
        {
            return 1;
        }
        
    }

    //snaps player to the ground when walking
    private bool StickToSurface(ContactPoint c)
    {
        float separation = c.separation;
        if (separation > 0.000001f && separation < 0.02)
        {
            Vector3 direction = -c.normal;
            rb.MovePosition(rb.transform.position + direction * separation);
            Debug.Log("snapped, distance: " + separation);
            return true;
        }

        return false;
    }




    //rotates Vector2s to align movement input with direction player is facing
    public Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float newX = v.x * Mathf.Cos(degrees) - v.y * Mathf.Sin(degrees);
        float newY = v.y * Mathf.Cos(degrees) + v.x * Mathf.Sin(degrees);
        return new Vector2(newX, newY);
    }

    //gets velocity along the xz plane
    public Vector3 Velocity2D()
    {
        return new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }






    //
    //OLD FUNCTIONS NOT IN USE
    //

    private bool IsGroundedOld()
    {
        return Physics.Raycast(transform.position, Vector3.down, bCollider.bounds.extents.y + 0.1f);
    }


    private void FrictionOld(Vector2 velocity2D)
    {
        float speed = velocity2D.magnitude;
        if (speed < stopspeed)
        {
            rb.velocity = Vector3.zero;
        }

        float drop = speed * groundFriction * Time.fixedDeltaTime;

        float newspeed = speed - drop;

        if (newspeed < 0)
        {
            newspeed = 0;
        }

        if (newspeed != speed)
        {
            float proportion = newspeed / speed;
            rb.velocity = rb.velocity * proportion;
        }
    }

    private void OnCollisionEnterOld(Collision collision)
    {
        
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);
        foreach (ContactPoint contact in contacts)
        {
            //projection of collision.normal onto xz plane
            Vector3 contact_normal_plane_proj = Vector3.ProjectOnPlane(contact.normal, Vector3.up);

            //if angle between contact.normal and xz plane < 45, return
            float angle = Vector3.Angle(contact_normal_plane_proj, contact.normal);
            if (contact.normal != Vector3.up && angle < 45f)
            {
                return;
            }
        }
        //if player lands on something relatively flat, grounded = True
        grounded = true;
        groundCollider = collision.collider;
    }

    private void OnCollisionStayOld(Collision collision)
    {
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);
        foreach (ContactPoint contact in contacts)
        {
            //projection of collision.normal onto xz plane
            Vector3 contact_normal_plane_proj = Vector3.ProjectOnPlane(contact.normal, Vector3.up);

            //if angle between contact.normal and xz plane < 45, return
            float angle = Vector3.Angle(contact_normal_plane_proj, contact.normal);
            if (contact.normal != Vector3.up && angle < 45f)
            {
                return;
            }
        }
        //if player lands on something relatively flat, grounded = True
        grounded = true;
        groundCollider = collision.collider;
    }
}
