using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 11f;
    public float slideSpeed = 12f;
    public float wallRunSpeed = 9f;
    public float groundDrag = 5f;
    public float jumpForce = 12f;
    public float airMultiplier = 0.4f;

    [Header("Sprinting")]
    public float sprintTime = 3f;
    public float sprintRegenPerSecond = 1.5f;
    private float sprintTimer;
    public float SprintTimer
    {
        get
        {
            return sprintTimer;
        }
    }
    private bool isSprinting = false;

    [Header("Sliding")]
    public float airGracePeriod = 1f;
    public float maxSlideTime = 0.75f;
    public float slideForce = 400f;
    public float slideColliderHeight = 1f; // Half height collider during slide
    public float slideStickForce = 25f; // Keeps player glued to ground
    private float airGraceTime;
    private float startColliderHeight;
    private Vector3 startColliderCenter;
    private float slideTimer;

    [Header("Wall Running")]
    public LayerMask whatIsWall;
    public float wallCheckDistance = 0.8f;
    public float wallRunSpeedForce = 200f;
    public float wallJumpUpForce = 10f;
    public float wallJumpSideForce = 10f;
    public float maxWallRunTime = 2f;
    public float wallTiltAngle = 10f;
    private float wallRunTimer;

    [Header("Ground & Slopes")]
    public LayerMask whatIsGround;
    public float playerHeight = 2f;
    public float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;

    [Header("References")]
    public PlayerCam playerCam;

    // Components & Physics State
    private Rigidbody rb;
    private CapsuleCollider col;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;

    private MovementState state;
    private enum MovementState { Walking, Sprinting, Sliding, WallRunning, Air }

    private bool grounded;
    private RaycastHit groundHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;
    private Transform movingTrans;
    private Vector3 movingLastPos;
    private Vector3 movingVelocity;
    private Collider lastGround;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;

        startColliderHeight = col.height;
        startColliderCenter = col.center;

        sprintTimer = sprintTime;
    }

    private void Update()
    {
        // Ground detection
        grounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, playerHeight * 0.5f + 0.3f, whatIsGround);
        if (groundHit.collider != null && (lastGround == null || groundHit.collider == lastGround))
        {
            lastGround = groundHit.collider;
            movingTrans = groundHit.collider.transform;
            Vector3 displacement;
            if (movingLastPos == Vector3.zero)
                displacement = Vector3.zero;
            else
                displacement = movingTrans.position - movingLastPos;
            movingVelocity = (movingTrans.position - movingLastPos) / Time.deltaTime;
            movingLastPos = movingTrans.position;
            transform.position += displacement;
        }
        else
        {
            movingTrans = null;
            movingLastPos = Vector3.zero;
            lastGround = null;
        }

        // Inputs and States
        GetInput();
        CheckForWall();
        StateHandler();

        // Physics drag
        rb.linearDamping = grounded ? groundDrag : 0f;

        // Sprint Timer
        if (isSprinting)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
                sprintTimer = 0f;
        }
        else
        {
            sprintTimer += Time.deltaTime * sprintRegenPerSecond;
            if (sprintTimer >= sprintTime)
                sprintTimer = sprintTime;
        }
    }

    private void FixedUpdate()
    {
        if (state == MovementState.Sliding)
            SlideMovement();
        else if (state == MovementState.WallRunning)
            WallRunMovement();
        else
            MovePlayer();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Jump
        if (Input.GetButtonDown("Jump") && grounded)
        {
            Jump();
        }

        // Slide Trigger
        if (Input.GetKey(KeyCode.LeftControl) && state == MovementState.Air)
        {
            airGraceTime += Time.deltaTime;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            airGraceTime = 0f;
        }
        if ((Input.GetKeyDown(KeyCode.LeftControl) || (airGraceTime > 0f && airGraceTime <= airGracePeriod)) && (horizontalInput != 0 || verticalInput != 0) && grounded)
        {
            if (airGraceTime > 0f && airGraceTime <= airGracePeriod)
            {
                airGraceTime = 0f;
            }
            StartSlide();
        }
        if (Input.GetKeyUp(KeyCode.LeftControl) && state == MovementState.Sliding)
        {
            airGraceTime = 0f;
            StopSlide();
        }

        // Sprint
        isSprinting = Input.GetKey(KeyCode.LeftShift) && grounded;
    }

    private void StateHandler()
    {
        // 1. Wall Running
        if ((wallLeft || wallRight) && verticalInput > 0 && !grounded)
        {
            if (state != MovementState.WallRunning)
                StartWallRun();

            wallRunTimer -= Time.deltaTime;
            if (wallRunTimer <= 0)
                StopWallRun();

            if (Input.GetButtonDown("Jump"))
                WallJump();
        }
        // 2. Sliding
        else if (state == MovementState.Sliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
                StopSlide();
        }
        // 3. Grounded
        else if (grounded)
        {
            if (state == MovementState.WallRunning) StopWallRun();

            state = isSprinting && sprintTimer != 0f ? MovementState.Sprinting : MovementState.Walking;
        }
        // 4. In Air
        else
        {
            if (state == MovementState.WallRunning) StopWallRun();
            state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        float speed = (state == MovementState.Sprinting) ? sprintSpeed : walkSpeed;

        if (OnSlope())
        {
            Vector3 slopeDir = GetSlopeMoveDirection(moveDirection);
            rb.AddForce(slopeDir * speed * 10f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * speed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * speed * 10f * airMultiplier, ForceMode.Force);
        }

        // Speed Limit
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > speed)
        {
            Vector3 limitedVel = flatVel.normalized * speed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (movingTrans != null)
        {
            Vector3 totalVelocity = rb.linearVelocity + movingVelocity;
            rb.linearVelocity = totalVelocity;
            Debug.Log(totalVelocity);
        }
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    // --- SLIDING ---
    private void StartSlide()
    {
        state = MovementState.Sliding;
        slideTimer = maxSlideTime;

        col.height = slideColliderHeight;
        col.center = new Vector3(startColliderCenter.x, slideColliderHeight * 0.5f, startColliderCenter.z);

        if (playerCam != null)
            playerCam.SetSlide(true);

        Vector3 inputDir = transform.forward * verticalInput + transform.right * horizontalInput;
        rb.AddForce(inputDir.normalized * slideForce, ForceMode.Impulse);
        rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
    }

    private void SlideMovement()
    {
        Vector3 inputDir = transform.forward * verticalInput + transform.right * horizontalInput;
        Vector3 slideDirection = OnSlope() ? GetSlopeMoveDirection(inputDir) : inputDir.normalized;

        rb.AddForce(slideDirection * slideForce, ForceMode.Force);
        rb.AddForce(Vector3.down * slideStickForce, ForceMode.Force);
    }

    private void StopSlide()
    {
        col.height = startColliderHeight;
        col.center = startColliderCenter;

        if (playerCam != null)
            playerCam.SetSlide(false);

        if (grounded)
            state = Input.GetKey(KeyCode.LeftShift) ? MovementState.Sprinting : MovementState.Walking;
        else
            state = MovementState.Air;
    }

    // --- SLOPE DETECTION ---
    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.5f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    // --- WALL RUNNING ---
    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private void StartWallRun()
    {
        state = MovementState.WallRunning;
        wallRunTimer = maxWallRunTime;
        rb.useGravity = false;

        if (playerCam != null)
            playerCam.SetTilt(wallLeft ? -wallTiltAngle : wallTiltAngle);
    }

    private void WallRunMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunSpeedForce, ForceMode.Force);
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);
    }

    private void StopWallRun()
    {
        rb.useGravity = true;
        if (playerCam != null)
            playerCam.SetTilt(0f);
    }

    private void WallJump()
    {
        StopWallRun();
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}