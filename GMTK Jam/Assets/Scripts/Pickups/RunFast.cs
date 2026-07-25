using UnityEngine;

public class RunFast : Pickup
{
    public override void OnPickup()
    {
        //Increase player speed
        LevelUIManager.Instance.InstantiateCooldownUI(this);

    }

    public override void OnDespawn()
    {
        //Decrease player speed
    }
}
