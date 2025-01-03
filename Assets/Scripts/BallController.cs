using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    public float moveForce = 10f;
    public float maxSquishTime = 2f;
    public float jumpForce = 10f;
    public float squishFactor = 0.5f;
    public Transform cameraTransform;
    public float scaleRecoverySpeed = 2f;
    public float airControlFactor = 0.5f;
    public float fallMultiplier = 2.5f;
    public float minJumpForce = 30f;
    public float maxJumpForce = 100f;

    public Transform currentPlatform;
    private Rigidbody rb;
    private bool isChargingJump;
    private float squishStartTime;
    public Vector3 originalScale;
    private BallInput ballInput;
    private Vector2 movementInput;
    private bool isRecoveringScale;
    private List<RaycastHit> groundHits = new List<RaycastHit>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        ballInput = new BallInput();

        ballInput.PogoControls.Jump.started += OnJumpStarted;
        ballInput.PogoControls.Jump.canceled += OnJumpCanceled;
        ballInput.PogoControls.Move.performed += OnMovementPerformed;
        ballInput.PogoControls.Move.canceled += OnMovementCanceled;
    }

    private void OnEnable() => ballInput.Enable();
    private void OnDisable() => ballInput.Disable();

    private void Update()
    {
        HandleMovement();
        HandleScaleSquish();
        HandleScaleRecovery();
        HandleAirborne();
        HandleDrag();
    }

    private void HandleMovement()
    {
        Vector3 direction = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 moveDir = direction.z * cameraForward + direction.x * cameraRight;
            float controlFactor = IsGrounded() ? 1f : airControlFactor;
            rb.AddForce(moveDir * moveForce * controlFactor);
        }
    }

    private void HandleScaleSquish()
    {
        if (isChargingJump)
        {
            float squishTime = Mathf.Clamp(Time.time - squishStartTime, 0, maxSquishTime);
            float squishAmount = 1 - (squishTime / maxSquishTime) * (1 - squishFactor);
            transform.localScale = originalScale * squishAmount;
        }
    }

    private void HandleScaleRecovery()
    {
        if (isRecoveringScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * scaleRecoverySpeed);
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
                transform.localScale = originalScale;
                isRecoveringScale = false;
            }
        }
    }

    private void HandleAirborne()
    {
        if (!IsGrounded())
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    private void HandleDrag()
    {
        rb.drag = IsGrounded() ? 1f : 0f;
        rb.angularDrag = IsGrounded() ? 1f : 0.05f;
    }

    private void Jump()
    {
        if (IsGrounded())
        {
            Vector3 combinedNormal = Vector3.zero;
            foreach (var hit in groundHits)
            {
                combinedNormal += hit.normal;
            }
            combinedNormal.Normalize();

            float squishTime = Mathf.Clamp(Time.time - squishStartTime, 0, maxSquishTime);
            float appliedJumpForce = Mathf.Clamp(jumpForce * (squishTime / maxSquishTime), minJumpForce, maxJumpForce);

            Vector3 jumpDirection = combinedNormal * appliedJumpForce;

            // Add platform velocity if on a moving platform
            if (currentPlatform)
            {
                ObjectMovementController platformController = currentPlatform.GetComponent<ObjectMovementController>();
                if (platformController != null)
                {
                    jumpDirection += platformController.Velocity;
                }
            }

            rb.AddForce(jumpDirection, ForceMode.Impulse);
        }
    }
    
    private bool IsGrounded()
    {
        groundHits.Clear();
        int rayCount = 50; // Antall stråler
        float radius = 3f;

        // Fibonacci-sfærisk fordeling for jevnt fordelte punkter på en kule
        for (int i = 0; i < rayCount; i++)
        {
            float phi = Mathf.Acos(1 - 2 * (i + 0.5f) / rayCount); // Høydevinkel
            float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i; // Azimuthal vinkel

            Vector3 direction = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                Mathf.Cos(phi),
                Mathf.Sin(phi) * Mathf.Sin(theta)
            );

            Debug.DrawRay(transform.position, direction * radius, Color.green);
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, radius))
            {
                if (!groundHits.Exists(h => h.collider == hit.collider))
                {
                    groundHits.Add(hit);
                }
            }
        }

        return groundHits.Count > 0;
    }


    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        isChargingJump = true;
        squishStartTime = Time.time;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (isChargingJump)
        {
            if (IsGrounded())
            {
                Jump();
            }
            isChargingJump = false;
            isRecoveringScale = true;
        }
    }
    
    private void OnMovementPerformed(InputAction.CallbackContext context) => movementInput = context.ReadValue<Vector2>();
    private void OnMovementCanceled(InputAction.CallbackContext context) => movementInput = Vector2.zero;
} 
