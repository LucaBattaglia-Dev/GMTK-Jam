using UnityEngine;

public class TimeWarp : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float newTimeScale; 

    public override void OnPickup(){
        Time.timeScale = newTimeScale;
    }

    public override void OnDespawn(){
        Time.timeScale = 1f;
    }
}
