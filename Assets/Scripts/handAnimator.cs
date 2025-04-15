using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandAnimator : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] private XRNode handNode = XRNode.RightHand;
    [SerializeField] private Animator handAnimator;
    [SerializeField] private float gripThreshold = 0.4f;
    [SerializeField] private float triggerThreshold = 0.4f;

    private InputDevice targetDevice;
    private bool deviceInitialized = false;

    private void Start()
    {
        handAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!deviceInitialized)
        {
            InitializeHand();
            return;
        }

        UpdateHandAnimation();
    }

    private void InitializeHand()
    {
        targetDevice = InputDevices.GetDeviceAtXRNode(handNode);
        if (targetDevice.isValid)
        {
            deviceInitialized = true;
        }
    }

    private void UpdateHandAnimation()
    {
        if (handAnimator == null) return;

        // Handle grip
        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            handAnimator.SetFloat("Grip", gripValue);
        }

        // Handle trigger
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            handAnimator.SetFloat("Trigger", triggerValue);
        }

        // Handle pinch
        bool isPinching = false;
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float pinchValue))
        {
            isPinching = pinchValue >= triggerThreshold;
        }
        

        // Handle fist
        bool isFist = false;
        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float fistValue))
        {
            isFist = fistValue >= gripThreshold;
        }
    
    }
}
