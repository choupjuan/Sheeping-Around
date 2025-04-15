using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ToeRotator : MonoBehaviour
{
    [Header("XR Settings")]
    public XRNode leftHand = XRNode.LeftHand;
    private InputDevice leftDevice;

    [Header("Interaction Settings")]
    [SerializeField] private float rotationSensitivity = 5f;
    [SerializeField] private float minRotationThreshold = 0.1f;
    [SerializeField] private float maxRotationAngle = 25f;
    [SerializeField] private float gripThreshold = 0.5f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float maxMoveDistance = 0.5f;

    [Header("Toe References")]
    [SerializeField] private Transform leftToe;
    [SerializeField] private Transform rightToe;

    private bool deviceInitialized = false;
    private Quaternion leftToeOriginalRotation;
    private Quaternion rightToeOriginalRotation;
    private Vector3 originalPosition;
    private bool isGrabbed = false;

    private void Start()
    {
        TryInitialiseDevice();
        if (leftToe != null) leftToeOriginalRotation = leftToe.localRotation;
        if (rightToe != null) rightToeOriginalRotation = rightToe.localRotation;
        originalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!deviceInitialized)
        {
            TryInitialiseDevice();
            return;
        }

        HandleGrabInput();
        if (!isGrabbed)
        {
            HandleToeRotation();
        }
        else
        {
            HandleToeMovement();
        }
    }

    private void HandleGrabInput()
    {
        if (leftDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            isGrabbed = gripValue >= gripThreshold;
            
            // Reset position when releasing grab
            if (!isGrabbed)
            {
                transform.localPosition = originalPosition;
            }
        }
    }

    private void HandleToeMovement()
    {
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
        {
            float moveAmount = joystickValue.y * moveSpeed * Time.deltaTime;
            Vector3 newPosition = transform.localPosition + Vector3.forward * moveAmount;

            // Limit movement range
            float distanceFromOriginal = Vector3.Distance(newPosition, originalPosition);
            if (distanceFromOriginal <= maxMoveDistance)
            {
                transform.localPosition = newPosition;
            }
        }
    }

    private void TryInitialiseDevice()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(leftHand);
        deviceInitialized = leftDevice.isValid;
    }

    private void HandleToeRotation()
    {
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
        {
            float rotationAmount = joystickValue.x * rotationSensitivity * Time.deltaTime;

            if (Mathf.Abs(rotationAmount) > minRotationThreshold)
            {
                // Left toe control
                if (rotationAmount < 0 && leftToe != null)
                {
                    float currentAngle = leftToe.localEulerAngles.z;
                    if (currentAngle > 180) currentAngle -= 360;

                    if (currentAngle - rotationAmount <= maxRotationAngle)
                    {
                        leftToe.Rotate(Vector3.forward * -rotationAmount, Space.Self);
                    }
                    // Reset right toe when moving left
                    if (rightToe != null)
                    {
                        rightToe.localRotation = rightToeOriginalRotation;
                    }
                }
                // Right toe control
                else if (rotationAmount > 0 && rightToe != null)
                {
                    float currentAngle = rightToe.localEulerAngles.z;
                    if (currentAngle > 180) currentAngle -= 360;

                    if (Mathf.Abs(currentAngle + rotationAmount) <= maxRotationAngle)
                    {
                        rightToe.Rotate(Vector3.forward * -rotationAmount, Space.Self);
                    }
                    // Reset left toe when moving right
                    if (leftToe != null)
                    {
                        leftToe.localRotation = leftToeOriginalRotation;
                    }
                }
            }
            else
            {
                ResetToes();
            }
        }
        else
        {
            ResetToes();
        }
    }

    private void ResetToes()
    {
        if (leftToe != null) leftToe.localRotation = leftToeOriginalRotation;
        if (rightToe != null) rightToe.localRotation = rightToeOriginalRotation;
    }
}
