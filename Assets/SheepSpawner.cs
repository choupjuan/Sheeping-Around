using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject sheepPrefab;
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private int maxSheepCount = 4;
    
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool randomizeSpawnPoints = true;
    
    [Header("References")]
    [SerializeField] private Countdown gameTimer;
    
    private float nextSpawnTime;
    private int currentSheepCount;
    private List<Transform> availableSpawnPoints;

    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
        InitializeSpawnPoints();
        
        // Subscribe to the sheep destroyed event from SheepInteractor
        SheepInteractor.OnSheepDestroyed += HandleSheepDestroyed;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when this component is destroyed
        SheepInteractor.OnSheepDestroyed -= HandleSheepDestroyed;
    }

    private void InitializeSpawnPoints()
    {
        availableSpawnPoints = new List<Transform>(spawnPoints);
        if (randomizeSpawnPoints)
        {
            ShuffleSpawnPoints();
        }
    }

    private void ShuffleSpawnPoints()
    {
        int n = availableSpawnPoints.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            Transform temp = availableSpawnPoints[k];
            availableSpawnPoints[k] = availableSpawnPoints[n];
            availableSpawnPoints[n] = temp;
        }
    }

    private void Update()
    {
        // Check if we can spawn more sheep
        if (currentSheepCount >= maxSheepCount) return;

        // Check if there are available spawn points
        if (availableSpawnPoints.Count == 0) return;

        // Check if the timer is still running
        if (gameTimer != null && !gameTimer.IsRunning()) return;

        // Check if it's time to spawn
        if (Time.time >= nextSpawnTime)
        {
            SpawnSheep();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnSheep()
    {
        if (sheepPrefab == null || availableSpawnPoints.Count == 0) return;

        // Get next spawn point
        Transform spawnPoint = availableSpawnPoints[0];
        availableSpawnPoints.RemoveAt(0);

        // Spawn the sheep at the spawn point
        GameObject sheep = Instantiate(sheepPrefab, spawnPoint.position, spawnPoint.rotation);
        currentSheepCount++;
        
        Debug.Log($"Sheep spawned at {spawnPoint.name}. Current count: {currentSheepCount}");
    }

    // Update the sheep destroyed handler
    private void HandleSheepDestroyed(GameObject sheep)
    {
        currentSheepCount--;
        Debug.Log($"Sheep destroyed via SheepInteractor. Current count: {currentSheepCount}");
        
        // Recycle a spawn point
        Transform closestPoint = FindClosestSpawnPoint(sheep.transform.position);
        if (closestPoint != null && !availableSpawnPoints.Contains(closestPoint))
        {
            availableSpawnPoints.Add(closestPoint);
            
            if (randomizeSpawnPoints)
            {
                ShuffleSpawnPoints();
            }
        }
    }
    
    private Transform FindClosestSpawnPoint(Vector3 position)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Transform point in spawnPoints)
        {
            float distance = Vector3.Distance(point.position, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }
        
        return closest;
    }

    private void OnDrawGizmos()
    {
        // Draw spawn points in editor
        Gizmos.color = Color.yellow;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
            }
        }
    }
}
