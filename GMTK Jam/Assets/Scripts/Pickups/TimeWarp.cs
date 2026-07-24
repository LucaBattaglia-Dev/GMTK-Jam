using UnityEngine;

public class TimeWarp : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float newTimeScale; 
    public override void OnPickup(){
        //Add slowing down trucks & player
        LevelUIManager.Instance.InstantiateCooldownUI(this); 
    }

    public override void OnDespawn(){
        //Add slowing down trucks & player
    }
}
