using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PogoController : MonoBehaviour
{
    public Transform cameraTransform;
    
    public float angleSpeed = 50f;
    public float angleSpeedCentering = 100f;
    public float maxJumpForce = 1000f;
    public float normalizedJumpCharge;
    public float maxChargeTimer = 1f;
    public float minJumpForce = 200f;
    public float repeatedJumpTimeLimit = 0.2f;
    public float compressionFactor = 0.7f;
    public float decompressionMultiplier = 5f;
    
    private Vector3 _normalScale;
    private float _jumpCharge = 0f;
    private float _previousJumpCharge = 0f;
    private bool _isCharging = false;
    private Rigidbody _rb;
    private PogoInput _pogoInput;
    private Vector2 _angleInput;
    private bool _isCentering = false;
    private bool _isGrounded = false;
    private float _groundedTime;
    private bool _canJump = true;
    
    Quaternion _originalRotation;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _pogoInput = new PogoInput();
        
        _pogoInput.PogoControls.Jump.started += OnJump;
        _pogoInput.PogoControls.Jump.canceled += OnJump;
        _pogoInput.PogoControls.Centre.started += _ => _isCentering = true;
        _pogoInput.PogoControls.Centre.canceled += _ => _isCentering = false;
        
        _originalRotation = transform.rotation;
        _normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _pogoInput.PogoControls.Enable();
    }

    private void OnDisable()
    {
        _pogoInput.PogoControls.Disable();
    }

    private void FixedUpdate()
    {
        if (_isCentering)
        {
            // For centering the rotation (returning to upright position)
            float step = angleSpeedCentering * Time.deltaTime;
            float newX = Mathf.MoveTowardsAngle(transform.eulerAngles.x, 0, step);
            float newZ = Mathf.MoveTowardsAngle(transform.eulerAngles.z, 0, step);

            // Set Y rotation to match the camera (camera's Y rotation)
            float newYRotation = cameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(newX, newYRotation, newZ);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * angleSpeed);
        }
        else
        {
            // Read WASD input values
            Vector2 movementInput = _pogoInput.PogoControls.AdjustAngle.ReadValue<Vector2>();

            // Calculate rotation deltas based on input (WASD for X and Z axis)
            float deltaX = movementInput.y * angleSpeed * Time.deltaTime;  // Forward/backward (W/S for pitch)
            float deltaZ = -movementInput.x * angleSpeed * Time.deltaTime; // Left/right (A/D for roll)

            // Convert current rotation to local Euler angles
            Vector3 localEulerAngles = transform.localRotation.eulerAngles;

            // Normalize the Euler angles to avoid wrapping issues
            float currentX = localEulerAngles.x > 180f ? localEulerAngles.x - 360f : localEulerAngles.x;
            float currentZ = localEulerAngles.z > 180f ? localEulerAngles.z - 360f : localEulerAngles.z;

            // Apply deltas and clamp rotations
            float newX = Mathf.Clamp(currentX + deltaX, -80f, 80f);
            float newZ = Mathf.Clamp(currentZ + deltaZ, -80f, 80f);

            // Get mouse-controlled Y rotation (camera's Y rotation)
            float newY = cameraTransform.eulerAngles.y;

            // Apply the new rotation while maintaining clamped X and Z values
            Quaternion targetRotation = Quaternion.Euler(newX, newY, newZ);

            // Use Rigidbody to apply rotation
            _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * angleSpeed));
        }

        
        // Handle jump charging and scaling
        if (_isCharging)
        {
            float targetYScale = _normalScale.y * compressionFactor;

            // Move only the y scale towards target at a linear rate
            float compressionStep = (1f - compressionFactor) / maxChargeTimer * Time.deltaTime;
            float newYScale = Mathf.MoveTowards(transform.localScale.y, targetYScale, compressionStep);
            transform.localScale = new Vector3(_normalScale.x, newYScale, _normalScale.z);

            // Calculate and update jump charge at a linear rate
            float chargeRate = (maxJumpForce - minJumpForce) / maxChargeTimer;
            _jumpCharge = Mathf.Min(_jumpCharge + Time.deltaTime * chargeRate, maxJumpForce);
            normalizedJumpCharge = Mathf.Clamp01(_jumpCharge / maxJumpForce);
        }
        else
        {
            var targetScale = _normalScale;
            float decompressionSpeed = decompressionMultiplier / maxChargeTimer;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * decompressionSpeed);
        }
        
        _isGrounded = Physics.Raycast(transform.position + new Vector3(0,0.1f,0), Vector3.down, 1f);
        Debug.DrawRay(transform.position, Vector3.down * 1f, _isGrounded ? Color.white : Color.black);

        if (!_isGrounded)
        {
            _groundedTime = 0f;
        }
        else
        {
            _groundedTime += Time.deltaTime;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            _isCharging = true;
            _jumpCharge = minJumpForce;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            _isCharging = false;
            Jump(false);
        }
    }

    private void Jump(bool defaultJump)
    {
        if (!_isGrounded)
        {
            _jumpCharge = minJumpForce;
        }
        
        if (!_canJump) return;
        
        if (_isGrounded)
        {
            if (_groundedTime < repeatedJumpTimeLimit && !defaultJump)
            {
                
            }
            
            else
            {
                _previousJumpCharge = 0;
            }
            
            Vector3 velocity = _rb.velocity;
            velocity.x = 0;
            velocity.y = 0;
            velocity.z = 0;
            _rb.velocity = velocity;
            
            Vector3 jumpDirection = transform.up;
            
            _rb.AddForce(jumpDirection * _jumpCharge, ForceMode.Impulse);
            _previousJumpCharge = _jumpCharge;
            _jumpCharge = minJumpForce;
            
            _canJump = false;
            StartCoroutine(LockJumpForTime(1f));
        }
    }

    IEnumerator LockJumpForTime(float time)
    {
        yield return new WaitForSeconds(time);
        _canJump = true;
        normalizedJumpCharge = 0;
    }
}