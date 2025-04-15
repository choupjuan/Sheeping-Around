using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class HapticSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private float triggerThreshold = 0.5f;
    [SerializeField] private float switchCooldown = 0.5f;

    [Header("Position Settings")]
    [SerializeField] private Vector3 handOffset = new Vector3(0.1f, 0f, 0f); // Offset to the right

    private Transform leftHandParent;
    private InputDevice leftController;
    private bool isPointedAt = false;
    private Transform xrOrigin;
    private float lastSwitchTime;
    private bool isInHand = false;
    private Transform originalParent;
    private GameObject modelParent;

    private void Start()
    {
        FindLeftHandParent();
        FindXROrigin(); 
    }

    private void OnEnable()
    {
        FootRotator.OnFootDismissed += HandleFootDismissed;
    }

    private void OnDisable()
    {
        FootRotator.OnFootDismissed -= HandleFootDismissed;
    }

    private void FindLeftHandParent()
    {
        modelParent = GameObject.Find("[LeftHand] Model Parent");
        if (modelParent != null)
        {
            leftHandParent = modelParent.transform.parent;
            Debug.Log("Found [LeftHand] Model Parent");
        }
        else
        {
            Debug.LogError("Could not find [LeftHand] Model Parent in scene");
        }
    }

    private void FindXROrigin()
    {
        var xrOriginObj = GameObject.FindWithTag("XROrigin");
        if (xrOriginObj != null)
        {
            xrOrigin = xrOriginObj.transform;
            Debug.Log("Found XR Origin");
        }
        else
        {
            Debug.LogError("Could not find XR Origin - make sure it's tagged with 'XROrigin'");
        }
    }

    private void Update()
    {
        if (!leftController.isValid)
        {
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            return;
        }

        CheckPointing();
        HandleInteraction();
    }

    private void CheckPointing()
    {
        if (XRRaycastUtility.TryGetWorldRay(xrOrigin, leftController, out Ray ray))
        {
            isPointedAt = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactionLayer) 
                && hit.transform == transform;
        }
    }

    private void HandleInteraction()
    {
        if (!isPointedAt) return;

        if (IsTriggerPressed() && Time.time >= lastSwitchTime + switchCooldown)
        {
            if (!isInHand)
            {
                MoveToHand();
            }
            else
            {
                ReturnToOriginal();
            }
            lastSwitchTime = Time.time;
        }
    }

    private bool IsTriggerPressed()
    {
        if (leftController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            return triggerValue >= triggerThreshold;
        }
        return false;
    }

    private void MoveToHand()
    {
        Transform footObject = transform.parent.parent;
        Transform baseObject = transform.parent;
        if (footObject != null && leftHandParent != null)
        {
            // Store original parent for later
            originalParent = footObject.parent;
            Vector3 forward = leftHandParent.forward;
            Vector3 up = leftHandParent.up;
            Vector3 right = leftHandParent.right;

            baseObject.localRotation = Quaternion.Euler(0, 180, 0);
            // Parent to the hand first
            footObject.SetParent(leftHandParent);

            // Set local position and rotation directly
            baseObject.localPosition = Vector3.zero;
            baseObject.rotation = Quaternion.Euler(0,180, 0);
            footObject.localPosition = Vector3.zero;
            footObject.localRotation = Quaternion.Euler(0, 0, 0);

            isInHand = true;
            modelParent.gameObject.SetActive(false);
            Debug.Log("Moved foot to hand and disabled hand model");
        }
        else
        {
            Debug.LogWarning("Missing references for foot movement");
        }
    }

    private void ReturnToOriginal()
    {
        Transform footObject = transform.parent.parent;
        if (footObject != null && originalParent != null)
        {
            footObject.SetParent(originalParent, true);
            isInHand = false;
            
            // Reactivate the left hand model
            modelParent.gameObject.SetActive(true);
            Debug.Log("Returned foot to original position and enabled hand model");
        }
    }

    private void HandleFootDismissed(Transform dismissedFoot)
    {
        // Check if this is our foot that was dismissed
        Transform footObject = transform.parent.parent;
        if (isInHand && footObject == dismissedFoot)
        {
            // Reactivate hand model
            modelParent.gameObject.SetActive(true);
            isInHand = false;
            Debug.Log("Foot dismissed, reactivating hand model");
        }
    }
}
