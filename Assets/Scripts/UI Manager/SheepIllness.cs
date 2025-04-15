public enum SheepIllness
{
    None,
    BrokenLeg,
    Fever,
    Infection,
    Poisoned
}

[System.Serializable]
public class IllnessData
{
    public SheepIllness type;
    
    public string description;
    public int requiredFootCount = 4;
}