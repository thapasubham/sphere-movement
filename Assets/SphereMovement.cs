using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SphereMovement : MonoBehaviour
{
    [SerializeField, Range(5, 100)]
    float maxAcceleration, airAcceleration = 1.5f;
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField]
    Transform playerInputSpace = default;
    [SerializeField, Range(0, 90)]
    float maxGroundAngle = 25f, maxStairsAngle = 50f;
    [SerializeField]
    float jumpHeight = 5;
    bool jump;
    [SerializeField, Range(1, 3)]
    int maxJump = 2;
    int currentJump;
    [SerializeField, Min(0f)]
    float probeDistance = 1f;
    Rigidbody body;
    [SerializeField]
    LayerMask probeMask = -1, stairMask = -1;

    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;
    Vector3 upAxis;
    Vector3 velocity, desiredVelocity, jumpVelocity;
    Vector2 playerInput;

    float minGroundDotProduct, minStairsDotProduct;
    Vector3 contactNormal, steepNormal;


    int groundContactCount, steepContactCount;

    bool OnGround => groundContactCount > 0;

    bool OnSteep => steepContactCount > 0;
    int stepSinceLastGround, stepsSinceLastJump;
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    // Update is called once per frame
    void Update()
    {
        jump |= Input.GetButtonDown("Jump");

        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");

        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            desiredVelocity =
                (forward * playerInput.y + right * playerInput.x) * maxSpeed;
        }
        else
        {
            desiredVelocity =
                new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        }
    }

    void FixedUpdate()
    {
        upAxis = -Physics.gravity.normalized;
        UpdateState();



        AdjustVelocity();

        if (jump)
        {
            jump = false;
            jumpVelocity = body.velocity;

            Jump();
        }
        body.velocity = velocity;
        clearState();

    }
    void Jump()
    {

        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            currentJump = 0;
        }
        else if (maxJump > 0 && currentJump <= maxJump)
        {
            if (currentJump == 0)
            {
                currentJump = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        stepsSinceLastJump = 0;
        currentJump++;

        float jumpSpeed = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;
        float alignedSpeed = Vector3.Dot(jumpVelocity, jumpDirection);

        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }


        jumpVelocity = jumpDirection * jumpSpeed;
        velocity = jumpVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }



    void EvaluateCollision(Collision collision)
    {

        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {


            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(upAxis, normal);
            Debug.DrawRay(collision.GetContact(i).point, normal, Color.green, 1f);
            if (upDot >= minDot)
            {

                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (upDot > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }
    void AdjustVelocity()
    {

        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : airAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX =
            Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);
        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }
    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
    void clearState()
    {
        groundContactCount = 0;
        steepContactCount = 0;
        contactNormal = Vector3.zero;
    }

    void UpdateState()
    {
        stepSinceLastGround += 1;
        stepsSinceLastJump += 1;
        velocity = body.velocity;

        if (OnGround || SnapToGround() || CheckSteepContacts())
        {

            stepSinceLastGround = 0;

            if (stepsSinceLastJump > 1)
            {
                currentJump = 0;
            }

            if (groundContactCount > 1)
            {

                contactNormal.Normalize();
            }


        }

        else
        {
            contactNormal = upAxis;
        }
    }
    bool SnapToGround()
    {
        if (stepSinceLastGround > 1 || stepsSinceLastJump <= 2)
        {
            return false;

        }

        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }

        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
        {

            return false;
        }
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        groundContactCount = 1;
        contactNormal = hit.normal;

        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }
    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            float upDot = Vector3.Dot(steepNormal, upAxis);
            if (upDot >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }
    float GetMinDot(int layer)
    {
        return (stairMask & (1 << layer)) == 0 ?
             minGroundDotProduct : minStairsDotProduct;
    }
}
