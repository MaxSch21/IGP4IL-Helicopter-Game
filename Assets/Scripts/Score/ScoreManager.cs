using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField, Min(0)] private int pointsPerPackage = 100;
    [SerializeField] private string highScoreKey = "HelicopterHighScore";

    private int temporaryScore;
    private int finalScore;
    private int highScore;
    private bool isNewHighScore;

    public event Action<int> OnTemporaryScoreChanged;
    public event Action<int, int, bool> OnFinalScoreChanged;

    public int TemporaryScore => temporaryScore;
    public int FinalScore => finalScore;
    public int HighScore => highScore;
    public bool IsNewHighScore => isNewHighScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetScore()
    {
        temporaryScore = 0;
        finalScore = 0;
        isNewHighScore = false;
        OnTemporaryScoreChanged?.Invoke(temporaryScore);
    }

    public void AddPackageScore()
    {
        temporaryScore += pointsPerPackage;
        OnTemporaryScoreChanged?.Invoke(temporaryScore);
    }

    public void FinalizeScore(int fuelBonusPoints = 0)
    {
        finalScore = temporaryScore + Mathf.Max(0, fuelBonusPoints);
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);

        if (finalScore > highScore)
        {
            highScore = finalScore;
            isNewHighScore = true;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
        else
        {
            isNewHighScore = false;
        }

        OnFinalScoreChanged?.Invoke(finalScore, highScore, isNewHighScore);
    }
}
