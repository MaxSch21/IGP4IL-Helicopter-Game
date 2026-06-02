using UnityEngine;

public static class StarEvaluator
{
    public static int Evaluate(LevelResult result)
    {
        if (!result.IsValid)
            return 0;

        int damageStars = Mathf.Clamp(3 - Mathf.Max(0, result.DamageTaken), 0, 3);
        int timeStars = EvaluateTimeStars(result.ElapsedSeconds, result.EstimatedTimeSeconds);
        return Mathf.Clamp(damageStars + timeStars, 0, 5);
    }

    private static int EvaluateTimeStars(float elapsedSeconds, float estimatedTimeSeconds)
    {
        if (estimatedTimeSeconds <= 0f)
            return 0;

        if (elapsedSeconds <= estimatedTimeSeconds)
            return 2;

        if (elapsedSeconds <= estimatedTimeSeconds + 5f)
            return 1;

        return 0;
    }
}
