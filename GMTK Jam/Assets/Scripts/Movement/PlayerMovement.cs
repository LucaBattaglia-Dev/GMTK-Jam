using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 11f;
    public float wallRunSpeed = 9f;
    public float superSpeed = 40f;
    public float groundDrag = 5f;
    public float jumpForce = 12f;
    public float airMultiplier = 0.4f;

    [Header("Sprinting & Stamina")]
    public float sprintTime = 6.0f; 
    public float sprintRegenPerSecond = 0.5625f; 
    public float emptyStaminaRegenDelay = 0.5f;
    
    private float sprintTimer;
    private float regenDelayTimer; 
    
    public float SprintTimer
    {
        get { return sprintTimer; }
    }
    private bool isSprinting = false;

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

    [Header("Special Abilities")]
    public KeyCode superSpeedKey = KeyCode.LeftControl;
    public float superSpeedGrowthRate = 35f;
    public float superSpeedDecayRate = 25f;
    public float superSpeedDrainGrowthRate = 3.0f; 
    
    private bool isSuperSpeedActive = false;
    private float currentSpeed;
    private float superSpeedDrainMultiplier = 1f;
    private float superSpeedHoldTimer = 0f; 

    public bool IsExhausted { get; private set; }
    private float exhaustionTimer = 0f;

    [Header("References")]
    public PlayerCam playerCam;

    // Components & Physics State
    private Rigidbody rb;
    private CapsuleCollider col;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;

    public enum MovementState { Walking, Sprinting, SuperSpeed, WallRunning, Air }
    public MovementState state { get; private set; } 
    
    public bool IsSuperSpeeding => state == MovementState.SuperSpeed;

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

    [Header("Audio Source")]
    [SerializeField] private AudioClip superSpeedSound; 
    private AudioSource superSpeedSource; 

    // Time Warp Momentum Tracking
    private float lastGlobalSpeedMultiplier = 1f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
        sprintTimer = sprintTime;
        currentSpeed = walkSpeed;
        lastGlobalSpeedMultiplier = TruckDriver.GlobalSpeedMultiplier;
        superSpeedSource = SFXManager.Instance.PlaySFX(superSpeedSound, 1f, false);
        superSpeedSource.Pause();
        superSpeedSource.loop = true; 
    }

    private void Update()
    {
        // --- TIME WARP MOMENTUM PRESERVATION ---
        float currentGlobalSpeed = TruckDriver.GlobalSpeedMultiplier;
        if (currentGlobalSpeed != lastGlobalSpeedMultiplier)
        {
            if (currentGlobalSpeed < 1f && lastGlobalSpeedMultiplier >= 1f)
            {
                rb.linearVelocity *= currentGlobalSpeed;
            }
            else if (currentGlobalSpeed >= 1f && lastGlobalSpeedMultiplier < 1f)
            {
                rb.linearVelocity /= lastGlobalSpeedMultiplier;
            }
            lastGlobalSpeedMultiplier = currentGlobalSpeed;
        }

        float timeMultiplier = currentGlobalSpeed;

        // Ground detection
        grounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, playerHeight * 0.5f + 0.3f, whatIsGround);
        if (groundHit.collider != null && (lastGround == null || groundHit.collider == lastGround))
        {
            lastGround = groundHit.collider;
            movingTrans = groundHit.collider.transform;
            Vector3 displacement;
            if (movingLastPos == Vector3.zero) displacement = Vector3.zero;
            else displacement = movingTrans.position - movingLastPos;
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

        GetInput();
        CheckForWall();
        StateHandler();

        rb.linearDamping = grounded ? groundDrag : 0f;

        // --- STAMINA LOGIC ---
        bool holdingSprintOrSuperSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(superSpeedKey);

        if (state == MovementState.Sprinting || state == MovementState.SuperSpeed)
        {
            regenDelayTimer = emptyStaminaRegenDelay;

            if (state == MovementState.SuperSpeed)
            {
                superSpeedHoldTimer += Time.deltaTime * timeMultiplier;
                superSpeedDrainMultiplier = 1f + (superSpeedHoldTimer * superSpeedHoldTimer * superSpeedDrainGrowthRate);
                
                sprintTimer -= Time.deltaTime * timeMultiplier * superSpeedDrainMultiplier;
            }
            else
            {
                superSpeedHoldTimer = 0f;
                superSpeedDrainMultiplier = 1f;
                sprintTimer -= Time.deltaTime * timeMultiplier * 0.9f;
            }

            if (sprintTimer <= 0f)
            {
                if (state == MovementState.SuperSpeed)
                {
                    IsExhausted = true;
                    exhaustionTimer = 1.5f; 
                }
                sprintTimer = 0f;
            }
        }
        else
        {
            superSpeedHoldTimer = 0f;
            superSpeedDrainMultiplier = 1f;

            if (holdingSprintOrSuperSpeed)
            {
                regenDelayTimer = emptyStaminaRegenDelay;
            }
            else if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime * timeMultiplier;
            }

            if (regenDelayTimer <= 0f)
            {
                float currentRegenRate = sprintRegenPerSecond;

                if (IsExhausted)
                {
                    currentRegenRate *= 0.25f; 
                    exhaustionTimer -= Time.deltaTime * timeMultiplier;
                    
                    if (exhaustionTimer <= 0f) 
                        IsExhausted = false;
                }

                sprintTimer += Time.deltaTime * timeMultiplier * currentRegenRate;
                if (sprintTimer >= sprintTime)
                    sprintTimer = sprintTime;
            }
        }
    }

    private void FixedUpdate()
    {
        if (state == MovementState.WallRunning) WallRunMovement();
        else 
        {
            MovePlayer();

            // Scale gravity to s^2 during Time Warp so the jump arc stretches symmetrically to the same height
            if (!grounded && TruckDriver.GlobalSpeedMultiplier < 1f)
            {
                float s = TruckDriver.GlobalSpeedMultiplier;
                rb.AddForce(-Physics.gravity * (1f - (s * s)), ForceMode.Acceleration);
            }
        }
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isSuperSpeedActive = Input.GetKey(superSpeedKey);

        if (Input.GetButtonDown("Jump") && grounded)
        {
            Jump();
        }
    }

    private void StateHandler()
    {
        float timeMultiplier = TruckDriver.GlobalSpeedMultiplier;

        if ((wallLeft || wallRight) && verticalInput > 0 && !grounded)
        {
            if (state != MovementState.WallRunning) StartWallRun();
            wallRunTimer -= Time.deltaTime * timeMultiplier;
            if (wallRunTimer <= 0) StopWallRun();
            if (Input.GetButtonDown("Jump")) WallJump();
        }
        else
        {
            if (state == MovementState.WallRunning) StopWallRun();

            if (isSuperSpeedActive && sprintTimer > 0f)
            {
                if(state != MovementState.SuperSpeed){
                    superSpeedSource.Play();
                }
                state = MovementState.SuperSpeed;
            }
            else if (isSprinting && sprintTimer > 0f)
            {
                state = MovementState.Sprinting;
            }
            else if (grounded)
            {
                state = MovementState.Walking;
            }
            else
            {
                state = MovementState.Air;
            }

            if(state != MovementState.SuperSpeed){
                superSpeedSource.Pause(); 
            }
        }
    }

    private void MovePlayer()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (state == MovementState.SuperSpeed)
        {
            currentSpeed += superSpeedGrowthRate * Time.deltaTime;
            if (currentSpeed > superSpeed) currentSpeed = superSpeed;
        }
        else 
        {
            float targetSpeed = (state == MovementState.Sprinting) ? sprintSpeed : walkSpeed;

            if (currentSpeed > targetSpeed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, superSpeedDecayRate * Time.deltaTime);
            else
                currentSpeed = targetSpeed;
        }

        float speed = currentSpeed * TruckDriver.GlobalSpeedMultiplier;

        if (OnSlope())
        {
            Vector3 slopeDir = GetSlopeMoveDirection(moveDirection);
            rb.AddForce(slopeDir * speed * 10f, ForceMode.Force);
            if (rb.linearVelocity.y > 0) rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * speed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * speed * 10f * airMultiplier, ForceMode.Force);
        }

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
        }
        
        // Scale jump force by time multiplier so the initial velocity matches the height target for slow-mo
        float timeMultiplier = TruckDriver.GlobalSpeedMultiplier;
        float jumpScale = (timeMultiplier < 1f) ? timeMultiplier : 1f;

        rb.AddForce(transform.up * jumpForce * jumpScale, ForceMode.Impulse);
    }

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
        if (playerCam != null) playerCam.SetTilt(wallLeft ? -wallTiltAngle : wallTiltAngle);
    }

    private void WallRunMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude) wallForward = -wallForward;
        
        float timeMultiplier = TruckDriver.GlobalSpeedMultiplier;
        rb.AddForce(wallForward * wallRunSpeedForce * timeMultiplier, ForceMode.Force);
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);
    }

    private void StopWallRun()
    {
        rb.useGravity = true;
        if (playerCam != null) playerCam.SetTilt(0f);
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