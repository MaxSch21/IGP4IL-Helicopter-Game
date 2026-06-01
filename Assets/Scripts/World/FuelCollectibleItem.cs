using UnityEngine;

public class FuelCollectibleItem : CollectibleItem
{
    [SerializeField, Min(0f)] private float fuelAmount = 50f;

    protected override void Collect(GameManager gameManager)
    {
        gameManager.AddFuel(fuelAmount);
    }
}
