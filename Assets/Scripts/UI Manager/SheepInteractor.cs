using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Ursaanimation.CubicFarmAnimals;


public class SheepInteractor : MonoBehaviour
{
    // Add these static events
    public static event System.Action<GameObject> OnFootSpawned;
    public static event System.Action<GameObject> OnSheepDestroyed;

    [Header("XR Settings")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;
    [SerializeField] private Transform controllerTransform;
    private Transform xrOrigin;
    [SerializeField] private GameObject leftHandObject;
    
    [Header("Interaction Settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float triggerThreshold = 0.5f;

    private Transform lastHitObject;
    private static bool anySheepHit = false;

    [Header("Leg Interaction")]
    [SerializeField] private LayerMask legLayerMask;
    [SerializeField] private Color legHighlightColor = Color.green;
    private bool isSheepLifted = false;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Transform xrCamera;

    private Transform currentSheep;

    // Add this to track spawned feet
    private HashSet<Transform> spawnedFeet = new HashSet<Transform>();

    // Add to class fields
    [SerializeField] private LayerMask footLayerMask; // Layer for spawned feet
    private bool isFootActive = false;

    [Header("Settings")]
    [SerializeField] private string spawnedFootTag = "SpawnedFoot";

    private int totalLegsInteracted = 0;
    private const int REQUIRED_LEG_COUNT = 4;

    private List<LamenessData> currentSheepConditions;

    private SheepIllnessManager illnessManager;

    private void Start()
    {
        illnessManager = GetComponent<SheepIllnessManager>();
        
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
        if (controllerTransform == null) return;

        if (!isSheepLifted)
        {
            
            if (TryGetSheepInView(out RaycastHit hit))
            {
                HandleSheepInteraction(hit);
            }
            else
            {
                lastHitObject = null;
            }
        }
        else
        {
            HandleLegInteraction();
        }
    }

    private bool TryGetSheepInView(out RaycastHit hit)
    {
        if (!xrOrigin || !XRRaycastUtility.TryGetWorldRay(xrOrigin, InputDevices.GetDeviceAtXRNode(controllerNode), out Ray ray))
        {
            hit = new RaycastHit();
            return false;
        }
        return Physics.Raycast(ray, out hit, maxDistance) && hit.transform.CompareTag("sheep"); 
    }

    private void HandleSheepInteraction(RaycastHit hit)
    {
        if (lastHitObject != hit.transform)
        {
            lastHitObject = hit.transform;
            Debug.Log("Sheep detected");
        }

        if (IsTriggerPressed() && !anySheepHit)
        {
            Debug.Log("Interacting with sheep");
            ShowLeftHand();
            ProcessSheepInteraction(hit.transform);
        }
    }

    private void HandleLegInteraction()
    {
        // First check if there's an active foot
        if (isFootActive)
        {
            // Check if any foot is still in the scene
            GameObject[] activeFeet = GameObject.FindGameObjectsWithTag(spawnedFootTag);
            isFootActive = activeFeet.Length > 0;
        }

        // Only allow new foot spawning if no foot is active
        if (!isFootActive)
        {
            if (!XRRaycastUtility.TryGetWorldRay(xrOrigin, InputDevices.GetDeviceAtXRNode(controllerNode), out Ray ray))
                return;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, legLayerMask))
            {
                if (IsTriggerPressed())
                {
                    if (hit.transform.CompareTag("SheepLeg"))
                    {
                        Transform legGroup = hit.transform.parent;
                        // Check if the leg belongs to the current sheep
                        if (legGroup != null && legGroup.IsChildOf(currentSheep) && !spawnedFeet.Contains(legGroup))
                        {
                            HighlightLegGroup(legGroup);
                            SpawnObjectInFrontOfUser();
                            Debug.Log($"Leg group spawned: {legGroup.name} from sheep: {currentSheep.name}");
                            spawnedFeet.Add(legGroup);
                            isFootActive = true;
                        }
                        else
                        {
                            Debug.Log("Leg does not belong to current sheep or already spawned");
                        }
                    }
                }
            }
        }
    }

    private void HighlightLegGroup(Transform legGroup)
    {
        // Get immediate children of the leg group
        foreach (Transform legPart in legGroup)
        {
            SkinnedMeshRenderer renderer = legPart.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].color = legHighlightColor;
                }
                renderer.materials = materials;
            }
            else
            {
                Debug.LogWarning($"SkinnedMeshRenderer not found on leg part: {legPart.name}");
            }
        }
    }

    private bool IsTriggerPressed()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.trigger, out float value))
        {
            return value > triggerThreshold;
        }
        return false;
    }

    private void ShowLeftHand()
    {
        if (leftHandObject != null)
        {
            leftHandObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Left hand object reference is missing");
        }
    }

    private void ProcessSheepInteraction(Transform sheep)
    {
        if (sheep == null) return;

        anySheepHit = true;
        DisableAnimations(sheep);
        sheep.position = Vector3.up * 2.0f;
        currentSheep = sheep;
        isSheepLifted = true;

        // Assign multiple random conditions
        currentSheepConditions = illnessManager.AssignRandomLameness(sheep.gameObject);
        Debug.Log($"Assigned {currentSheepConditions.Count} conditions to sheep: {sheep.name}");

        // Complete the trigger prompt when sheep is lifted
        if (PromptManager.Instance != null)
        {
            PromptManager.Instance.CompleteTriggerPrompt();
        }
    }


    private void DisableAnimations(Transform sheep)
    {
        Ursaanimation.CubicFarmAnimals.AnimationController animationController = sheep.GetComponent<Ursaanimation.CubicFarmAnimals.AnimationController>();
       
        if (animationController != null)
        {
            sheep.GetComponent<Animator>().StopPlayback();
            animationController.enabled = false;
            Debug.Log("Animation controller disabled");
        }
        else
        {
            Debug.LogWarning("Animation controller not found");
        }
        Animator animator = sheep.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Up");
            animator.Update(0);
            animator.enabled = false;
            Debug.Log("Animator disabled");
        }
        else
        {
            Debug.LogWarning("Animator not found");
        }
    }

    private void SpawnObjectInFrontOfUser()
    {
        if (spawnPrefab == null || xrCamera == null)
        {
            Debug.LogWarning("Spawn prefab or XR Camera reference missing");
            return;
        }

        // Get camera position and forward direction
        Vector3 cameraPosition = xrCamera.transform.position;
        Vector3 forward = xrCamera.transform.forward;
        forward.y = 0; // Remove vertical component
        forward.Normalize();

        // Calculate spawn position at exact eye level
        Vector3 spawnPosition = new Vector3(
            cameraPosition.x + (forward.x * spawnDistance),
            cameraPosition.y, // Keep exact camera Y position
            cameraPosition.z + (forward.z * spawnDistance)
        );

        // Add any additional offset if needed
        spawnPosition += spawnOffset;

        // Spawn the object facing forward
        GameObject spawnedObject = Instantiate(spawnPrefab, spawnPosition, Quaternion.identity);
        spawnedObject.tag = spawnedFootTag;

        // Trigger the event when foot is spawned
        OnFootSpawned?.Invoke(spawnedObject);

        // Get the PartActivator and set active illnesses
        var partActivator = spawnedObject.GetComponent<PartActivator>();
        if (partActivator != null && currentSheepConditions != null)
        {
            var forcedIllnesses = illnessManager.GetForcedIllnesses();
            partActivator.SetActiveIllnesses(currentSheepConditions, forcedIllnesses);
        }
        else
        {
            Debug.LogWarning("PartActivator not found on spawned foot or no current sheep conditions");
        }
    }

    // Add this method to reset the spawned feet when the sheep is dropped
    private void ResetSpawnedFeet()
    {
        spawnedFeet.Clear();
        isSheepLifted = false;
        totalLegsInteracted = 0;
        anySheepHit = false;
        currentSheep = null;
        isFootActive = false;
    }

    private void OnEnable()
    {
        FootRotator.OnFootDismissed += HandleFootDismissed;

    }

    private void OnDisable()
    {
        FootRotator.OnFootDismissed -= HandleFootDismissed;
    }


    private void HandleFootDismissed(Transform legGroup)
    {
        totalLegsInteracted++;
        if (totalLegsInteracted >= REQUIRED_LEG_COUNT)
        {
            // Trigger the event before destroying the sheep
            OnSheepDestroyed?.Invoke(currentSheep.gameObject);
            Destroy(currentSheep.gameObject);
            ResetSpawnedFeet();
        }
        Debug.Log($"Leg group dismissed: {legGroup.name}");
        if (spawnedFeet.Contains(legGroup))
        {
            spawnedFeet.Remove(legGroup);

            // Check remaining feet in scene using tag
            GameObject[] activeFeet = GameObject.FindGameObjectsWithTag(spawnedFootTag);
            isFootActive = activeFeet.Length > 0;
            Debug.Log($"Active feet remaining: {activeFeet.Length}");
        }
        else
        {
            Debug.LogWarning($"Attempted to dismiss untracked leg group: {legGroup.name}");
        }
    }
}
