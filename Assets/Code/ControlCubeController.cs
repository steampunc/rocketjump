using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlCubeController : MonoBehaviour
{
    private const float runAcceleration = 100f; //acceleration when running on ground
    private const float airAcceleration = 10f; //acceleration in air
    private const float maxRunSpeed = 8f;
    private const float maxAirSpeed = 3f;
    private const float groundFriction = 10f;
    private const float stopspeed = 0.1f; //if moving on ground and v < stopspeed, velocity becomes 0
    private const float jumpHeight = 1.1f;
    private const float walkOnSlopeNormal = 0.7f; //if y-value of surface normal >= 0.7f player can walk on it
    private const float groundedDistance = 0.01f; //if distance to ground < groundedDistance, player considered grounded
    private const float comeOffGroundSpeed = 4f; //vertical speed needed to become airborne

    private Vector3 moveInputDirection;
    private Vector3 v_current;

    private BoxCollider bCollider;
    private Rigidbody rb;
    private Camera playerCam;

    private bool grounded;

    private bool jumpPressed;


    // Start is called before the first frame update
    void Start()
    {
        //initialize default values, get necessary components
        moveInputDirection = Vector3.zero;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        //rb.interpolation = RigidbodyInterpolation.Interpolate;

        bCollider = GetComponent<BoxCollider>();
        playerCam = Camera.main;

        grounded = false;
        //groundNormal = Vector3.zero;
        jumpPressed = false;

        //set gravity to __ units/s^2
        Physics.gravity = new Vector3(0, -20f, 0);
        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMovementInput();

        if (!jumpPressed && grounded)
        {
            //jump
            if (Input.GetButtonDown("Jump"))
            {
                jumpPressed = true;
            }
        }
        DrawDebugLines();
    }

    //Called every time physics updates
    private void FixedUpdate()
    {
        grounded = CheckGrounded();
        if (grounded)
        {
            v_current.y = 0;
            Friction();

            if (jumpPressed)
            {
                Jump();
                jumpPressed = false;
                grounded = false;
                //Debug.Log("done jumping");
            }
            
            rb.velocity = Vector3.zero;
            WalkMove();
        }
        else
        {
            AirMove();
        }
        //Debug.Log("grounded: " + grounded);
        //Debug.Log("v: " + v_current + "   grd?: " + grounded);
        //grounded = false;
    }

    //checks if player is on a walkeable surface
    private bool CheckGrounded()
    {
        Vector3 boxCastExtents = new Vector3(bCollider.bounds.extents.x, groundedDistance, bCollider.bounds.extents.z);
        RaycastHit hit;
        RaycastHit hit2;

        //casts a box downward to detect ground, then raycasts down to double-check/work around bugs
        if (Physics.BoxCast(rb.position, boxCastExtents, Vector3.down, out hit, Quaternion.identity, bCollider.bounds.extents.y) &&
            hit.collider.Raycast(new Ray(hit.point + Vector3.up, Vector3.down), out hit2, 1 + groundedDistance))
        {
            if (hit2.normal.y >= walkOnSlopeNormal)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Debug.Log("boxcast miss");
        return false;
    }

    /*
    //checks if player is on a walkeable surface
    private bool CheckNormalGrounded(Vector3 normal, Vector3 position)
    {
        
        if (normal == Vector3.up) //if on flat surface
        {
            //checks if contact point is near feet of player
            if (position.y - 0.01f <= BottomOfPlayerY())
            {
                return true;
            }
            else
            {
                Debug.Log("position: " + position + ", bottom of player: " + BottomOfPlayerY());
            }
        }
        else
        {
            float angle = GetRampAngleFromNormal(normal); //if on a ramp and ramp angle not too steep
            if (angle >= 0 && angle <= walkUpRampAngle)
            {
                return true;
            }
        }
        return false;
    }

    
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint cp = collision.GetContact(0);
        grounded = CheckNormalGrounded(cp.normal, cp.point);
        if (grounded)
        {
            Debug.DrawRay(cp.point, cp.normal * 5, Color.yellow);
        }
        else
        {
            Debug.DrawRay(cp.point, cp.normal * 5, Color.red);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint cp = collision.GetContact(0);
        grounded = CheckNormalGrounded(cp.normal, cp.point);
        if (grounded)
        {
            Debug.DrawRay(cp.point, cp.normal * 5, Color.yellow);
        } else
        {
            Debug.DrawRay(cp.point, cp.normal * 5, Color.red);
        }
    }
    */

    private LayerMask MaskFromIntArray(int[] intArray)
    {
        int mask = 0;
        foreach (int i in intArray)
        {
            mask = mask | 1 << i;
        }
        return mask;
    }
    

    //movement when grounded
    private void WalkMove()
    {
        Vector3 acceleration = moveInputDirection * runAcceleration; //movement acceleration = direction * acceleration scaled for time
        Vector3 v_desired = v_current + acceleration * Time.fixedDeltaTime;

        if (v_desired.magnitude > maxRunSpeed) //can't exceed max run speed
        {
            v_desired = Vector3.ClampMagnitude(v_desired, maxRunSpeed);
        }

        //make movement direction parallel to surface player is standing on
        //v_desired = Vector3.ProjectOnPlane(v_desired, groundNormal).normalized * v_desired.magnitude;
        
        //Move(v_desired);
    }

    //applies friction to current velocity
    private void Friction()
    {
        //if moving very slowly, set velocity to 0
        if (v_current.magnitude < stopspeed && v_current.magnitude != 0)
        {
            v_current = Vector3.zero;
        }
        v_current = v_current * 0.8f;
    }

    //movement when in the air
    private void AirMove()
    {
        Vector3 acceleration = moveInputDirection * airAcceleration + Physics.gravity;
        Vector3 v_desired = v_current + acceleration * Time.fixedDeltaTime;
        Vector3 v_desired_xz = new Vector3(v_desired.x, 0, v_desired.z);

        if (v_desired_xz.magnitude > maxAirSpeed)
        {
            if (Vector3.Dot(v_desired_xz, moveInputDirection) >= 0)
            {
                return;
            }
            v_desired_xz = Vector3.ClampMagnitude(v_desired_xz, Mathf.Max(maxAirSpeed, v_desired_xz.magnitude));
        }

        v_desired.x = v_desired_xz.x;
        v_desired.z = v_desired_xz.z;

        Move(v_desired);
    }

    //applies velocity changes given current and desired velocity
    private void Move(Vector3 v_desired)
    {
        RaycastHit? hitInfo = TracePlayerBBox(rb.position, v_desired * Time.fixedDeltaTime);

        if (!hitInfo.HasValue)
        {
            Vector3 newPosition = rb.position + v_desired * Time.fixedDeltaTime;
            //Debug.Log("moving from: " + rb.position + " to: " + newPosition);
            
            //rb.MovePosition(rb.position + v_desired);
            rb.position = newPosition;

            v_current = v_desired;
        }
        else
        {
            RaycastHit hit = hitInfo.Value;

            Debug.DrawRay(rb.position, hit.normal * 2f, Color.grey);

            float distance = hit.distance;
            Vector3 direction = v_desired.normalized;
            Vector3 newPosition = rb.position + direction * distance;

            Debug.Log("moving distance: " + distance);

            //rb.MovePosition(rb.position + direction * distance);
            rb.position = newPosition;

            v_current = v_desired;
        }
    }

    //player jumps
    private void Jump()
    {
        Debug.Log("jumping");
        //make current movement parallel to xz plane
        //Vector3 v_current_parallel = Vector3.ProjectOnPlane(v_current, Vector3.up).normalized * v_current.magnitude;

        float jump_velocity = Mathf.Sqrt(-2.1f * Physics.gravity.y * jumpHeight); // - Mathf.Clamp(rb.velocity.y, 0, 3f); //calculates velocity change needed to reach a designated jump height
        //Vector3 v_desired = v_current_parallel + Vector3.up * jump_v_change;
        v_current.y += jump_velocity;
    }
    
    private void UpdateMovementInput()
    {
        Vector3 axisInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        axisInput = Vector3.ClampMagnitude(axisInput, 1);
        moveInputDirection = axisInput;
    }

    private void DrawDebugLines()
    {
        //Debug.DrawRay(transform.position, rb.velocity, Color.green); //draw velocity vector
        Debug.DrawRay(transform.position, moveInputDirection * 10, Color.blue); //draw moveInput vector
        //Debug.DrawRay(transform.position, friction * 5, Color.red); //draw friction vector
    }

    //returns velocity along the xz plane
    public Vector3 Velocity2D()
    {
        return new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }
    
    private RaycastHit? TracePlayerBBox(Vector3 boxCenter, Vector3 velocity)
    {
        //Debug.Log(velocity);

        Vector3 boxCastExtents = bCollider.bounds.extents;
        
        Vector3 direction = velocity.normalized;
        float maxDistance = velocity.magnitude;

        Vector3 boxBottomCenter = boxCenter + new Vector3(0, -boxCastExtents.y, 0);

        //Debug.Log("transform position: " + transform.position + ", rb position: " + rb.position);
        Debug.DrawRay(rb.position, Vector3.forward * 4, Color.cyan);
        Debug.DrawRay(boxBottomCenter, direction * maxDistance);

        //casts a box downward to detect ground, then raycasts down to double-check/work around bugs
        if (Physics.BoxCast(boxCenter, boxCastExtents, direction, out RaycastHit hit, Quaternion.identity, maxDistance))
        {
            //Debug.Log("boxcast hit at " + hit.point);
            return hit;
        }

        //Debug.Log("boxcast miss");
        return null;
    }

    private float BottomOfPlayerY()
    {
        return bCollider.bounds.center.y - bCollider.bounds.extents.y;
    }

    //returns slope of ramp (rise/run) where run = 1
    private float GetRampAngleFromNormal(Vector3 normal)
    {
        //projection of hit.normal onto xz plane
        Vector3 planeProjection = Vector3.ProjectOnPlane(normal, Vector3.up);
        float angle = 90 - Vector3.Angle(planeProjection, normal);
        //Debug.Log("standing on ramp with angle: " + angle);
        return angle;
    }
}
