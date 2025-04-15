using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

public class PromptManager : MonoBehaviour
{
    public static PromptManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("References")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private GameObject firstPromptPrefab;
    [SerializeField] private GameObject secondPromptPrefab;  // Add second prompt prefab
    [SerializeField] private float followDistance = 2f;
    [SerializeField] private float heightOffset = 0f;  // Changed to 0 for eye level
    [SerializeField] private float horizontalOffset = 0f;  // Add this for left/right adjustment
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 promptScale = new Vector3(0.001f, 0.001f, 0.001f);

    [Header("Task Settings")]
    [SerializeField] private bool isFirstTaskCompleted = false;
    [SerializeField] private bool isSecondTaskCompleted = false;

    private Canvas currentPromptCanvas;
    private GameObject firstPrompt;
    private GameObject secondPrompt;

    private void Start()
    {
        if (xrCamera == null)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject.CompareTag("MainCamera"))
            {
                xrCamera = mainCamera.transform;
            }
            else
            {
                Debug.LogError("XR Camera reference missing!");
                return;
            }
        }

        SpawnFirstPrompt();
    }

    private void SpawnFirstPrompt()
    {
        if (firstPromptPrefab == null) return;
        firstPrompt = SpawnPrompt(firstPromptPrefab);
        currentPromptCanvas = firstPrompt.GetComponent<Canvas>();
    }

    private void SpawnSecondPrompt()
    {
        if (secondPromptPrefab == null) return;
        secondPrompt = SpawnPrompt(secondPromptPrefab);
        currentPromptCanvas = secondPrompt.GetComponent<Canvas>();
    }

    private GameObject SpawnPrompt(GameObject prefab)
    {
        Vector3 spawnPosition = xrCamera.position + (xrCamera.forward * followDistance);
        spawnPosition.y += heightOffset;

        GameObject promptObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        promptObject.transform.localScale = promptScale;
        
        var canvas = promptObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }

        return promptObject;
    }

    private void Update()
    {
        if (currentPromptCanvas != null && !isSecondTaskCompleted)
        {
            // Get forward and right directions
            Vector3 forward = xrCamera.forward;
            Vector3 right = xrCamera.right;
            forward.y = 0;
            forward.Normalize();

            // Calculate pivot point with offsets
            Vector3 pivotPoint = xrCamera.position;
            pivotPoint.y += heightOffset;
            pivotPoint += right * horizontalOffset;

            // Calculate target position based on pivot point
            Vector3 targetPosition = pivotPoint + (forward * followDistance);

            // Smoothly move canvas to target position
            currentPromptCanvas.transform.position = Vector3.Lerp(
                currentPromptCanvas.transform.position, 
                targetPosition, 
                Time.deltaTime * smoothSpeed
            );

            // Make canvas face the offset pivot point instead of camera position
            currentPromptCanvas.transform.rotation = Quaternion.LookRotation(
                currentPromptCanvas.transform.position - pivotPoint,
                Vector3.up
            );
        }
    }

    public void CompleteTriggerPrompt()
    {
        isFirstTaskCompleted = true;
        if (firstPrompt != null)
        {
            firstPrompt.SetActive(false);
            SpawnSecondPrompt();
        }
    }

    public void CompleteSecondPrompt()
    {
        isSecondTaskCompleted = true;
        if (secondPrompt != null)
        {
            secondPrompt.SetActive(false);
        }
    }
}
