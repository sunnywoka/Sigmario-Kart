using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Challenge_Advanced_PlayerController : MonoBehaviour
{
    //Reference to the CharacterController component
    public CharacterController controller;

    //Reference to the Animator component
    public Animator animator;

    //Current Player Speed
    public float m_speed = 10;

    //Walk speed
    public float m_walkSpeed = 2;

    //Run speed
    public float m_runSpeed = 6;

    //Crouch speed
    public float m_crouchSpeed = 2;

    //Crawl speed
    public float m_crawlSpeed = 1;

    //Vertical speed
    public float m_speedY;

    //Jump height
    public float m_jumpHeight = 10;

    //Used for more controlled jump
    public float m_jumpControlAmount = 2f;

    //Current Speed at which player smoothly rotates
    public float m_turnSpeed;

    //Movement direction of the player
    private Vector3 movement, movementJump;

    //Player input
    private Vector3 inputDirection;

    //Gravity
    private const float GRAVITY = 9.87f;

    //Gravity stick constant
    private const float GRAVITY_STICK = 0.3f;

    //Reference to the camera
    private Transform playerCam;

    //Player state
    public enum MoveState
    {
        Idle,
        Walk,
        Crouch,
        Crawl,
        Run,
        Jump
    };

    //Current player state
    [SerializeField] MoveState m_moveState;

    //Smoothing for changing between speeds (can modify later with acceleration and decceleration for more control)
    [SerializeField] private float smoothSpeed;

    void Start()
    {
        //Fetch the CharacterController component 
        controller = GetComponent<CharacterController>();

        //Fetch the Animator component
        animator = GetComponent<Animator>();

        //Fetch the Main camera and store its reference
        //Caching this reference is extremely cheap since Camera.main uses Gameobject.Find 
        //Which is very expensive if used in Update
        playerCam = Camera.main.transform.parent;

        m_moveState = MoveState.Idle;
    }

    void Update()
    {   

        //Update current player state
        UpdatePlayerState();

        //Left-Right, Forward-Backward movement
        UpdateHorizontalMovement();

        //Jump 
        UpdateVerticalMovement();

        //Rotate Face in the movement direction
        UpdateRotation();

        //Move in the given input direction and in the jump direction if there is any
        controller.Move(movement * Time.deltaTime + movementJump * Time.deltaTime);
    }


   

    //Updates the rotation of player to face in the movement direction
    private void UpdateRotation()
    {
        //If we have some input, then only rotate other wise preserve the rotation
        if (inputDirection != Vector3.zero)            
        {
           
            //Calcuate the angle required to rotate based on the given direction
            float turnAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;

            //Convert the angle into a Quaternion (Data Type used by many game engines for rotation)
            //It consists of 4 dimensions: 3 imaginary numbers (x , y , z) and a real number (w) *Notations can differ
            Quaternion targetRotation = Quaternion.Euler(0 , turnAngle, 0);

            //Smoothly Interpolate from current rotation to target rotation with the given turn speed amount 
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_turnSpeed * Time.deltaTime);      
                    
        }
    }

    //Updates the L-R-F-B movement
    private void UpdateHorizontalMovement()
    {
       
        //Get the input and normalize
        inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0 , Input.GetAxisRaw("Vertical")).normalized;
   
        //We want to preserve and modify the movement.y value when there is jump input, 
        //hence we only assign individual xz components
        //movement.x = inputDirection.x * m_speed;
        //movement.z = inputDirection.z * m_speed;

        //With this previous code, our movement.y will always be 0
        //movement = inputDirection.normalized * m_speed;

      
        //Camera forward (Player Forward), Camera right (Player right)
        movement = (playerCam.forward * inputDirection.z + playerCam.right * inputDirection.x) * m_speed;

        //Play the movement animation
        //This will take care of both the idle and run animations since for idle speed will 0 when there is no input
        animator.SetFloat("speed", m_speed);
       
    }


    //Handles the jumping functionality
    private void UpdateVerticalMovement()
    {
        //Player is on ground, then only check for jump input
        if(controller.isGrounded)
        {
            //Common hack used to make sure the player is always sticked to the ground when on ground
            //Sometimes, isgrounded does not work correctly and the player keeps floating in air
           m_speedY = -GRAVITY * GRAVITY_STICK;
        
           animator.SetBool("jump", false);

            //If we press space and not already jumping and if we are not crawling or crouching, then jump
            if(Input.GetKeyDown(KeyCode.Space) && !animator.GetBool("jump") && 
            m_moveState != MoveState.Crouch && m_moveState != MoveState.Crawl) 
            {
                //Simple jump
                m_speedY = m_jumpHeight;

                //For constant speed while jumping
                //m_speed = m_runSpeed;

                //More Controlled jump (proportionate to our defined gravity value)
                //movement.y += Mathf.Sqrt(m_jumpHeight * m_jumpControlAmount * GRAVITY);

                animator.SetBool("jump", true);

                m_moveState = MoveState.Jump;
                
       
            }

             
        }
        //Player is airborne
        else
        {   
            //Apply gravity so that the player falls back to the ground
           m_speedY -= GRAVITY * Time.deltaTime;
           
        }      

        movementJump.y = m_speedY;
        
    }


    //Updates current player state
    private void UpdatePlayerState()
    {
        //If grounded 
        if(controller.isGrounded)
        {
            //If we have some input then check for Run or Walk states
            if(inputDirection != Vector3.zero)
            {
            
                //Walk when key pressed
                if(Input.GetKey(KeyCode.LeftShift))
                {
                    m_speed = Mathf.MoveTowards(m_speed, m_walkSpeed, smoothSpeed * Time.deltaTime);
                    m_moveState = MoveState.Walk;
                }

                //Run if not currently crawling or crouching
                else if(m_moveState != MoveState.Crawl && m_moveState != MoveState.Crouch)
                {
                    m_speed = Mathf.MoveTowards(m_speed, m_runSpeed, smoothSpeed * Time.deltaTime);
                    m_moveState = MoveState.Run;                   
                }

                //Switch to Run state if coming from Crouch or Crawl
                if(m_moveState != MoveState.Walk && m_moveState != MoveState.Jump)
                    m_moveState = MoveState.Run; 
            }

            //Idle when no input
            else
            {
                m_speed = Mathf.MoveTowards(m_speed, 0, smoothSpeed * Time.deltaTime);
                m_moveState = MoveState.Idle;
            }
        

        
            //Check for Crouch 
            if(Input.GetKey(KeyCode.C))
            {   
                //Crouch moving or idle?
                if(inputDirection != Vector3.zero)
                    m_speed = Mathf.MoveTowards(m_speed, m_crouchSpeed, smoothSpeed * Time.deltaTime);
                else
                    m_speed = Mathf.MoveTowards(m_speed, 0, smoothSpeed * Time.deltaTime);

                animator.SetBool("crouch", true);
    
                
                m_moveState = MoveState.Crouch;

            }

            else
            {
                animator.SetBool("crouch", false);
            }


            //Check for Crawl 
            if(Input.GetKey(KeyCode.LeftControl))
            {
                //Crawl moving or idle?
                if(inputDirection != Vector3.zero)
                    m_speed = Mathf.MoveTowards(m_speed, m_crawlSpeed, smoothSpeed * Time.deltaTime);
                else
                    m_speed = Mathf.MoveTowards(m_speed, 0, smoothSpeed * Time.deltaTime);

                animator.SetBool("crawl", true);
            
                m_moveState = MoveState.Crawl;

            }

            else
            {
                animator.SetBool("crawl", false);
            }
   
        }
        
    }
}
