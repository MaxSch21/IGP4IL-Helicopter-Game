using UnityEngine;

public struct LevelResult
{
    public int LevelIndex;
    public int PackagesDelivered;
    public int RequiredPackages;
    public int DamageTaken;
    public float FuelRemaining;
    public float ElapsedSeconds;
    public float EstimatedTimeSeconds;

    public bool IsValid =>
        LevelIndex > 0 &&
        RequiredPackages > 0 &&
        ElapsedSeconds >= 0f &&
        EstimatedTimeSeconds > 0f;
}
