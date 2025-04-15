using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartActivator : MonoBehaviour
{
    [System.Serializable]
    private class IllnessPartMapping
    {
        public LamenessType illnessType;
        public GameObject[] parts;
    }

    [SerializeField] private List<IllnessPartMapping> partMappings = new List<IllnessPartMapping>();
    [SerializeField] [Range(1, 4)] private int maxPartsToShow = 2; // Per illness, minimum of 1
    private HashSet<LamenessType> activeIllnesses = new HashSet<LamenessType>();
    private HashSet<LamenessType> forcedIllnesses = new HashSet<LamenessType>();

    public void SetActiveIllnesses(List<LamenessData> illnesses, HashSet<LamenessType> forceShow)
    {
        activeIllnesses.Clear();
        forcedIllnesses = forceShow;
        DeactivateAllParts();

        foreach (var illness in illnesses)
        {
            activeIllnesses.Add(illness.type);
        }

        UpdateVisibleParts();
    }

    private void DeactivateAllParts()
    {
        foreach (var mapping in partMappings)
        {
            foreach (var part in mapping.parts)
            {
                if (part != null)
                {
                    part.SetActive(false);
                }
            }
        }
    }

    private void UpdateVisibleParts()
    {
        foreach (var mapping in partMappings)
        {
            if (!activeIllnesses.Contains(mapping.illnessType) || mapping.parts.Length == 0) 
                continue;

            bool shouldShow = forcedIllnesses.Contains(mapping.illnessType) || 
                            Random.value < 0.5f; // 50% chance to show if not forced

            if (shouldShow)
            {
                // Show at least one random part
                int randomIndex = Random.Range(0, mapping.parts.Length);
                if (mapping.parts[randomIndex] != null)
                {
                    mapping.parts[randomIndex].SetActive(true);
                }

                // Possibly show additional parts
                if (maxPartsToShow > 1)
                {
                    var remainingParts = new List<GameObject>(mapping.parts);
                    remainingParts.RemoveAt(randomIndex);

                    int extraParts = Random.Range(0, Mathf.Min(maxPartsToShow - 1, remainingParts.Count));
                    for (int i = 0; i < extraParts; i++)
                    {
                        int extraIndex = Random.Range(0, remainingParts.Count);
                        if (remainingParts[extraIndex] != null)
                        {
                            remainingParts[extraIndex].SetActive(true);
                        }
                        remainingParts.RemoveAt(extraIndex);
                    }
                }
            }
        }
    }
}
