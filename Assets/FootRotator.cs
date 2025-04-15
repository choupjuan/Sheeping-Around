using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FootRotator : MonoBehaviour
{
    // Add at top of class with other fields
    public static event System.Action<Transform> OnFootDismissed;

    [Header("XR Settings")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;

    [Header("Interaction Settings")]
    [SerializeField] private float pinchThreshold = 0.5f;
    [SerializeField] private float gripThreshold = 0.5f;
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("Haptic Settings")]
    [SerializeField] private float grabHapticStrength = 0.5f;
    [SerializeField] private float pinchHapticStrength = 0.3f;
    [SerializeField] private float hapticDuration = 0.1f;

    [Header("Dismiss Settings")]
    [SerializeField] private float throwVelocityThreshold = 2f;
    [SerializeField] private float doubleTapThreshold = 0.3f;
    [SerializeField] private float throwHapticStrength = 0.7f;

    // Remove the SerializeField since we'll find it at runtime
    private Transform xrOrigin;

    private InputDevice device;
    private bool deviceInitialized;
    private List<XRNodeState> nodeStates = new List<XRNodeState>();

    // Interaction state tracking
    private bool isPointedAt;
    private bool isPinching;
    private bool isGrabbing;
    private bool wasGrabbing;
    private bool wasPinching;
    private bool grabStartedOnObject;
    private bool pinchStartedOnObject;

    // Transform tracking
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private bool isTracking;

    // Add to existing fields
    private float lastTapTime;

    private void Awake()
    {
        // Find XR Origin by tag
        GameObject xrOriginObj = GameObject.FindGameObjectWithTag("XROrigin");
        if (xrOriginObj != null)
        {
            xrOrigin = xrOriginObj.transform;
        }
        else
        {
            Debug.LogError("XR Origin not found! Make sure it's tagged with 'XROrigin'");
        }
    }

    private void Update()
    {
        if (!deviceInitialized && !TryInitializeDevice()) return;

        CheckPointing();
        HandleInput();
        HandleInteraction();
    }

    private bool TryInitializeDevice()
    {
        device = InputDevices.GetDeviceAtXRNode(controllerNode);
        deviceInitialized = device.isValid;
        return deviceInitialized;
    }

    private void CheckPointing()
    {
        if (XRRaycastUtility.TryGetWorldRay(xrOrigin, device, out Ray ray))
        {
            isPointedAt = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactionLayer) 
                && hit.transform == transform;
        }
    }

    private void HandleInput()
    {
        HandleGrabInput();
        HandlePinchInput();

        wasGrabbing = isGrabbing;
        wasPinching = isPinching;
    }

    private void HandleGrabInput()
    {
        if (!device.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
            return;

        bool isGrabbingNow = gripValue >= gripThreshold;

        // Check for double-tap when grabbing starts
        if (isGrabbingNow && !wasGrabbing)
        {
            if (Time.time - lastTapTime < doubleTapThreshold && isPointedAt)
            {
                SendHapticFeedback(throwHapticStrength);
                // Notify with parent transform before destroying
                OnFootDismissed?.Invoke(transform.parent);
                Destroy(transform.parent.gameObject);
                return;
            }
            lastTapTime = Time.time;
        }

        // Regular grab handling
        if (isGrabbingNow && !wasGrabbing && isPointedAt)
        {
            StartGrab();
        }
        else if (!isGrabbingNow)
        {
            StopGrab();
        }
        else if(isGrabbingNow)
        {
            isGrabbing = true;
        }
        else
        {
            isGrabbing = grabStartedOnObject;
        }
    }

    private void HandlePinchInput()
    {
        if (isGrabbing || !device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            return;

        bool isPinchingNow = triggerValue >= pinchThreshold;

        if (isPinchingNow && !wasPinching && isPointedAt)
        {
            StartPinch();
        }
        else if (!isPinchingNow)
        {
            StopPinch();
        }
        else if(isPinchingNow)
        {
            isPinching = true;
        }
        else
        {
            isPinching = pinchStartedOnObject;
        }
    }

    private void StartGrab()
    {
        isGrabbing = true;
        grabStartedOnObject = true;
        SendHapticFeedback(grabHapticStrength);
    }

    private void StopGrab()
    {
        if (isTracking && currentVelocity.magnitude > throwVelocityThreshold)
        {
            SendHapticFeedback(throwHapticStrength);
            // Notify with parent transform before destroying
            OnFootDismissed?.Invoke(transform.parent);
            Destroy(transform.parent.gameObject);
            return;
        }

        isGrabbing = false;
        grabStartedOnObject = false;
        isTracking = false;
        currentVelocity = Vector3.zero;
    }

    private void StartPinch()
    {
        isPinching = true;
        pinchStartedOnObject = true;
        SendHapticFeedback(pinchHapticStrength);
    }

    private void StopPinch()
    {
        isPinching = false;
        pinchStartedOnObject = false;
    }

    private void SendHapticFeedback(float strength)
    {
        device.SendHapticImpulse(0, strength, hapticDuration);
    }

    private void HandleInteraction()
    {
        if (!isPointedAt) return;

        InputTracking.GetNodeStates(nodeStates);
        var controllerState = nodeStates.Find(node => node.nodeType == controllerNode);
        
        if (!controllerState.tracked) return;

        controllerState.TryGetPosition(out Vector3 currentPosition);
        controllerState.TryGetRotation(out Quaternion currentRotation);

        if (isPinching && pinchStartedOnObject)
        {
            HandleRotation(currentRotation);
        }
        else if (isGrabbing && grabStartedOnObject)
        {
            HandleMovement(currentPosition);
        }

        lastPosition = currentPosition;
        lastRotation = currentRotation;
    }

    private void HandleRotation(Quaternion currentRotation)
    {
        Quaternion rotationDelta = currentRotation * Quaternion.Inverse(lastRotation);
        transform.rotation = rotationDelta * transform.rotation;
    }

    private void HandleMovement(Vector3 currentPosition)
    {
        Vector3 positionDelta = currentPosition - lastPosition;
        transform.position += positionDelta;

        // Calculate velocity for throw detection
        if (isTracking)
        {
            currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        }
        previousPosition = transform.position;
        isTracking = true;
    }
}
