using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System;

public class VRCanvasController : MonoBehaviour
{
    [System.Serializable]
    private class CanvasMapping
    {
        public LayerMask targetLayer;
        public List<GameObject> canvasPrefabs = new List<GameObject>();  // Changed to list of prefabs
        public Canvas currentCanvas;  // Renamed for clarity
    }

    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Transform controllerTransform;
    [SerializeField] private List<CanvasMapping> canvasMappings = new List<CanvasMapping>();
    
    [Header("XR Settings")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;
    [SerializeField] private LayerMask interactionLayerMask;
    [SerializeField] private float maxRayDistance = 100f;

    [Header("Canvas Settings")]
    [SerializeField] private float distanceFromUser = 2f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Vector3 canvasScale = new Vector3(0.001f, 0.001f, 0.001f); // Add this line
    [Header("XR Origin")]
    [SerializeField] private Transform xrOrigin;

    private InputDevice device;
    private bool deviceInitialized;
    private bool lastTriggerState;
    private bool isCanvasVisible;
    private bool isAnimating;
    private Canvas activeCanvas;
    private Vector3 originalScale;
    private float animationTime;
    private List<GameObject> instantiatedCanvases = new List<GameObject>();

    private void Start()
    {
        InitializeComponents();
        HideCanvas();
    }

    private void OnEnable()
    {
        // Subscribe to foot dismissed event
        FootRotator.OnFootDismissed += OnFootDismissed;
        SheepInteractor.OnFootSpawned += OnFootSpawned;
    }

    private void OnDisable()
    {
        // Unsubscribe when component is disabled
        FootRotator.OnFootDismissed -= OnFootDismissed;
        SheepInteractor.OnFootSpawned -= OnFootSpawned;
    }

    private void OnFootSpawned(GameObject objecte)
    {
        InitializeComponents();
    }

    
    

    private void OnFootDismissed(Transform dismissedFoot)
    {
        DestroyAllCanvases();
    }

    private void DestroyAllCanvases()
    {
        // Hide current canvas first
        HideCurrentCanvas();
        activeCanvas = null;
        
        // Destroy all instantiated canvases
        foreach (var canvasObj in instantiatedCanvases)
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
            }
        }
        
        // Clear the list
        instantiatedCanvases.Clear();
        
        // Reset canvas mappings
        foreach (var mapping in canvasMappings)
        {
            mapping.currentCanvas = null;
        }
        
        Debug.Log("All canvases destroyed after foot dismissal");
    }

    private void Update()
    {
        if (!deviceInitialized && !TryInitializeDevice()) return;

        HandleInput();
        UpdateAnimation();
    }

    private void InitializeComponents()
    {
        if (xrCamera == null) xrCamera = Camera.main;
        originalScale = canvasScale;

        // Destroy any existing canvases first
        DestroyAllCanvases();

        // Instantiate random canvas prefabs for each mapping
        foreach (var mapping in canvasMappings)
        {
            if (mapping.canvasPrefabs != null && mapping.canvasPrefabs.Count > 0)
            {
                // Select random prefab from the list
                int randomIndex = UnityEngine.Random.Range(0, mapping.canvasPrefabs.Count);
                GameObject selectedPrefab = mapping.canvasPrefabs[randomIndex];

                if (selectedPrefab != null)
                {
                    Debug.Log($"Instantiating random canvas prefab: {selectedPrefab.name}");
                    GameObject canvasInstance = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
                    mapping.currentCanvas = canvasInstance.GetComponent<Canvas>();

                    if (mapping.currentCanvas != null)
                    {
                        // Hide initially
                        canvasInstance.transform.localScale = Vector3.zero;
                        mapping.currentCanvas.enabled = false;
                        
                        // Add to our list of instantiated canvases
                        instantiatedCanvases.Add(canvasInstance);
                    }
                    else
                    {
                        Debug.LogError($"Canvas component not found on prefab: {selectedPrefab.name}");
                        Destroy(canvasInstance);
                    }
                }
            }
        }
    }

    private bool TryInitializeDevice()
    {
        device = InputDevices.GetDeviceAtXRNode(controllerNode);
        deviceInitialized = device.isValid;
        return deviceInitialized;
    }

    private void HandleInput()
    {
        if (!device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerValue))
            return;

        if (triggerValue && !lastTriggerState)
        {
            RaycastHit hit;
            if (TryGetTarget(out hit))
            {
                Canvas targetCanvas = GetCanvasForTarget(hit.transform);
                if (targetCanvas != null)
                {
                    if (activeCanvas != targetCanvas)
                    {
                        HideCurrentCanvas();
                        activeCanvas = targetCanvas;
                    }
                    ToggleCanvas();
                    if (isCanvasVisible) PositionCanvas();
                }
            }
            
        }

        // Check if the active canvas target still exists
        if (isCanvasVisible && activeCanvas != null)
        {
            bool targetExists = false;
            foreach (var mapping in canvasMappings)
            {
                if (mapping.currentCanvas == activeCanvas)
                {
                    // Try to find any object in the target layer
                    Collider[] colliders = Physics.OverlapSphere(transform.position, maxRayDistance, mapping.targetLayer);
                    if (colliders.Length > 0)
                    {
                        targetExists = true;
                        break;
                    }
                }
            }

            if (!targetExists)
            {
                HideCurrentCanvas();
            }
        }

        lastTriggerState = triggerValue;
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = new RaycastHit();
        
        if (!XRRaycastUtility.TryGetControllerRay(xrOrigin, controllerNode, out Ray ray))
            return false;

        return Physics.Raycast(ray, out hit, maxRayDistance, interactionLayerMask);
    }

    private Canvas GetCanvasForTarget(Transform target)
    {
        var mapping = canvasMappings.Find(m => (m.targetLayer.value & (1 << target.gameObject.layer)) != 0);
        return mapping?.currentCanvas;
    }

    private void HideCurrentCanvas()
    {
        if (activeCanvas != null)
        {
            activeCanvas.transform.localScale = Vector3.zero;
            activeCanvas.enabled = false;
        }
    }

    private void UpdateAnimation()
    {
        if (!isAnimating) return;

        animationTime += Time.deltaTime;
        float progress = Mathf.Clamp01(animationTime / animationDuration);

        if (progress >= 1f)
        {
            FinishAnimation();
            return;
        }

        UpdateCanvasScale(progress);
    }

    private void ToggleCanvas()
    {
        if (isAnimating) return;

        isCanvasVisible = !isCanvasVisible;
        if (activeCanvas != null) activeCanvas.enabled = true;
        StartAnimation();
    }

    private void StartAnimation()
    {
        isAnimating = true;
        animationTime = 0f;
    }

    private void FinishAnimation()
    {
        isAnimating = false;
        if (!isCanvasVisible) HideCanvas();
    }

    private void HideCanvas()
    {
        if (activeCanvas != null)
        {
            activeCanvas.transform.localScale = Vector3.zero;
            activeCanvas.enabled = false;
        }
    }

    private void UpdateCanvasScale(float progress)
    {
        if (activeCanvas == null) return;
        
        float curveValue = showCurve.Evaluate(progress);
        Vector3 targetScale = isCanvasVisible ? originalScale : Vector3.zero;
        Vector3 startScale = isCanvasVisible ? Vector3.zero : originalScale;
        activeCanvas.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
    }

    private void PositionCanvas()
    {
        if (xrCamera == null || activeCanvas == null) return;

        Vector3 forward = xrCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 position = xrCamera.transform.position + forward * distanceFromUser;
        position.y += heightOffset;

        activeCanvas.transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation(forward, Vector3.up)
        );
    }
}