using UnityEngine;
using DG.Tweening;

//TODO: finish code to turn off renderer
//Parent class for pickups, also handles animating them
public class Pickup : MonoBehaviour
{
    [SerializeField] public AudioClip pickupSFX; 
    [SerializeField] public float powerupDuration;
    private float currentDuration;
    public float CurrentDuration {get{return currentDuration;}}
    private bool isActive = false; 

    #region class methods
    void Start(){
        currentDuration = powerupDuration;
    }

    private void OnTriggerEnter(Collider collision){
        PlaySFX();
        isActive = true;
        OnPickup();
        //Turn off whatever's rendering it
    }

    public void PlaySFX(){
    }

    void FixedUpdate(){
        if(isActive){
            currentDuration -= Time.unscaledDeltaTime; 
            if(currentDuration <= 0){
                OnDespawn();
                Destroy(this.gameObject);
            }
        }
    }
    #endregion

    #region to override
    public virtual void OnPickup(){
    }

    public virtual void OnDespawn(){
    }
    #endregion
}
