using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;

public class PlayerMove : MonoBehaviour
{
    public bool debugMode = false;
    //Data
    public PlayerData data;
    public Camera cam;
    Vector2 moveInput;
    //Is bools
    public bool isFacingRight;
    public bool isJumping;
    public bool isRocketJumping;
    public bool isWallJumping;
    public bool isSliding;
    public bool isFallingAfterJump;
    public bool isFallingAfterRocketJump;
    public bool isJumpCut;
    //Component
    public Rigidbody2D rb;
    //Timers
    public float jumpInputKeepTimer;
    public float onGroundTimer;
    public float onWallRightTimer;
    public float onWallLeftTimer;
    float onWallTimer;
    private int lastWallJumpDir;
    private float wallJumpStartTime;
    public int numberOfAirRocketJumps = 0;
    public Sprite faceRight;
    public Sprite faceLeft;
    [SerializeField] Animator playerAnim;
    //Checks
    [Header("Checks")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform rightWallCheckPoint;
    [SerializeField] private Transform leftWallCheckPoint;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.5f, 1f);
    [Header("Layers & Tags")]
    [SerializeField] private LayerMask groundLayer;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        //playerAnim = GetComponent<Animator>();
        rb.gravityScale = data.gravityScale;
        if (PlayerPrefs.HasKey(SceneManager.GetActiveScene().buildIndex+"positionX")&& PlayerPrefs.HasKey(SceneManager.GetActiveScene().buildIndex + "positionY"))
        {
            transform.position = new Vector2(PlayerPrefs.GetFloat(SceneManager.GetActiveScene().buildIndex + "positionX"), PlayerPrefs.GetFloat(SceneManager.GetActiveScene().buildIndex + "positionY"));
        }
    }

    void Update()
    {
        /*if (FindObjectOfType<PlayerDeath>().dying)
        {
            rb.simulated = false;
            return;
        }
        else
        {
            rb.simulated = true;
        }
        if (FindObjectOfType<Dialogue>() != null)
        {
            if(FindObjectOfType<Dialogue>().dialogueInProgress)
            {
                rb.simulated = false; return;
            }
        }
        else
        {
            rb.simulated = true;
        }*/
        onGroundTimer -= Time.deltaTime;
        onWallTimer -= Time.deltaTime;
        onWallRightTimer -= Time.deltaTime;
        onWallLeftTimer -= Time.deltaTime;

        jumpInputKeepTimer -= Time.deltaTime;
        Checks();
        Inputs();
        Gravity();        
        if (CanJump()&& jumpInputKeepTimer >0)
        {
            isJumping = true;
            isRocketJumping = false;
            isWallJumping = false;
            isJumpCut = false;
            isFallingAfterJump = false;
            Jump();
        }
        else if (CanWallJump() && jumpInputKeepTimer > 0)
        {
            isWallJumping = true;
            isJumping = false;
            isRocketJumping=false;
            isJumpCut = false;
            isFallingAfterJump = false;
            wallJumpStartTime = Time.time;
            lastWallJumpDir = (onWallRightTimer > 0) ? -1 : 1;

            WallJump(lastWallJumpDir);
        }
        /*playerAnim.SetBool("isFacingRight", isFacingRight);
        if (isJumping || isRocketJumping)
        {
            playerAnim.SetBool("isJumping", true);
        }
        else
        {
            playerAnim.SetBool("isJumping", false);
        }
        if (isFallingAfterJump || isFallingAfterRocketJump)
        {
            playerAnim.SetBool("isFalling", true);
        }
        else
        {
            playerAnim.SetBool("isFalling", false);
        }*/
    }
    private void Gravity()
    {
        if(debugMode)
        {
            rb.gravityScale = 0;
            return;
        }
        if (isSliding)
        {
            rb.gravityScale = 0;
        }
        else if (rb.velocity.y < 0 && moveInput.y < 0)
        {
            //Much higher gravity if holding down
            rb.gravityScale = data.gravityScale * data.fastFallGravityMult;
            //Caps maximum fall speed
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            //Higher gravity if jump button released
            rb.gravityScale = data.gravityScale * data.jumpCutGravityMult;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFallSpeed));
        }
        else if (isFallingAfterRocketJump)
        {
            rb.gravityScale = data.gravityScale * data.rocketJumpCutGravityMult;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFallSpeed));
        }
        else if ((isJumping || isWallJumping || isRocketJumping || isFallingAfterJump) && Mathf.Abs(rb.velocity.y) < data.jumpHangTimeThreshold)
        {
            rb.gravityScale = data.gravityScale * data.jumpHangGravityMult;
        }
        else if (rb.velocity.y < -2)
        {
            //Higher gravity if falling
            rb.gravityScale = data.gravityScale * data.fallGravityMult;
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -data.maxFallSpeed));
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            rb.gravityScale = data.gravityScale;
        }
    }

    void FixedUpdate()
    {
        if (debugMode)
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            transform.Translate(moveInput * Time.deltaTime * 20);
        }
        else
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        }

        if (isWallJumping)
        {
            Run(data.wallJumpRunLerp);
        }
        else
        {
            Run(1);
        }
        if(isSliding == true)
        {
            Slide();
        }
    }

    private void Checks()
    {
        if (!isJumping)
        {
            //Ground Check
            if (Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer))
            {
                onGroundTimer = data.coyoteTime;
            }
            //Right Wall Check
            if (Physics2D.OverlapBox(rightWallCheckPoint.position, wallCheckSize, 0, groundLayer))
            {
                onWallRightTimer = data.coyoteTime;
            }
            //Left Wall Check
            if (Physics2D.OverlapBox(leftWallCheckPoint.position, wallCheckSize, 0, groundLayer))
            {
                onWallLeftTimer = data.coyoteTime;
            }
            //Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
            onWallTimer = Mathf.Max(onWallLeftTimer, onWallRightTimer);
            //Airborne or not

        }
        if (isJumping && rb.velocity.y <= 0)
        {
            isJumping = false;
            if (!isWallJumping)
            {
                isFallingAfterJump = true;
            }
        }
        if (isRocketJumping && rb.velocity.y < 0)
        {
            isRocketJumping = false;

            if (!isWallJumping)
            {
                isFallingAfterRocketJump = true;
            }
        }
        if (onGroundTimer >0)
        {
            numberOfAirRocketJumps = 0;
        }
        if (onGroundTimer > 0 && !isJumping && !isRocketJumping && !isWallJumping)
        {
            isJumpCut = false;
            isFallingAfterJump = false;
            isFallingAfterRocketJump = false;
        }
        if (isWallJumping && Time.time - wallJumpStartTime > data.wallJumpTime)
        {
            isWallJumping = false;
        }
        if (CanSlide() && ((onWallLeftTimer > 0 && moveInput.x < 0) || (onWallRightTimer > 0 && moveInput.x > 0)))
        {
            isSliding = true;
        }
        else
        {
            isSliding = false;
        }
       

    }

    private void Inputs()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        if (moveInput.x != 0)
        {
            if (moveInput.x > 0)
            {
                //GetComponent<SpriteRenderer>().sprite = faceRight;
                isFacingRight = true;
            }        
            else if (moveInput.x < 0)
            {
                //GetComponent<SpriteRenderer>().sprite = faceLeft;
                isFacingRight = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpKeyDown();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            JumpKeyUp();
        }
    }
    private void Run(float lerpAmount)
    {
        //Velocity we want
        float targetSpeed = moveInput.x * data.runMaxSpeed;
        //Smoothing direction change
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);
        float accelRate;
        //Gets an acceleration value based on if we are accelerating (includes turning) 
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (onGroundTimer > 0)
        {
            //If target speed is over 0.01f, accel rate = accelAmount else accel rate = deccelAmount
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        }
        else
        {
            //Same thing but with air multipliers
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.runAccelAmount * data.accelInAirMulti : data.runDeccelAmount * data.deccelInAirMulti;
        }

        //Increase acceleration and maxSpeed when at the apex of their jump (bounce + natural)
        if ((isJumping|| isRocketJumping || isWallJumping || isFallingAfterJump) && Mathf.Abs(rb.velocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult; //Increase accel
            targetSpeed *= data.jumpHangMaxSpeedMult; //Increase max speed
        }

        //Don't slow the player down if they are moving over maxSpeed
        if (data.doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && onGroundTimer < 0)
        {
            accelRate = 0;
        }

        //Difference between current velocity and desired velocity
        float speedDiff = targetSpeed - rb.velocity.x;
        //Force to apply to player

        float movement = speedDiff * accelRate;

        //Apply to rigidbody
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        if (Mathf.Abs(movement) > 10f)
        {
            //playerAnim.SetBool("walking", true);
        }
        else
        {
            //playerAnim.SetBool("walking",false);
        }
    }
    void Slide()
    {
        float speedDiff = data.slideSpeed - rb.velocity.y;
        float movement = speedDiff * data.slideAccel;
        //So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
        //The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDiff) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDiff) * (1 / Time.fixedDeltaTime));
        rb.AddForce(movement * Vector2.up);
    }
    private void WallJump(int dir)
    {
        //Ensures we can't call Wall Jump multiple times from one press
        jumpInputKeepTimer = 0;
        onGroundTimer = 0;
        onWallRightTimer = 0;
        onWallLeftTimer = 0;

        Vector2 force = new Vector2(data.wallJumpForce.x, data.wallJumpForce.y);
        force.x *= dir; //apply force in opposite direction of wall

        if (Mathf.Sign(rb.velocity.x) != Mathf.Sign(force.x))
            force.x -= rb.velocity.x;

        if (rb.velocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
            force.y -= rb.velocity.y;

        //Unlike in the run we want to use the Impulse mode.
        //The default mode will apply are force instantly ignoring masss
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    private void JumpKeyDown()
    {
       jumpInputKeepTimer = data.jumpInputMemory;
    }
    private void JumpKeyUp()
    {
        if (CanJumpCut() || CanWallJumpCut())
        {
            isJumpCut = true;
        }
    }    
    bool CanJump()
    {
        return onGroundTimer > 0 && isJumping == false;
    }
    private bool CanWallJump()
    {
        return jumpInputKeepTimer > 0 && onWallTimer > 0 && onGroundTimer <= 0 && (!isWallJumping || (onWallRightTimer > 0 && lastWallJumpDir == 1) || (onWallLeftTimer > 0 && lastWallJumpDir == -1));
    }
    private bool CanJumpCut()
    {
        return isJumping && rb.velocity.y > 0;
    }

    private bool CanWallJumpCut()
    {
        //return isWallJumping && rb.velocity.y > 0;
        return false;
    }
    /*private bool CanRocketJumpCut()
    {
        return isRocketJumping && rb.velocity.y > 0;
    }*/
    bool CanSlide()
    {
        if (onWallTimer > 0 && !isJumping && !isWallJumping && onGroundTimer <=0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private void Jump()
    {

        //Ensures we can't call Jump multiple times from one press
        jumpInputKeepTimer = 0;
        onGroundTimer = 0;

        //We increase the force applied if we are falling
        //This means we'll always feel like we jump the same amount 
        //(setting the player's Y velocity to 0 beforehand will likely work the same, but I find this more elegant :D)
        float force = data.jumpForce;
        if (rb.velocity.y < 0)
        {
            force -= rb.velocity.y;
        }
        if (rb.velocity.y > 0)
        {
            force -= rb.velocity.y;

        }
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        //playerAnim.SetTrigger("jump");
    }
    /*public void Bounce()
    {
        jumpInputKeepTimer = 0;
        onGroundTimer = 0;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        float force=40; 
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        playerAnim.SetTrigger("jump");
    }*/
    /*public void RocketJump(Vector2 direction)
    {
        print(direction);
        isJumping = false;
        isRocketJumping = true;
        isWallJumping = false;
        isJumpCut = false;
        isFallingAfterJump = false;
        isFallingAfterRocketJump = false;
        onGroundTimer = 0;
        Vector2 force = new Vector2(data.rocketJumpForce, data.rocketJumpForce);
        if (rb.velocity.y < 0)
        {
            force.y -= rb.velocity.y;

        }
        if (rb.velocity.y > 0)
        {
            force.y -= rb.velocity.y;

        }
        if (numberOfAirRocketJumps > 0)
        {
            force -= new Vector2(data.rocketJumpHeightDecay, data.rocketJumpHeightDecay)*numberOfAirRocketJumps;
        }
        numberOfAirRocketJumps++;
        rb.AddForce(-direction * force, ForceMode2D.Impulse);
        playerAnim.SetTrigger("jump");
    }*/
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(rightWallCheckPoint.position, wallCheckSize);
        Gizmos.DrawWireCube(leftWallCheckPoint.position, wallCheckSize);
    }
}
