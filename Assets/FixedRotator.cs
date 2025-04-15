using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Linq;

public class FixedRotator : MonoBehaviour
{
    [Header("XR Settings")]
    [SerializeField] private float pinchThreshold = 0.5f;
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float maxRotationAngle = 45f; // Maximum rotation in either direction

    private float currentRotation = 0f; // Track total rotation

    [Header("Visual Settings")]
    [SerializeField] private float transparentAlpha = 0.5f;
    [SerializeField] private float solidAlpha = 1.0f;

    [Header("Haptic Settings")]
    [SerializeField] private float hapticAmplitude = 0.5f;
    [SerializeField] private float hapticDuration = 0.1f;
    [SerializeField] private float continuousHapticStrength = 0.1f;

    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool leftInitialized;
    private bool rightInitialized;
    private bool isLeftPinching;
    private bool isRightPinching;
    private bool wasLeftPinching;
    private bool wasRightPinching;
    private bool isPointedAt;
    private Quaternion lastLeftRotation;
    private Quaternion lastRightRotation;

    private bool leftStartedOnObject;
    private bool rightStartedOnObject;

    private Material[] materials;
    private Color[] originalColors;
    private MeshRenderer meshRenderer;
    private Transform xrOrigin;

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

        // Cache materials and colors
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            materials = meshRenderer.materials;
            originalColors = materials.Select(m => m.color).ToArray();
        }
    }

    private void Start()
    {
        // Cache renderer and materials
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            materials = meshRenderer.materials;
            originalColors = new Color[materials.Length];
            
            // Store original colors and set initial transparency
            for (int i = 0; i < materials.Length; i++)
            {
                originalColors[i] = materials[i].color;
                SetMaterialTransparency(materials[i], transparentAlpha);
            }
        }
    }

    private void Update()
    {
        if (!leftInitialized) TryInitializeDevice(XRNode.LeftHand, ref leftDevice, ref leftInitialized);
        if (!rightInitialized) TryInitializeDevice(XRNode.RightHand, ref rightDevice, ref rightInitialized);
        if (!leftInitialized && !rightInitialized) return;

        CheckPointing();
        HandleBothHandsInput();
        
        if (((isLeftPinching || isRightPinching) && isPointedAt)|| ((isLeftPinching || isRightPinching) &&
        (wasLeftPinching || wasRightPinching)))
        {
            HandleRotation();
        }

        // Store last rotations
        if (leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentLeftRotation))
        {
            lastLeftRotation = currentLeftRotation;
        }
        if (rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentRightRotation))
        {
            lastRightRotation = currentRightRotation;
        }
    }

    private void CheckPointing()
    {
        if (!xrOrigin)
            return;

        if (XRRaycastUtility.TryGetWorldRay(xrOrigin, leftDevice, out Ray leftRay))
        {
            bool wasPointedAt = isPointedAt;
            isPointedAt = Physics.Raycast(leftRay, out RaycastHit hit, maxRayDistance, interactionLayer) 
                && hit.transform == transform;

            if (wasPointedAt != isPointedAt)
            {
                UpdateTransparency();
            }
        }

        if (XRRaycastUtility.TryGetWorldRay(xrOrigin, rightDevice, out Ray rightRay))
        {
            bool wasPointedAt = isPointedAt;
            isPointedAt = Physics.Raycast(rightRay, out RaycastHit hit, maxRayDistance, interactionLayer) 
                && hit.transform == transform;

            if (wasPointedAt != isPointedAt)
            {
                UpdateTransparency();
            }
        }
    }

    private bool TryInitializeDevice(XRNode node, ref InputDevice device, ref bool initialized)
    {
        device = InputDevices.GetDeviceAtXRNode(node);
        initialized = device.isValid;
        return initialized;
    }

    private void HandleBothHandsInput()
    {
        HandleSingleHandInput(leftDevice, ref isLeftPinching, ref wasLeftPinching, ref leftStartedOnObject);
        HandleSingleHandInput(rightDevice, ref isRightPinching, ref wasRightPinching, ref rightStartedOnObject);
    }

    private void HandleSingleHandInput(InputDevice device, ref bool isPinching, ref bool wasPinching, ref bool startedOnObject)
    {
        if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            bool isPinchingNow = triggerValue >= pinchThreshold;

            // Start pinch only if pointing at object
            if (isPinchingNow && !wasPinching && isPointedAt)
            {
                isPinching = true;
                startedOnObject = true;
                // Initial strong haptic pulse when pinch starts
                device.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
            }
            // Stop pinch when released
            else if (!isPinchingNow)
            {
                isPinching = false;
                startedOnObject = false;
            }
            // Maintain pinch if it started on object and provide continuous feedback
            else if (startedOnObject)
            {
                isPinching = true;
                // Continuous light haptic feedback while pinching
                device.SendHapticImpulse(0, continuousHapticStrength, Time.deltaTime);
            }
            else if(isPinchingNow && wasPinching)
            {
                isPinching = true;
            }

            wasPinching = isPinchingNow;
        }
    }

    private void HandleRotation()
    {
        float totalRotation = 0f;

        // Only rotate with hands that started on object
        if (isLeftPinching && leftStartedOnObject && 
            leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentLeftRotation))
        {
            Quaternion leftDelta = currentLeftRotation * Quaternion.Inverse(lastLeftRotation);
            totalRotation += leftDelta.eulerAngles.z;
        }

        if (isRightPinching && rightStartedOnObject && 
            rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentRightRotation))
        {
            Quaternion rightDelta = currentRightRotation * Quaternion.Inverse(lastRightRotation);
            totalRotation += rightDelta.eulerAngles.z;
        }

        if (totalRotation != 0)
        {
            // Convert angles over 180 to negative values
            if (totalRotation > 180f) totalRotation -= 360f;

            // Calculate new rotation
            float newRotation = currentRotation + totalRotation;

            // Clamp to limits
            newRotation = Mathf.Clamp(newRotation, -maxRotationAngle, maxRotationAngle);

            // Apply only the difference
            float rotationToApply = newRotation - currentRotation;
            if (rotationToApply != 0)
            {
                transform.Rotate(0, 0, rotationToApply, Space.Self);
                currentRotation = newRotation;
            }
        }
    }

    private void UpdateTransparency()
    {
        if (materials == null) return;

        float targetAlpha = isPointedAt ? solidAlpha : transparentAlpha;
        foreach (Material material in materials)
        {
            SetMaterialTransparency(material, targetAlpha);
        }
    }

    private void SetMaterialTransparency(Material material, float alpha)
    {
        if (material == null) return;

        // Enable transparency
        material.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
        material.SetFloat("_Blend", 0);   // 0 = alpha, 1 = premultiply
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        // Set alpha
        Color color = material.color;
        color.a = alpha;
        material.color = color;
    }

    // Add initialization method for explicit reference setting
    public void Initialize(Transform xrOriginTransform)
    {
        xrOrigin = xrOriginTransform;
    }
}
