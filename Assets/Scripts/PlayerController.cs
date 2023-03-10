using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PlayerController : MonoBehaviour
{
    //isGrounded is used by the jumpTrigger Script
    public bool isGrounded;
    

    public float acceleration;
    public float maxSpeed;
    public float jumpStrength;
    public float sprintMultiplier;
    public float crouchMultiplier;

    GameObject playerCamera;
    Rigidbody rb;
    Camera PlayerCameraComponent;
    float accelerationBaseValue;
    float maxSpeedBaseValue;
    float moveLeftRight;
    float moveForwardBackward;
    float noise;
    float timeStamp;

    bool upPressed;
    bool downPressed;
    bool rightPressed;
    bool leftPressed;
    bool sprintPressed;
    bool jumpPressed;
    bool crouchPressed;

    bool disableRegularForce;


    // Start is called before the first frame update
    void Start()
    {
        //isGrounded is used by the jumpTrigger Script
        isGrounded = true;

        rb = GetComponent<Rigidbody>();
        playerCamera = transform.GetChild(0).gameObject;
        PlayerCameraComponent = playerCamera.GetComponent<Camera>();

        accelerationBaseValue = acceleration;
        maxSpeedBaseValue = maxSpeed;

        noise = 0.5f;

        

        upPressed = false;
        downPressed = false;
        rightPressed = false;
        leftPressed = false;
        sprintPressed = false;
        jumpPressed = false;
        crouchPressed = false;

        disableRegularForce = false;
    }


    private void Update()
    {
        //grabs player input and stores it
        if (Input.GetKey(KeyCode.W))
            upPressed = true;

        if (Input.GetKey(KeyCode.S))
            downPressed = true;

        if (Input.GetKey(KeyCode.D))
            rightPressed = true;

        if (Input.GetKey(KeyCode.A))
            leftPressed = true;

        if (Input.GetKey(KeyCode.LeftShift))
            sprintPressed = true;

        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            crouchPressed = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //set noise to default value (walking)
        noise = 0.7f;

        //resets drag/gravity incase anything changed them
        rb.drag = 1;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;

        disableRegularForce = false;
        
        //set acceleration and max speed back to normal after sprint
        acceleration = accelerationBaseValue;
        maxSpeed = maxSpeedBaseValue;

        //If sprint is input, up both accelertion and speed as well as change fov
        if (sprintPressed && !crouchPressed && (upPressed || leftPressed || rightPressed))
        {
            sprintPressed = false;
            noise = 1f;
            maxSpeed = maxSpeed * sprintMultiplier;
            acceleration = acceleration * sprintMultiplier;
            //PlayerCameraComponent.fieldOfView = Mathf.Lerp(PlayerCameraComponent.fieldOfView, 70, Time.deltaTime * 2);
        }
        else
            //PlayerCameraComponent.fieldOfView = Mathf.Lerp(PlayerCameraComponent.fieldOfView, 60, Time.deltaTime * 10);

        //If crouch is input, lower the camera and Shorten the Collider of the player
        if (isGrounded && crouchPressed)
        {
            crouchPressed = false;

            //reduces player and camera height
            playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, Mathf.Lerp(playerCamera.transform.position.y, transform.position.y + 0.2f, Time.deltaTime * 10), playerCamera.transform.position.z);
            transform.GetComponent<CapsuleCollider>().height = 1;
            transform.GetComponent<CapsuleCollider>().center = new Vector3(0, -0.5f, 0);

            if (Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)) > 4 && upPressed)
            {
                if (timeStamp == 0)
                    timeStamp = Time.time;

                Debug.Log(timeStamp);

                noise = 0.4f;

                //disable any deceleration
                disableRegularForce = true;

                if (timeStamp + 0.7 > Time.time)
                {
                    rb.drag = 0;
                    rb.angularDrag = 0;
                    rb.useGravity = false;
                }

                maxSpeed = maxSpeed * sprintMultiplier * 1.2f;

                if (Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)) < maxSpeed || timeStamp + 0.7 > Time.time)
                    rb.AddForce(rb.velocity);

            }
            else
            {
                noise = 0.2f;
                maxSpeed = maxSpeed * crouchMultiplier;
                acceleration = acceleration * crouchMultiplier;
            }

        }
        else
        {
            timeStamp = 0;
            playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, Mathf.Lerp(playerCamera.transform.position.y, transform.position.y + 0.6f, Time.deltaTime * 10), playerCamera.transform.position.z);
            transform.GetComponent<CapsuleCollider>().height = 2;
            transform.GetComponent<CapsuleCollider>().center = Vector3.zero;
        }

        //Bug Fix
        if (sprintPressed && crouchPressed)
        {
            sprintPressed = false;
            crouchPressed = false;
        }

        //Resets force Adding Variables to zero for the next add force
        moveForwardBackward = 0;
        moveLeftRight = 0;

        //turns WASD Input into the Add Force Variables 
        if (upPressed)
        {
            moveForwardBackward += 1;
            upPressed = false;
        }
        if (downPressed)
        {
            moveForwardBackward -= 1;
            downPressed = false;
        }
        if (rightPressed)
        {
            moveLeftRight += 1;
            rightPressed = false;
        }
        if (leftPressed)
        {
            moveLeftRight -= 1;
            leftPressed = false;
        }

        if (!disableRegularForce)
        {
            
            //Uses the Add force Variables to Add force as well as capping the speed
            if (Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)) < maxSpeed)
                rb.AddRelativeForce(new Vector3(moveLeftRight, 0, moveForwardBackward).normalized * Time.deltaTime * 1000 * acceleration);

            //Increases deceleration to prevent sliding
            rb.velocity = new Vector3 (rb.velocity.x * 0.9f, rb.velocity.y, rb.velocity.z * 0.9f);
        }


        //if the player is on the ground and pressed jump then add force to the y for a jump
        if (jumpPressed && isGrounded)
            rb.AddForce(new Vector3(0.0f, jumpStrength, 0.0f), ForceMode.Impulse);

        jumpPressed = false;

        //if standing still make "no noise"
        if ((rb.velocity.x < 0.1f && rb.velocity.x > -0.1f) && (rb.velocity.z < 0.1f && rb.velocity.z > -0.1f) || !isGrounded)
            noise = 0.05f;

        Debug.Log(Mathf.Clamp01(((float)Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)) - 3) / ((maxSpeedBaseValue * sprintMultiplier * 1.2f) - 3)));

        PlayerCameraComponent.fieldOfView = Mathf.Lerp(PlayerCameraComponent.fieldOfView, 60 + (15 * Mathf.Clamp01(((float) Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)) - 2) / ((maxSpeedBaseValue * sprintMultiplier * 1.2f) - 2))), Time.deltaTime * 2);
    

        //DEBUG: check total x y speed
        //Debug.Log(Math.Sqrt(Math.Pow(rb.velocity.x, 2) + Math.Pow(rb.velocity.z, 2)));

        //DEBUG: check Noise level
        //Debug.Log(noise);
    }

    //Takes a Y rotation and sets the player's camera to that direction
    public void RecenterCamera(float yRotation = 0)
    {
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        playerCamera.GetComponent<CameraController>().xRotation = 0;
    }

    //gives player postition but helps account for crouching
    public Vector3 GetPosition()
    {
        if (isGrounded && crouchPressed && !sprintPressed)
            return (transform.position + new Vector3(0, -0.5f, 0));
        else
            return (transform.position + new Vector3(0, 0.5f, 0));
    }

    //just gives noise value
    public float GetNoise()
    {
        return noise;
    }
}
