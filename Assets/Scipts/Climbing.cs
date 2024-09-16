using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    private Rigidbody rb;
    private PlayerMovement pm;
    private LedgeGrabbing lg;
    [SerializeField] private LayerMask whatIsWall;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed;
    [SerializeField] private float maxClimbTime;
    private float climbTimer;

    [Header("ClimbJumping")]
    [SerializeField] private float climbJumpUpForce;
    [SerializeField] private float climbJumpBackForce;

    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] int climbJumps;
    private int climbJumpsLeft;

    [Header("Detection")]
    [SerializeField] private float detectionLength;
    [SerializeField] private float sphereCastRadius;
    [SerializeField] private float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    [SerializeField] private float minWallNormalAngleChange;

    [Header("Exiting")]
    [SerializeField] private bool exitingWall;
    [SerializeField] private float exitWallTime;
    private float exitWallTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        lg = GetComponent<LedgeGrabbing>();
    }

    private void Update()
    {
        Wallcheck();
        StateMachine();

        if (pm.climbing && !exitingWall)
            ClimbMovement();
    }

    private void Wallcheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if ((wallFront && newWall) || pm.grounded)
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void StateMachine()
    {
        if (pm.holding)
        {
            if (pm.climbing) StopClimbing();
        }
        if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!pm.climbing && climbTimer > 0)
                startclimbing();

            if (climbTimer > 0)
                climbTimer -= Time.deltaTime;

            if (climbTimer < 0)
                StopClimbing();

        }
        else if (exitingWall) 
        {
            if (pm.climbing)
                StopClimbing();
            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0) 
                exitingWall = false;
        }

        else
        {
            if (pm.climbing)
                StopClimbing();
        }

        if (wallFront && Input.GetKey(jumpKey) && climbJumpsLeft > 0)
            ClimbJump();
    }

    private void startclimbing()
    {
        pm.climbing = true;

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
    }
    private void ClimbMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
    }
    private void StopClimbing()
    {
        pm.climbing = false;
    }

    private void ClimbJump()
    {
        if (pm.grounded) return;
        if (pm.holding || lg.exitingLedge) return;

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;
        
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        climbJumpsLeft--;
    }
}
