using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Flags]
public enum LamenessType
{
    None = 0,
    Footrot = 1 << 0,
    ToeGranuloma = 1 << 1,
    InterdigitalDermatitis = 1 << 2
}

[System.Serializable]
public class LamenessData
{
    public LamenessType type;
}

public class SheepIllnessManager : MonoBehaviour
{
    [SerializeField] private List<LamenessData> lamenessTypes = new List<LamenessData>();
    [SerializeField] [Range(0, 1)] private float illnessChancePerCondition = 0.3f;
    private HashSet<LamenessType> unseenIllnesses = new HashSet<LamenessType>();
    private int remainingLegs = 4;

    private void Awake()
    {
        InitializeDefaultLamenessTypes();
    }

    private void InitializeDefaultLamenessTypes()
    {
        lamenessTypes = new List<LamenessData>
        {
            new LamenessData { type = LamenessType.Footrot },
            new LamenessData { type = LamenessType.ToeGranuloma },
            new LamenessData { type = LamenessType.InterdigitalDermatitis }
        };
    }

    public List<LamenessData> AssignRandomLameness(GameObject sheep)
    {
        List<LamenessData> activeConditions = new List<LamenessData>();

        foreach (var condition in lamenessTypes)
        {
            if (Random.value < illnessChancePerCondition)
            {
                activeConditions.Add(condition);
            }
        }

        return activeConditions;
    }

    public void InitializeNewSheep(List<LamenessData> conditions)
    {
        unseenIllnesses.Clear();
        remainingLegs = 4;
        
        // Add all active illnesses to unseen set
        foreach (var condition in conditions)
        {
            unseenIllnesses.Add(condition.type);
        }
    }

    public HashSet<LamenessType> GetForcedIllnesses()
    {
        // If this is the last leg, force show all remaining unseen illnesses
        if (remainingLegs <= 1)
        {
            return new HashSet<LamenessType>(unseenIllnesses);
        }

        remainingLegs--;
        return new HashSet<LamenessType>();
    }

    public void MarkIllnessesAsSeen(IEnumerable<LamenessType> shownIllnesses)
    {
        foreach (var illness in shownIllnesses)
        {
            unseenIllnesses.Remove(illness);
        }
    }
}