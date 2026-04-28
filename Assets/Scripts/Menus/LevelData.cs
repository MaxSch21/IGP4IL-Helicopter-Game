using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    [Min(1)]
    public int levelIndex = 1;

    public string displayName = "Level 1";
    public string sceneName;

    public bool IsConfigured => levelIndex > 0 && !string.IsNullOrWhiteSpace(sceneName);
}
