using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPositionController : MonoBehaviour
{
    private const float runAcceleration = 100f; //acceleration when running on ground
    private const float airAcceleration = 40f; //acceleration in air
    private const float maxRunSpeed = 8f;
    private const float maxAirSpeed = 3f;
    private const float groundFriction = 10f;
    private const float stopspeed = 0.1f; //if moving on ground and v < stopspeed, velocity becomes 0
    private const float jumpHeight = 1.2f;
    private const float walkOnSlopeNormal = 0.7f; //if y-value of surface normal >= 0.7f player can walk on it
    private const float comeOffGroundSpeed = 4f; //vertical speed needed to become airborne
    private const float groundedDistance = 0.011f; //if distance to ground < groundedDistance, player considered grounded
    private const float stepSize = 0.11f;

    private const float lookSpeed = 2f; //view sensitivity
    private Vector2 lookRotation;
    private Vector3 moveInputDirection;

    private Vector3 v_current;

    private BoxCollider bCollider;
    private Rigidbody rb;
    private Camera playerCam;

    private bool grounded;

    private bool jumpPressed;
    private bool hitByExplosion;


    // Start is called before the first frame update
    //initialize default values, get necessary components
    void Start()
    {
        moveInputDirection = Vector3.zero;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;

        //set gravity to __ units/s^2
        Physics.gravity = new Vector3(0, -15f, 0);
        rb.useGravity = false;
        

        bCollider = GetComponent<BoxCollider>();
        playerCam = Camera.main;

        grounded = false;
        jumpPressed = false;
        hitByExplosion = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLookDirection();
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

    //Called every time physics updates
    private void FixedUpdate()
    {
        if (jumpPressed)
        {
            Jump();
            jumpPressed = false;
            grounded = false;
        }

        if (hitByExplosion)
        {
            hitByExplosion = false;
            grounded = false;
            rb.isKinematic = false;
            rb.useGravity = true;
            v_current = Vector3.zero;
            return;
        }

        bool wasGrounded = grounded;
        grounded = CheckGrounded();
        //Debug.Log("grounded: " + grounded);
        
        if (grounded)
        {
            if (!wasGrounded)
            {
                v_current = rb.velocity;
            }
            //rb.velocity = Vector3.zero;
            rb.useGravity = false;

            rb.isKinematic = true; ////

            v_current.y = 0;

            Friction();
            WalkMove();
            //Debug.Log("v: " + v_current + "   grd?: " + grounded);
        }
        else
        {
            if (wasGrounded)
            {
                //rb.velocity = rb.velocity + v_current;
                //rb.velocity updates by itself
            }

            rb.isKinematic = false; ////

            rb.useGravity = true;
            v_current = Vector3.zero;
            
            AirMove();
        }
        //Debug.Log("rb.velocity: " + rb.velocity + " magnitude: " + rb.velocity.magnitude);
        //Debug.Log("v_current: " + v_current + " magnitude: " + v_current.magnitude);
        //grounded = false;
        if (!PositionIsValid(rb.position))
        {
            //Debug.Log("Position invalid!");
        }

    }

    //checks if player is on a walkeable surface
    private bool CheckGrounded()
    {
        Vector3 boxCastExtents = new Vector3(bCollider.bounds.extents.x, groundedDistance, bCollider.bounds.extents.z);
        RaycastHit hit;
        RaycastHit hit2;

        bool wasGrounded = grounded;

        //casts a box downward to detect ground, then raycasts down to double-check/work around bugs
        if (Physics.BoxCast(rb.position, boxCastExtents, Vector3.down, out hit, Quaternion.identity, bCollider.bounds.extents.y) &&
            hit.collider.Raycast(new Ray(hit.point + Vector3.up, Vector3.down), out hit2, 1 + groundedDistance))
        {
            if (hit2.point.y - groundedDistance > BottomOfPlayerY())
            {
                //Debug.Log("contact point too high. Contact.y: " + hit2.point.y + ", playerbottom.y: " + BottomOfPlayerY());
                return wasGrounded;
            }

            if (SurfaceIsStandeable(hit2.normal))
            {
                //if moving up at an angle too steep to be walking up a ramp/the ground
                if (!wasGrounded && MovingUpRelativeToPlane(hit2.normal, rb.velocity))
                {
                    //Debug.Log("moving up relative to plane");
                    return false;
                }
                //Debug.DrawRay(hit2.point, hit2.normal * 3, Color.red);
                return true;
            }
            else
            {
                //Debug.Log("slope too steep");
                return false;
            }
        }
        //Debug.Log("boxcast miss");
        return false;
    }

    private void LateUpdate()
    {
        UpdateFirstPersonView();
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
        axisInput = axisInput.normalized;

        float degreeRotation = playerCam.transform.eulerAngles.y - 360;
        axisInput = Quaternion.Euler(0, degreeRotation, 0) * axisInput;

        moveInputDirection = axisInput;
        //moveInputDirection = new Vector3(3, 0, 1).normalized; //test
    }

    //moving 
    private bool MovingUpRelativeToPlane(Vector3 normal, Vector3 velocity)
    {
        if (velocity.y < 0)
        {
            return false;
        }
        else if (velocity.normalized == normal)
        {
            return true;
        }

        Vector3 planeProjection = Vector3.ProjectOnPlane(velocity, normal);

        //Debug.DrawRay(rb.position + Vector3.down * 0.2f, planeProjection, Color.black);

        float angleDiff = Vector3.Angle(planeProjection, velocity);
        if (angleDiff < 1)
        {
            return false;
        }
        //Debug.Log("angleDiff: " + angleDiff);
        return true;
    }

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
        
        Vector3 newPos = rb.position;
        float boxcastoffset = 0.01f;

        Vector3 initial_direction = v_desired.normalized;
        float distance = (v_desired * Time.fixedDeltaTime).magnitude;

        int numIterations = 0;
        Vector3 direction = initial_direction;
        while (distance > 0 && numIterations < 6)
        {
            numIterations++;
            RaycastHit? hitInfo = TracePlayerBBox(newPos - direction * boxcastoffset, direction * (distance + boxcastoffset));
            if (!hitInfo.HasValue)
            {
                newPos = newPos + direction * distance; //didn't hit anything, free movement :)
                Debug.DrawRay(newPos, direction, Color.green);
                break;
            }
            else //move as far as possible. subtract hit distance from distance. if hit surface is standeable, redirect movement direction. continue loop
            {
                RaycastHit hit = hitInfo.Value;
                float moveDistance = hit.distance - boxcastoffset;
                float prevY = newPos.y;
                newPos = newPos + direction * moveDistance;
                distance = distance - moveDistance;
                //Debug.Log("hit. moving from y: " + prevY + " to: " + newPos.y);
                Debug.DrawRay(hit.point, hit.normal * 3, Color.cyan);
                
                if (SurfaceIsStandeable(hit.normal))
                {
                    direction = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
                    //Debug.Log("redirect direction to: " + direction);
                    continue;
                }
                else
                {
                    //try moving up a stair
                    float stairMoveDistance = TryMovingUpStair(newPos, direction, distance, boxcastoffset);
                    if (stairMoveDistance > 0.01f)
                    {
                        float oldY = newPos.y;
                        newPos = newPos + Vector3.up * stepSize; //+ direction * stairMoveDistance;
                        //Debug.Log("stairmoved from: " + oldY + " by: " + stepSize);
                        //Debug.Log("able to stairmove forward " + stairMoveDistance / distance + " of the way");
                        direction = initial_direction;
                    }
                    else
                    {
                        //we hit a wall. project movement vector on wall so if we don't hit it head on, we can slide along it

                        //pretend wall goes straight up
                        Vector3 wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                        direction = Vector3.ProjectOnPlane(direction, wallNormal);
                        if (direction == Vector3.zero)
                        {
                            break;
                        }
                        continue;
                    }
                }
            }
        }
        //Debug.Log("desired: " + (v_desired * Time.fixedDeltaTime).magnitude);
        
        Vector3 finalPos = StayOnGround(newPos);
        //Debug.Log("intial.y: " + rb.position.y + ", before stayonground: " + newPos.y + ", final pos.y: " + finalPos.y);

        //rb.position = finalPos;
        rb.MovePosition(finalPos);
        v_current = v_desired;
    }

    //jumps position up by stepSize, tries moving forward, returns distance moved forward
    private float TryMovingUpStair(Vector3 pos, Vector3 direction, float distance, float boxcastoffset)
    {
        RaycastHit? moveUpHitInfo = TracePlayerBBox(pos + Vector3.down * boxcastoffset, Vector3.up * (boxcastoffset + stepSize));
        if (moveUpHitInfo.HasValue)
        {
            RaycastHit hit = moveUpHitInfo.Value;
            if (FastApproximately(hit.distance, boxcastoffset, 0.0001f))
            {
                return 0;
            }
            else
            {
                pos = pos + Vector3.up * (hit.distance - boxcastoffset);
            }
        }
        else
        {
            pos = pos + Vector3.up * stepSize;
        }
        
        Vector3 start = pos - direction * boxcastoffset;
        Vector3 movement = direction * (distance + boxcastoffset);
        RaycastHit? hitInfo = TracePlayerBBox(start, movement);
        if (!hitInfo.HasValue)
        {
            //Debug.Log("stair test didn't hit anything");
            return distance;
        }
        else
        {
            //Debug.Log("stair test hit something");
            RaycastHit hit = hitInfo.Value;
            float moveDistance = hit.distance - boxcastoffset;
            return moveDistance;
        }
    }

    private bool PositionIsValid(Vector3 position)
    {
        if (Physics.CheckBox(position, bCollider.bounds.extents, Quaternion.identity, MaskFromIntArray(new int[] { 0 })))
        {
            return false;
        }
        return true;
    }

    private bool SurfaceIsStandeable(Vector3 normal)
    {
        if (normal.y >= walkOnSlopeNormal)
        {
            return true;
        }
        //Debug.Log("slope: " + normal.y + " too steep, < " + walkOnSlopeNormal);
        return false;
    }

    private Vector3 StayOnGround(Vector3 newPos)
    {
        float upOffset = 0.01f;
        Vector3 start = newPos + Vector3.up * upOffset;
        Vector3 movement = Vector3.down * (stepSize + upOffset + 0.001f);

        RaycastHit? hitInfo = TracePlayerBBox(start, movement);

        if (hitInfo.HasValue)
        {
            RaycastHit hit = hitInfo.Value;

            if (!FastApproximately(hit.distance, upOffset, 0.0001f))
            {
                Debug.Log("snapped distance " + hit.distance);
                return start + Vector3.down * hit.distance;
            }
        }
        else
        {
            Debug.Log("stayonground miss");
        }
        
        return newPos;
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
        Vector3 acceleration = moveInputDirection * airAcceleration;
        Vector3 v_desired = rb.velocity + acceleration * Time.fixedDeltaTime;
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

        rb.velocity = v_desired;
        //rb.AddForce(v_desired - rb.velocity);

        //Move(v_desired);
    }

    //player jumps
    private void Jump()
    {
        Debug.Log("jumping");
        float jump_velocity = Mathf.Sqrt(-2.1f * Physics.gravity.y * jumpHeight); //calculates velocity change needed to reach a designated jump height
        if (hitByExplosion)
        {
            rb.velocity = rb.velocity + Vector3.up * jump_velocity;
        }
        else
        {
            rb.velocity = new Vector3(rb.velocity.x, jump_velocity, rb.velocity.z);
        }
        //Debug.Log("jump velocity " + jump_velocity + " actual: " + rb.velocity);
    }

    private void DrawDebugLines()
    {
        //Debug.DrawRay(transform.position, rb.velocity, Color.green); //draw velocity vector
        //Debug.DrawRay(transform.position, moveInputDirection * 10, Color.blue); //draw moveInput vector
        //Debug.DrawRay(transform.position, friction * 5, Color.red); //draw friction vector
    }

    //returns velocity along the xz plane
    public Vector3 Velocity2D()
    {
        return new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }
    
    private RaycastHit? TracePlayerBBox(Vector3 boxCenter, Vector3 movement)
    {
        Vector3 direction = movement.normalized;
        float maxDistance = movement.magnitude;

        Vector3 boxCastExtents = bCollider.bounds.extents;
        
        Vector3 boxBottomCenter = boxCenter + new Vector3(0, -boxCastExtents.y, 0);

        //Debug.Log("transform position: " + transform.position + ", rb position: " + rb.position);
        if (Physics.BoxCastAll(boxCenter, boxCastExtents, direction, Quaternion.identity, maxDistance).Length > 1)
        {
            Debug.Log("boxcast hit multiple things");
        }

        if (Physics.BoxCast(boxCenter, boxCastExtents, direction, out RaycastHit hit1, Quaternion.identity, maxDistance))
        {
            //Debug.DrawRay(hit1.point, hit1.normal * 3, Color.magenta);
            //if (hit1.collider.Raycast(new Ray(hit1.point + -direction, direction), out RaycastHit hit2, 2f))
            //{
            //    hit1.normal = hit2.normal;
            //    Debug.DrawRay(hit1.point, hit1.normal * 4, Color.white);
            //}
            //Debug.DrawRay(hit1.point, hit1.normal * 4, Color.red);

            //Debug.Log("boxcast hit at y: " + hit1.point.y + " of collider: " + hit1.collider.name);
            return hit1;
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

    public static bool FastApproximately(float a, float b, float threshold)
    {
        return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
    }

    public void HitByExplosion(Vector3 explosionV)
    {
        hitByExplosion = true;
        rb.isKinematic = false;

        Debug.Log("explosion force magnitude: " + explosionV.magnitude);
        Debug.Log("current v magnitude: " + rb.velocity.magnitude);

        rb.AddForce(explosionV, ForceMode.VelocityChange);

        //float scaleFactor = 0.4f;
        //Vector3 newV = new Vector3(rb.velocity.x * 1, rb.velocity.y, rb.velocity.z * 1);
        //if (newV.y < 0)
        //{
        //    newV = new Vector3(newV.x, newV.y * scaleFactor, newV.z);
        //}
        //rb.velocity = newV;
    }
}
