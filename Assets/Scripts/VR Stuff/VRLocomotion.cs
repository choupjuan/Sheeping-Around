using UnityEngine;
using UnityEngine.XR;

public class VRLocomotion : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private XRNode moveSource = XRNode.LeftHand;
    [SerializeField] private Transform headTransform;

    [Header("References")]
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;

    private InputDevice moveDevice;
    private bool deviceInitialized;
    private Vector2 inputAxis;
    private Vector3 originalLeftPosition;
    private Vector3 originalRightPosition;

    private void Update()
    {
        if (!deviceInitialized)
            InitializeDevice();

        if (!deviceInitialized)
            return;

        // Store original positions
        if (leftControllerTransform != null)
            originalLeftPosition = leftControllerTransform.position;
        if (rightControllerTransform != null)
            originalRightPosition = rightControllerTransform.position;

        HandleMovement();

        // Adjust controller positions relative to movement
        if (leftControllerTransform != null)
            leftControllerTransform.position = originalLeftPosition;
        if (rightControllerTransform != null)
            rightControllerTransform.position = originalRightPosition;
    }

    private void InitializeDevice()
    {
        moveDevice = InputDevices.GetDeviceAtXRNode(moveSource);
        deviceInitialized = moveDevice.isValid;
    }

    private void HandleMovement()
    {
        if (moveDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
        {
            // Only move if stick is pushed beyond deadzone
            if (axis.magnitude > 0.1f)
            {
                // Get forward direction from head but remove vertical component
                Vector3 forward = headTransform.forward;
                forward.y = 0;
                forward.Normalize();

                // Get right direction from head
                Vector3 right = headTransform.right;
                right.y = 0;
                right.Normalize();

                // Combine movement based on stick input
                Vector3 moveDirection = (forward * axis.y) + (right * axis.x);
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
        }
    }
}