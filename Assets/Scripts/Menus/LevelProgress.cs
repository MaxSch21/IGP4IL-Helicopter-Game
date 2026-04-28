using UnityEngine;

public static class LevelProgress
{
    private const int FirstLevelIndex = 1;
    private const string HighestUnlockedLevelKey = "HighestUnlockedLevel";
    private const string CompletedPrefix = "Level_";
    private const string CompletedSuffix = "_Complete";

    public static bool IsUnlocked(int levelIndex)
    {
        if (levelIndex <= FirstLevelIndex)
            return true;

        return levelIndex <= GetHighestUnlockedLevel();
    }

    public static bool IsCompleted(int levelIndex)
    {
        return PlayerPrefs.GetInt(GetCompletedKey(levelIndex), 0) == 1;
    }

    public static int GetHighestUnlockedLevel()
    {
        return Mathf.Max(FirstLevelIndex, PlayerPrefs.GetInt(HighestUnlockedLevelKey, FirstLevelIndex));
    }

    public static void CompleteLevel(int levelIndex)
    {
        if (levelIndex < FirstLevelIndex)
            return;

        PlayerPrefs.SetInt(GetCompletedKey(levelIndex), 1);
        UnlockLevel(levelIndex + 1);
        PlayerPrefs.Save();
    }

    public static void UnlockLevel(int levelIndex)
    {
        if (levelIndex < FirstLevelIndex)
            return;

        int highestUnlocked = GetHighestUnlockedLevel();
        if (levelIndex > highestUnlocked)
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }
    }

    public static void ResetAll(int maxLevelIndex)
    {
        for (int i = FirstLevelIndex; i <= maxLevelIndex; i++)
            PlayerPrefs.DeleteKey(GetCompletedKey(i));

        PlayerPrefs.SetInt(HighestUnlockedLevelKey, FirstLevelIndex);
        PlayerPrefs.Save();
    }

    public static bool HasAnyCompleted(int maxLevelIndex)
    {
        for (int i = FirstLevelIndex; i <= maxLevelIndex; i++)
        {
            if (IsCompleted(i))
                return true;
        }

        return false;
    }

    public static void SetCompleted(int levelIndex)
    {
        CompleteLevel(levelIndex);
    }

    public static void SetCompleted(int levelIndex, bool completed)
    {
        if (completed)
        {
            CompleteLevel(levelIndex);
            return;
        }

        PlayerPrefs.DeleteKey(GetCompletedKey(levelIndex));
        PlayerPrefs.Save();
    }

    public static void ResetCompleted(int levelIndex)
    {
        SetCompleted(levelIndex, false);
    }

    private static string GetCompletedKey(int levelIndex)
    {
        return $"{CompletedPrefix}{levelIndex}{CompletedSuffix}";
    }
}
