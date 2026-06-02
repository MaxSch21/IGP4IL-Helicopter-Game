using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HelicopterServiceStation : MonoBehaviour
{
    public enum ServiceType
    {
        Fuel,
        Repair
    }

    [SerializeField] private ServiceType serviceType = ServiceType.Fuel;
    [SerializeField, Min(0.01f)] private float tickInterval = 1f;
    [SerializeField, Min(0f)] private float fuelPerTick = 10f;
    [SerializeField, Min(1)] private int repairPerTick = 1;
    [SerializeField] private ParticleSystem serviceEffect;

    private HelicopterController currentHelicopter;
    private Coroutine serviceRoutine;

    private void Reset()
    {
        Collider2D stationCollider = GetComponent<Collider2D>();
        stationCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HelicopterController helicopter = other != null ? other.GetComponentInParent<HelicopterController>() : null;
        if (helicopter == null)
            return;

        currentHelicopter = helicopter;
        StartServiceRoutine();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HelicopterController helicopter = other != null ? other.GetComponentInParent<HelicopterController>() : null;
        if (helicopter == null || helicopter != currentHelicopter)
            return;

        currentHelicopter = null;
        StopServiceRoutine();
        StopServiceEffect();
    }

    private void OnDisable()
    {
        currentHelicopter = null;
        StopServiceRoutine();
        StopServiceEffect();
    }

    private void StartServiceRoutine()
    {
        StopServiceRoutine();
        serviceRoutine = StartCoroutine(ServiceLoop());
    }

    private void StopServiceRoutine()
    {
        if (serviceRoutine == null)
            return;

        StopCoroutine(serviceRoutine);
        serviceRoutine = null;
    }

    private IEnumerator ServiceLoop()
    {
        while (currentHelicopter != null)
        {
            ApplyService();
            yield return new WaitForSeconds(tickInterval);
        }

        serviceRoutine = null;
    }

    private void ApplyService()
    {
        GameManager gameManager = GameManager.Instance;
        if (!CanService(gameManager))
        {
            StopServiceEffect();
            return;
        }

        if (serviceType == ServiceType.Fuel)
        {
            float fuelBefore = gameManager.CurrentFuel;
            gameManager.AddFuel(fuelPerTick);
            UpdateServiceEffect(gameManager.CurrentFuel > fuelBefore);
            return;
        }

        int conditionBefore = gameManager.CurrentHeliCondition;
        gameManager.RepairHelicopter(repairPerTick);
        UpdateServiceEffect(gameManager.CurrentHeliCondition > conditionBefore);
    }

    private bool CanService(GameManager gameManager)
    {
        if (gameManager == null)
            return false;

        GameManager.GameState state = gameManager.CurrentState;
        return state != GameManager.GameState.FuelDepleted &&
               state != GameManager.GameState.GameOver &&
               state != GameManager.GameState.Win;
    }

    private void UpdateServiceEffect(bool isActivelyServicing)
    {
        if (serviceEffect == null)
            return;

        if (isActivelyServicing)
        {
            if (!serviceEffect.isPlaying)
                serviceEffect.Play();

            return;
        }

        StopServiceEffect();
    }

    private void StopServiceEffect()
    {
        if (serviceEffect == null)
            return;

        serviceEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
