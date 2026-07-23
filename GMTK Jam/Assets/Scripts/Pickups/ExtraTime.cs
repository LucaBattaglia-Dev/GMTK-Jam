using UnityEngine;

public class ExtraTime : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float timeAdded; 

    public override void OnPickup(){
        TimeManager.Instance.AddTime(timeAdded);
    }


}
