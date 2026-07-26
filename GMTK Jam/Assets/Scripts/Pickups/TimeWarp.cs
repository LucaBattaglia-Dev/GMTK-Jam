using UnityEngine;

public class TimeWarp : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float newTimeScale;
    
    public override void OnPickup(){
        //Add slowing down trucks & player
        LevelUIManager.Instance.InstantiateCooldownUI(this);
        TruckDriver.GlobalSpeedMultiplier = 0.1f;

        //Turn on black and white overlay
        LevelUIManager.Instance.Player.GetChild(0).gameObject.SetActive(true);
    }

    public override void OnDespawn(){
        //Add slowing down trucks & player
        TruckDriver.GlobalSpeedMultiplier = 1f;

        //Turn off black and white overlay
        LevelUIManager.Instance.Player.GetChild(0).gameObject.SetActive(false);
    }
}
