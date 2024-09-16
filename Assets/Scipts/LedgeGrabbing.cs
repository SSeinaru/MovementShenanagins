using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeGrabbing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform cam;
    private PlayerMovement pm;
    private Rigidbody rb;

    [Header("Ledge Grabbing")]
    [SerializeField] private float moveToLedgeSpeed;
    [SerializeField] private float maxLedgeGrabDistance;

    [SerializeField] private float minTimeOnLedge;
    private float timeOnLedge;

    [Header("Ledge Jumping")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float ledgeJumpForwardForce;
    [SerializeField] private float ledgeJumpUpForce;

    [Header("Ledge Detection")]
    [SerializeField] private float ledgeDetectionLength; 
    [SerializeField] private float ledgeSphereCastRadius;
    [SerializeField] private LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform CurrentLedge;

    private RaycastHit ledgeHit;

    [Header("Exiting Ledge")]
    [SerializeField] public bool exitingLedge;
    [SerializeField] private float exitLedgeTime;
    private float exitLedgeTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        LedgeDetection();
        SusStateMachine();
    }

    private void SusStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float VerticalInput = Input.GetAxisRaw("Vertical");

        bool anyInputKeyPressed = horizontalInput != 0 || VerticalInput != 0;

        //SubState 1
        if (pm.holding)
        {
            FreezeRigidBodyOnLedge();

            timeOnLedge += Time.deltaTime;

            if(timeOnLedge > minTimeOnLedge && anyInputKeyPressed)
            {
                ExitLedgeHold();
            }

            if (Input.GetKeyDown(jumpKey))
                LedgeJump();
            else if (exitingLedge)
            {
                if (exitLedgeTimer > 0) exitLedgeTimer -= Time.deltaTime;
                else exitingLedge = false;
            }
        }
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);

        if(!ledgeDetected)
        return;

        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);

        if (ledgeHit.transform == lastLedge)
            return;

        if (distanceToLedge < maxLedgeGrabDistance && !pm.holding) 
            EnterLedgeHold();
    }

    private void LedgeJump()
    {
        ExitLedgeHold();

        Invoke(nameof(LedgeJumpDelay), 0.05f);
    }
    private void LedgeJumpDelay()
    {
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpForce;
        rb.velocity = Vector3.zero;

        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }

    private void EnterLedgeHold()
    {
        pm.holding = true;

        pm.unlimited = true;
        pm.restricted = true;

        CurrentLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    private void FreezeRigidBodyOnLedge()
    {
        rb.useGravity = false;

        Vector3 directionToLedge = CurrentLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, CurrentLedge.position);

        if( distanceToLedge > 1f)
        {
            if(rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);
        }
        else
        {
            if (!pm.freeze)
                pm.freeze = true;
            if (pm.unlimited)
                pm.unlimited = false;
        }

        if (distanceToLedge > maxLedgeGrabDistance)
            ExitLedgeHold();

    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        pm.holding = false;

        timeOnLedge = 0f;

        pm.restricted = false;
        pm.freeze = false;

        rb.useGravity = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }

}
