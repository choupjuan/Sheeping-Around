using UnityEngine;
using UnityEngine.XR;

public class GrabbableObject : MonoBehaviour
{
    public XRNode rightHand = XRNode.RightHand;
    public XRNode leftHand = XRNode.LeftHand; // Add this for left controller
    public Transform controllerTransform;
    public float maxPointingDistance = 10f;
    public bool isGrabbed { get; private set; }
    public bool isPointedAt { get; private set; }

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSensitivity = 5f;
    [SerializeField] private float minRotationThreshold = 0.1f;
    [SerializeField] private float maxRotationAngle = 25f;  // Add this field

    [Header("Transform Settings")]
    [SerializeField] private Transform pivotPoint;
    [SerializeField] private Transform pivotPoint2;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    [Header("Toe References")]
    [SerializeField] private Transform leftToe;
    [SerializeField] private Transform rightToe;

    private InputDevice rightDevice;
    private InputDevice leftDevice;
    private bool rightDeviceAssigned = false;
    private bool leftDeviceAssigned = false;
    private Quaternion initialControllerRotation;
    private bool triggerHeld = false;
    private float currentTriggerValue = 0f;

    void Start()
    {
        TryInitialiseDevices();

        if (pivotPoint != null)
        {
            // Store the initial local transformations
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
        }
    }

    void Update()
    {
        if (!rightDeviceAssigned || !leftDeviceAssigned)
        {
            TryInitialiseDevices();
            return;
        }

        PerformRaycast();

        rightDevice.TryGetFeatureValue(CommonUsages.trigger, out currentTriggerValue);

        if (currentTriggerValue > 0)
        {
            if (!triggerHeld && isPointedAt)
            {
                StartGrab();
                isGrabbed = true;
            }
            triggerHeld = true;

            if (isGrabbed)
            {
                HandleJoystickRotation();
            }
        }
        else
        {
            triggerHeld = false;
            isGrabbed = false;
        }

        // Always maintain the original local position
        transform.localPosition = originalLocalPosition;
    }

    private void StartGrab()
    {
        initialControllerRotation = controllerTransform.rotation;
    }

    private void HandleJoystickRotation()
    {
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
        {
            float rotationAmount = joystickValue.x * rotationSensitivity * Time.deltaTime;
            
            if (Mathf.Abs(rotationAmount) > minRotationThreshold)
            {
                // Rotate left toe when joystick goes left
                if (rotationAmount < 0 && leftToe != null)
                {
                    float currentAngle = leftToe.localEulerAngles.z;
                    if (currentAngle > 180) currentAngle -= 360; // Convert 360 degrees to -180 to +180 range
                    
                    if (currentAngle - rotationAmount <= maxRotationAngle)
                    {
                        leftToe.Rotate(Vector3.forward * -rotationAmount, Space.Self);
                    }
                }
                // Rotate right toe when joystick goes right
                else if (rotationAmount > 0 && rightToe != null)
                {
                    float currentAngle = rightToe.localEulerAngles.z;
                    if (currentAngle > 180) currentAngle -= 360;
                    
                    if (Mathf.Abs(currentAngle + rotationAmount) <= maxRotationAngle)
                    {
                        rightToe.Rotate(Vector3.forward * -rotationAmount, Space.Self);
                    }
                }
            }
        }
    }

    private void PerformRaycast()
    {
        if (controllerTransform == null)
        {
            Debug.LogError("Controller Transform not assigned in GrabbableObject.");
            return;
        }

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPointingDistance))
        {
            if (hit.transform == transform)
            {
                isPointedAt = true;
                return;
            }
        }
        isPointedAt = false;
    }

    private void TryInitialiseDevices()
    {
        rightDevice = InputDevices.GetDeviceAtXRNode(rightHand);
        leftDevice = InputDevices.GetDeviceAtXRNode(leftHand);
        
        if (rightDevice.isValid && leftDevice.isValid)
        {
            rightDeviceAssigned = true;
            leftDeviceAssigned = true;
        }
    }

    // Reset to original position/rotation if things get out of sync
    public void ResetTransform()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }

    private void OnDrawGizmos()
    {
        if (controllerTransform != null)
        {
            Gizmos.color = isPointedAt ? Color.green : Color.red;
            Gizmos.DrawRay(controllerTransform.position, controllerTransform.forward * maxPointingDistance);
        }
    }
}