using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharControl_2 : MonoBehaviour
{

    private const float lookSpeed = 2f; //view sensitivity
    private const float runAcceleration = 100f; //acceleration when running on ground
    private const float airAcceleration = 20f; //acceleration in air
    private const float maxRunSpeed = 8f;
    private const float maxAirSpeed = 3f;
    private const float groundFriction = 10f;
    private const float stopspeed = 0.1f; //if moving on ground and v < stopspeed, velocity becomes 0
    private const float jumpHeight = 1.1f;
    private const float walkUpRampAngle = 30f; //max angle in degrees player can walk up
    private const float snapToGroundDistance = 0.1f; //snaps to ground if player < snapToGroundDistance away
    private const float groundedDistance = 0.1f; //if distance to ground < groundedDistance, player considered grounded
    private const float comeOffGroundSpeed = 4f; //vertical speed needed to become airborne

    private Vector2 lookRotation;

    private Vector3 velocityDifference;
    private Vector3 friction;

    private Vector3 moveInputDirection;

    private BoxCollider bCollider;
    private Rigidbody rb;
    private Camera playerCam;

    private bool grounded;
    private bool justJumped;

    // Start is called before the first frame update
    void Start()
    {
        //initialize default values, get necessary components
        lookRotation = Vector2.zero;
        velocityDifference = Vector3.zero;
        friction = Vector3.zero;
        moveInputDirection = Vector3.zero;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        bCollider = GetComponent<BoxCollider>();
        playerCam = Camera.main;

        grounded = false;
        justJumped = false;

        //set gravity to -20 units/s^2
        Physics.gravity = new Vector3(0, -20f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLookDirection();

        //movement input
        UpdateMovementInput();
        
        //jump
        if (grounded && Input.GetButtonDown("Jump"))
        {
            Jump();
        }
        
        //clicking on screen locks mouse to center, esc stops
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (Input.GetAxisRaw("Cancel") == 1)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        DrawDebugLines();
    }

    private void LateUpdate()
    {
        UpdateFirstPersonView();
    }

    //Called every time physics updates
    private void FixedUpdate()
    {
        CheckGrounded();

        if (grounded)
        {
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
        }

        if (justJumped) //wait a little after jumping
        {
            justJumped = false;
            return;
        }

        Vector3 v_current = rb.velocity; //current velocity
        Vector3 v_current_2D = Velocity2D(); //current velocity along xz plane

        if (grounded)
        {
            MoveGround(v_current);
            StickToSurface(); //allows player to move on/off ramps smoothly
        }
        else
        {
            friction = Vector3.zero;
            MoveAir(v_current_2D);
        }
        
    }

    private void UpdateLookDirection()
    {
        lookRotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        lookRotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        lookRotation.x = Mathf.Clamp(lookRotation.x, -90f, 90f);
    }


    //first person view
    private void UpdateFirstPersonView()
    {
        playerCam.transform.eulerAngles = new Vector3(lookRotation.x, lookRotation.y, 0);
    }

    //updates desired movement input
    private void UpdateMovementInput()
    {
        Vector2 axisInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        axisInput = Vector2.ClampMagnitude(axisInput, 1);

        float rot = (360 - playerCam.transform.eulerAngles.y) * Mathf.Deg2Rad;
        axisInput = RotateVector2(axisInput, rot);

        moveInputDirection = new Vector3(axisInput.x, 0, axisInput.y);
    }

    private void DrawDebugLines()
    {
        Debug.DrawRay(transform.position, rb.velocity, Color.green); //draw velocity vector
        Debug.DrawRay(transform.position, moveInputDirection * 10, Color.blue); //draw moveInput vector
        Debug.DrawRay(transform.position, friction * 5, Color.red); //draw friction vector
    }

    //checks if player is on a walkeable surface
    private void CheckGrounded()
    {
        //if moving up rapidly, not on ground
        if (rb.velocity.y > comeOffGroundSpeed)
        {
            grounded = false;
            return;
        }
        
        
        //ground detection box slightly narrower than player collider so player can't jump perfectly straight up to get on a ledge
        Vector3 boxCastExtents = new Vector3(bCollider.bounds.extents.x - 0.001f, groundedDistance, bCollider.bounds.extents.z - 0.001f);
        RaycastHit hit;
        RaycastHit hit2;
        //casts a box downward to detect ground
        if (Physics.BoxCast(bCollider.bounds.center, boxCastExtents, Vector3.down, out hit, Quaternion.identity, bCollider.bounds.extents.y) && 
            hit.collider.Raycast(new Ray(hit.point + Vector3.up, Vector3.down), out hit2, 1.1f))
        {
            //Debug.DrawRay(hit.point, hit.normal * 10f, Color.yellow, 0.1f);
            Debug.DrawRay(hit2.point, hit2.normal * 10f, Color.blue, 0.1f);

            if (hit2.normal == Vector3.up) //if on flat surface
            {
                //checks if contact point is near feet of player
                float playerBottom = bCollider.bounds.center.y - bCollider.bounds.extents.y;
                if (hit2.point.y - 0.003f <= playerBottom)
                {
                    grounded = true;
                }
            }
            else //if on a ramp
            {
                //projection of hit.normal onto xz plane
                Vector3 planeProjection = Vector3.ProjectOnPlane(hit2.normal, Vector3.up);
                
                //if angle between normal of ramp and xz plane not too steep, grounded = true
                float angle = Vector3.Angle(planeProjection, hit2.normal);
                if (angle > 90 - walkUpRampAngle - 0.5f)
                {
                    grounded = true;
                }
                else
                {
                    grounded = false;
                }
            }
        }
        else
        {
            //Debug.Log("boxcast miss");
            grounded = false;
        }
    }

    //returns friction vector given current velocity
    private Vector3 Friction(Vector3 v_current)
    {
        //if moving very slowly, set velocity to 0
        if (v_current.magnitude < stopspeed && v_current.magnitude != 0)
        {
            Debug.Log("stopped");
            rb.velocity = Vector3.zero;
        }

        //friction in reverse of current direction of movement, scaled to current speed
        Vector3 frictionDirection = -(v_current.normalized);
        float friction_magnitude = Mathf.Max(groundFriction * v_current.magnitude, 10f);
        friction = frictionDirection * friction_magnitude * Time.fixedDeltaTime;

        return friction;
    }


    //movement when grounded
    private void MoveGround(Vector3 v_current)
    {
        Vector3 movement = moveInputDirection * runAcceleration * Time.fixedDeltaTime; //movement acceleration = direction * acceleration scaled for time

        Vector3 v_desired = v_current + movement + Friction(v_current); //desired velocity = current velocity + movement acceleration + friction

        if (v_desired.magnitude > maxRunSpeed) //can't exceed max run speed
        {
            v_desired = Vector3.ClampMagnitude(v_desired, maxRunSpeed);
        }

        Move(v_current, v_desired);
    }

    //movement when in the air
    private void MoveAir(Vector3 v_current_2D)
    {
        Vector3 v_desired = v_current_2D + moveInputDirection * airAcceleration * Time.fixedDeltaTime;

        if (v_desired.magnitude > maxAirSpeed)
        {
            if (Vector3.Dot(v_current_2D, moveInputDirection) > 0)
            {
                return;
            }
            v_desired = Vector3.ClampMagnitude(v_desired, Mathf.Max(maxAirSpeed, v_current_2D.magnitude));
        }

        Move(v_current_2D, v_desired);
    }
    
    //applies velocity changes given current and desired velocity
    private void Move(Vector3 v_current, Vector3 v_desired)
    {
        velocityDifference = v_desired - v_current;
        rb.AddForce(velocityDifference, ForceMode.VelocityChange);
        //Debug.Log("v: " + rb.velocity.magnitude + "   grd?: " + grounded);
    }

    //player jumps
    private void Jump()
    {
        //Debug.Log("jumping");
        float jump_v_change = Mathf.Sqrt(-2.1f * Physics.gravity.y * jumpHeight) - Mathf.Clamp(rb.velocity.y, 0, 3f); //calculates velocity change needed to reach a designated jump height
        rb.AddForce(Vector3.up * jump_v_change, ForceMode.VelocityChange);
        rb.useGravity = true;
        grounded = false;
        justJumped = true;
    }

    
    //snaps player to the ground when walking
    private void StickToSurface()
    {
        RaycastHit hit;
        if (Physics.BoxCast(bCollider.bounds.center, bCollider.bounds.extents, Vector3.down, out hit, Quaternion.identity, snapToGroundDistance))
        {
            rb.MovePosition(rb.position + Vector3.down * hit.distance);
            Debug.Log("snapped");
        }
    }

    
    //rotates Vector2 to align movement input with direction player is facing
    public Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float newX = v.x * Mathf.Cos(degrees) - v.y * Mathf.Sin(degrees);
        float newY = v.y * Mathf.Cos(degrees) + v.x * Mathf.Sin(degrees);
        return new Vector2(newX, newY);
    }

    //returns velocity along the xz plane
    public Vector3 Velocity2D()
    {
        return new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }






    //
    //OLD FUNCTIONS NOT IN USE
    //
    
    
    ////checks if player can stand on collision surface
    ////type: OnCollisionEnter = 0, OnCollisionStay = 1
    //private void UpdateGrounded(Collision collision, int type)
    //{

    //    if (type == 1 && groundCollider != null) //if OnCollisionStay and the player was standing on something
    //    {
    //        if (groundCollider != collision.collider) //if the contacted collider isn't the collider the player is standing on, ignore OnCollisionStay calls
    //        {
    //            //Debug.Log(collision.collider.gameObject.name + " not ground collider, returned. grounded: " + grounded);
    //            return;
    //        }
    //        else if (jumping == true) //otherwise, if jumping and hasn't left the ground yet, ignore OnCollisionStay calls
    //        {
    //            //Debug.Log("jumping, have not yet left " + collision.collider.gameObject.name + ", returned. grounded: " + grounded);
    //            return;
    //        }
    //    }

    //    ContactPoint[] contacts = new ContactPoint[collision.contactCount];
    //    collision.GetContacts(contacts);
    //    ContactPoint c1 = contacts[0];


    //    //check if there's ambiguity in the collision surface
    //    for (int i = 1; i < collision.contactCount; i++)
    //    {
    //        if (contacts[i].normal != c1.normal)
    //        {
    //            Debug.Log("contacts mismatch");
    //            return;
    //        }
    //    }

    //    //check if we're walking on the surface we collided with
    //    int surfaceCheck = SurfaceType(c1.normal);
    //    if (surfaceCheck == -1) //angle too steep
    //    {
    //        return;
    //    }
    //    else if (surfaceCheck == 0) //flat surface
    //    {
    //        onRamp = false;
    //    }
    //    else //walkeable angle
    //    {
    //        rampNormal = c1.normal;
    //        onRamp = true;
    //    }

    //    //haven't returned yet, so player is standing on something
    //    grounded = true;
    //    jumping = false;

    //    groundCollider = collision.collider;
    //}



    ////returns -1 if surface can't be stood on, 0 if flat, 1 if angled
    //private int SurfaceType(Vector3 surfaceNormal)
    //{
    //    if (surfaceNormal == Vector3.up)
    //    {
    //        return 0;
    //    }

    //    //projection of collision.normal onto xz plane
    //    Vector3 contact_normal_plane_proj = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);

    //    //if angle between contact.normal and xz plane < 45 (slope too steep to stand on), return
    //    float angle = Vector3.Angle(contact_normal_plane_proj, surfaceNormal);
    //    if (angle < 90f - walkUpRampAngle)
    //    {
    //        return -1;
    //    }
    //    else //Otherwise, player is standing on a ramp
    //    {
    //        return 1;
    //    }

    //}


    //called when player comes in contact with another collider
    //private void OnCollisionEnter(Collision collision)
    //{
    //UpdateGrounded(collision, 0);
    //Debug.Log("entered " + collision.collider.gameObject.name + ". grounded: " + grounded + "   groundcollider: " + groundCollider.gameObject.name);
    //}

    //called when player remains in contact with another collider
    //private void OnCollisionStay(Collision collision)
    //{
    //UpdateGrounded(collision, 1);
    //Debug.Log("still on " + collision.collider.gameObject.name + ". grounded: " + grounded + "   groundcollider: " + groundCollider.gameObject.name);
    //}

    //called when player leaves contact with another collider
    //private void OnCollisionExit(Collision collision)
    //{
    //if (collision.collider == groundCollider)
    //{
    //    grounded = false;
    //    groundCollider = null;
    //    onRamp = false;
    //    rampNormal = Vector3.zero;
    //}
    //Debug.Log("left " + collision.collider.gameObject.name + ". grounded: " + grounded);
    //}
}
