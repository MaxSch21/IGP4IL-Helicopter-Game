using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    [Min(1)]
    public int levelIndex = 1;

    public string displayName = "Level 1";
    public string sceneName;
    [Min(1)] public int requiredPackages = 3;
    [Min(0.01f)] public float startFuel = 100f;
    [Min(0.01f)] public float estimatedTimeSeconds = 60f;

    public bool IsConfigured =>
        levelIndex > 0 &&
        !string.IsNullOrWhiteSpace(sceneName) &&
        requiredPackages > 0 &&
        startFuel > 0f &&
        estimatedTimeSeconds > 0f;
}
