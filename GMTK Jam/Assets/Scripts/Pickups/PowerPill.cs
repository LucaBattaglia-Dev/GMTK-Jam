using UnityEngine;

public class PowerPill : Pickup
{
    public override void OnPickup()
    {
        //Disable Time Consumption on Moves
        LevelUIManager.Instance.InstantiateCooldownUI(this);

    }

    public override void OnDespawn()
    {
        //Enable Time Consumption on Moves
    }
}
