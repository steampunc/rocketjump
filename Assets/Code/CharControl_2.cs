using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharControl_2 : MonoBehaviour
{

    private const float lookSpeed = 2f; //view sensitivity
    private const float runAcceleration = 100f; //acceleration when running on ground
    private const float airAcceleration = 40f; //acceleration in air
    private const float maxRunSpeed = 8f;
    private const float maxAirSpeed = 3f;
    private const float groundFriction = 10f;
    private const float stopspeed = 0.1f; //if moving on ground and v < stopspeed, velocity becomes 0
    private const float jumpHeight = 1.1f;
    private const float walkUpRampAngle = 35f; //max angle in degrees player can walk up
    //private const float snapToGroundDistance = 0.1f; //snaps to ground if player < snapToGroundDistance away
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
    private Vector3 groundNormal;

    private bool jumpPressed;


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
        groundNormal = Vector3.zero;
        jumpPressed = false;

        //set gravity to -20 units/s^2
        Physics.gravity = new Vector3(0, -20f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLookDirection();

        //movement input
        UpdateMovementInput();
        if (!jumpPressed && grounded)
        {
            //jump
            if (Input.GetButtonDown("Jump"))
            {
                jumpPressed = true;
            }
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
        if (jumpPressed) //wait a little after jumping
        {
            Jump();
            jumpPressed = false;
            //Debug.Log("done jumping");
            return;
        }

        CheckGrounded(bCollider.bounds.center);

        Vector3 v_current = rb.velocity; //current velocity
        Vector3 v_current_2D = Velocity2D(); //current velocity along xz plane

        if (grounded)
        {
            rb.useGravity = false;
            WalkMove(v_current);
        }
        else
        {
            rb.useGravity = true;
            friction = Vector3.zero;
            AirMove(v_current_2D);
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
        Vector3 axisInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        axisInput = Vector3.ClampMagnitude(axisInput, 1);

        float degreeRotation = playerCam.transform.eulerAngles.y - 360;
        axisInput = Quaternion.Euler(0, degreeRotation, 0) * axisInput;

        moveInputDirection = axisInput;
    }

    private void DrawDebugLines()
    {
        Debug.DrawRay(transform.position, rb.velocity, Color.green); //draw velocity vector
        //Debug.DrawRay(transform.position, moveInputDirection * 10, Color.blue); //draw moveInput vector
        //Debug.DrawRay(transform.position, friction * 5, Color.red); //draw friction vector
    }


    //movement when grounded
    private void WalkMove(Vector3 v_current)
    {
        Vector3 movement = moveInputDirection * runAcceleration * Time.fixedDeltaTime; //movement acceleration = direction * acceleration scaled for time

        Vector3 v_desired = v_current + movement + Friction(v_current); //desired velocity = current velocity + movement acceleration + friction

        if (v_desired.magnitude > maxRunSpeed) //can't exceed max run speed
        {
            v_desired = Vector3.ClampMagnitude(v_desired, maxRunSpeed);
        }
        
        //make movement direction parallel to surface player is standing on
        Vector3 v_desired_parallel = Vector3.ProjectOnPlane(v_desired, groundNormal).normalized * v_desired.magnitude;
        Debug.DrawRay(rb.position, groundNormal * 5, Color.red);

        Vector3 futurePosition = rb.position + v_desired_parallel * Time.fixedDeltaTime;
        Vector3 futureGroundNormal = CheckComingOffGround(futurePosition);
        //if moving onto a differently-sloped surface, change movement direction
        if (futureGroundNormal != Vector3.zero)
        {
            v_desired = Vector3.ProjectOnPlane(v_desired, futureGroundNormal).normalized * v_desired.magnitude;
        }
        else
        {
            v_desired = v_desired_parallel;
        }

        Move(v_current, v_desired);
    }


    //returns friction vector given current velocity
    private Vector3 Friction(Vector3 v_current)
    {
        //if moving very slowly, set velocity to 0
        if (v_current.magnitude < stopspeed && v_current.magnitude != 0)
        {
            //Debug.Log("v: "+ v_current.magnitude + " stopped");
            rb.velocity = Vector3.zero;
        }

        //friction in reverse of current direction of movement, scaled to current speed
        Vector3 frictionDirection = -(v_current.normalized);
        float friction_magnitude = Mathf.Max(groundFriction * v_current.magnitude, 10f);
        friction = frictionDirection * friction_magnitude * Time.fixedDeltaTime;

        return friction;
    }


    //movement when in the air
    private void AirMove(Vector3 v_current_2D)
    {
        Vector3 v_desired_2D = v_current_2D + moveInputDirection * airAcceleration * Time.fixedDeltaTime;

        if (v_desired_2D.magnitude > maxAirSpeed)
        {
            if (Vector3.Dot(v_current_2D, moveInputDirection) >= 0)
            {
                return;
            }
            v_desired_2D = Vector3.ClampMagnitude(v_desired_2D, Mathf.Max(maxAirSpeed, v_current_2D.magnitude));
        }

        Move(v_current_2D, v_desired_2D);
    }
    
    //applies velocity changes given current and desired velocity
    private void Move(Vector3 v_current, Vector3 v_desired)
    {
        velocityDifference = v_desired - v_current;
        rb.AddForce(velocityDifference, ForceMode.VelocityChange);
        //Debug.DrawRay(rb.position, v_desired * 10, Color.magenta);
        Debug.Log("v: " + rb.velocity.magnitude + "   grd?: " + grounded);
    }

    //player jumps
    private void Jump()
    {
        Debug.Log("jumping");
        Vector3 v_current = rb.velocity;
        
        //make current movement parallel to xz plane
        Vector3 v_current_parallel = Vector3.ProjectOnPlane(v_current, Vector3.up).normalized * v_current.magnitude;

        float jump_v_change = Mathf.Sqrt(-2.1f * Physics.gravity.y * jumpHeight);// - Mathf.Clamp(rb.velocity.y, 0, 3f); //calculates velocity change needed to reach a designated jump height
        Vector3 v_desired = v_current_parallel + Vector3.up * jump_v_change;

        rb.useGravity = true;
        grounded = false;

        Move(v_current, v_desired);
    }


    //checks if player is on a walkeable surface
    private void CheckGrounded(Vector3 playerCenterPosition)
    {
        bool wasGrounded = grounded;

        //if moving up rapidly, not on ground
        if (rb.velocity.y > comeOffGroundSpeed)
        {
            grounded = false;
            return;
        }

        RaycastHit? hitNullable = TraceBoundingBoxToGround(playerCenterPosition, groundedDistance);
        if (!hitNullable.HasValue)
        {
            grounded = false;
            groundNormal = Vector3.zero;
            return;
        }
        else
        {
            RaycastHit hit = hitNullable.Value;
            grounded = true;
            float distanceToGround = BottomOfPlayerY() - hit.point.y;
            groundNormal = hit.normal;
            Debug.DrawRay(hit.point, hit.normal * 10f, Color.yellow, 0.1f);
            if (!wasGrounded)
            {
                //SnapToGround(distanceToGround);
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            }
        }
    }
    
    // returns:
    // Vector3.zero if staying on ground or  if completely coming off (falling off a cliff or something)
    // normal of hit if leaving ground but close enough to snap down (moving down onto a ramp)
    private Vector3 CheckComingOffGround(Vector3 centerPosition)
    {
        RaycastHit? hitNullable = TraceBoundingBoxToGround(centerPosition, groundedDistance * 2);
        if (!hitNullable.HasValue)
        {
            return Vector3.zero;
        }

        RaycastHit hit = hitNullable.Value;
        float distance = centerPosition.y - bCollider.bounds.extents.y - hit.point.y;
        
        if (distance <= groundedDistance)
        {
            return Vector3.zero;
        }
        else
        {
            return hit.normal;
        }
    }

    //traces bounding box down to a max distance of groundedDistance below the feet of the player
    private RaycastHit? TraceBoundingBoxToGround(Vector3 playerCenterPosition, float distanceDown)
    {
        //ground detection box slightly narrower than player collider so player can't jump perfectly straight up to get on a ledge, only 2 * distanceFromFeet tall
        //Vector3 boxCastExtents = new Vector3(bCollider.bounds.extents.x - 0.001f, distanceDown, bCollider.bounds.extents.z - 0.001f);
        Vector3 boxCastExtents = new Vector3(bCollider.bounds.extents.x, distanceDown, bCollider.bounds.extents.z);
        RaycastHit hit;
        RaycastHit hit2;

        //casts a box downward to detect ground, then raycasts down to double-check/work around bugs
        if (Physics.BoxCast(playerCenterPosition, boxCastExtents, Vector3.down, out hit, Quaternion.identity, bCollider.bounds.extents.y) &&
            hit.collider.Raycast(new Ray(hit.point + Vector3.up, Vector3.down), out hit2, 1 + distanceDown))
        {
            if (hit2.normal == Vector3.up) //if on flat surface
            {
                //checks if contact point is near feet of player
                float playerBottom = playerCenterPosition.y - bCollider.bounds.extents.y;
                if (hit2.point.y - 0.003f > playerBottom)
                {
                    return null;
                }
            }
            else //if on a ramp
            {
                //projection of hit.normal onto xz plane
                Vector3 planeProjection = Vector3.ProjectOnPlane(hit2.normal, Vector3.up);

                //if angle between normal of ramp and xz plane too steep, grounded = false
                float angle = Vector3.Angle(planeProjection, hit2.normal);
                if (angle <= 90 - walkUpRampAngle - 0.5f)
                {
                    return null;
                }
            }
            return hit2;
        }

        //Debug.Log("boxcast miss");
        return null;
    }

    
    //snaps player to the ground when walking
    private void SnapToGround(float distanceDown)
    {
        Vector3 initialPosition = rb.position;
        rb.MovePosition(rb.position + Vector3.down * distanceDown);
        //Debug.Log("initial y: " + initialPosition.y + "should be moved down by d: " + distanceToGround);
        Debug.Log("snapped");
    }

    private float BottomOfPlayerY()
    {
        return bCollider.bounds.center.y - bCollider.bounds.extents.y;
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
