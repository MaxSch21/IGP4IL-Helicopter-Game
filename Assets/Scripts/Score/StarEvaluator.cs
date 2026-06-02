using UnityEngine;

public static class StarEvaluator
{
    private const int MaxStars = 5;
    private const int MinStarsOnWin = 1;
    private const float TimePenaltyStepSeconds = 5f;
    private const int MaxTimePenaltySteps = 3;

    public static int Evaluate(LevelResult result)
    {
        if (!result.IsValid)
            return 0;

        int damagePenalty = EvaluateDamagePenalty(result.DamageTaken);
        int timePenalty = EvaluateTimePenalty(result.ElapsedSeconds, result.EstimatedTimeSeconds);
        int stars = MaxStars - damagePenalty - timePenalty;

        return Mathf.Clamp(stars, MinStarsOnWin, MaxStars);
    }

    private static int EvaluateDamagePenalty(int damageTaken)
    {
        return Mathf.Max(0, damageTaken);
    }

    private static int EvaluateTimePenalty(float elapsedSeconds, float estimatedTimeSeconds)
    {
        if (estimatedTimeSeconds <= 0f || elapsedSeconds <= estimatedTimeSeconds)
            return 0;

        float overtimeSeconds = elapsedSeconds - estimatedTimeSeconds;
        int penaltySteps = Mathf.CeilToInt(overtimeSeconds / TimePenaltyStepSeconds);

        return Mathf.Clamp(penaltySteps, 0, MaxTimePenaltySteps);
    }
}
