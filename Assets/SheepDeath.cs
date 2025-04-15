using UnityEngine;
using System;

public class SheepDeath : MonoBehaviour
{
    public static event Action<GameObject, Transform> OnAnySheepDestroyed;
    public event Action<GameObject, Transform> OnThisSheepDestroyed;

    private Transform spawnPoint;
    private bool isDestroyed = false;

    public void Initialize(Transform originalSpawnPoint)
    {
        spawnPoint = originalSpawnPoint;
    }

    private void OnDestroy()
    {
        if (!isDestroyed)
        {
            isDestroyed = true;
            OnThisSheepDestroyed?.Invoke(gameObject, spawnPoint);
            OnAnySheepDestroyed?.Invoke(gameObject, spawnPoint);
            Debug.Log($"Sheep destroyed at {transform.position}, spawn point will be recycled");
        }
    }
}