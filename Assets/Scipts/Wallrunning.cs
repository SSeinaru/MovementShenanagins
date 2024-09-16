using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Wallrunning : MonoBehaviour
{
    [Header("Wallrunning")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;
    [SerializeField] private float wallClimbSpeed;
    [SerializeField] private float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode upwardsRunKey = KeyCode.LeftShift;
    [SerializeField] KeyCode downwardsRunKey = KeyCode.LeftControl;
    private bool upwardRunning;
    private bool downwardRunning;
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Exiting")]
    private bool exitingWall;
    [SerializeField] private float exitWallTime;
    private float exitWallTimer;

    [Header("Gravity")]
    [SerializeField] private bool useGravity;
    [SerializeField] private float gravityCounterForce;

    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private LookAround cam;
    private LedgeGrabbing lg;
    private PlayerMovement pm;
    private Rigidbody rb;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        lg = GetComponent<LedgeGrabbing>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if(pm.wallRunning)
            WallRunMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardRunning = Input.GetKey(upwardsRunKey);
        downwardRunning = Input.GetKey(downwardsRunKey);

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallRunning)
                StartWallRun();

            if (wallRunTimer > 0)
            {
                wallRunTimer -= Time.deltaTime;
            }

            if (wallRunTimer <= 0)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKey(jumpKey))
                WallJump();
        }

        else if (exitingWall)
        {
            if (pm.wallRunning)
                StopWallRun();
            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0)
                exitingWall = false;
        }
        else
        {
            if (pm.wallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        pm.wallRunning = true;

        wallRunTimer = maxWallRunTime;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        cam.DoFov(100f);
        if (wallLeft)
            cam.DoTilt(-5);
        else if (wallRight)
            cam.DoTilt(5);
    }

    private void WallRunMovement()
    {
        rb.useGravity = useGravity;



        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal ;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (upwardRunning)
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);
        if (downwardRunning)
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);

        
        if (!(wallLeft && horizontalInput > 0) && (wallRight && horizontalInput < 0))
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        if(useGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        pm.wallRunning = false;

        cam.DoFov(90f);
        cam.DoTilt(0f);
    }
    private void WallJump()
    {
        if (pm.holding || lg.exitingLedge) return;

        exitingWall = true;
        exitWallTimer = exitWallTime;


        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }

}
