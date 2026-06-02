public static class LevelRuntime
{
    public static LevelRuntimeData Current { get; private set; }
    public static bool HasLevelData { get; private set; }

    public static void Set(LevelData levelData)
    {
        if (levelData == null)
        {
            Clear();
            return;
        }

        Current = new LevelRuntimeData
        {
            LevelIndex = levelData.levelIndex,
            DisplayName = levelData.displayName,
            SceneName = levelData.sceneName,
            RequiredPackages = levelData.requiredPackages,
            StartFuel = levelData.startFuel,
            EstimatedTimeSeconds = levelData.estimatedTimeSeconds
        };

        HasLevelData = true;
    }

    public static void Clear()
    {
        Current = default;
        HasLevelData = false;
    }
}

public struct LevelRuntimeData
{
    public int LevelIndex;
    public string DisplayName;
    public string SceneName;
    public int RequiredPackages;
    public float StartFuel;
    public float EstimatedTimeSeconds;

    public bool IsValid =>
        LevelIndex > 0 &&
        RequiredPackages > 0 &&
        StartFuel > 0f &&
        EstimatedTimeSeconds > 0f;
}
