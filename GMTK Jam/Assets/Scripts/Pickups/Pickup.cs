using DG.Tweening;
using UnityEngine;

//TODO: finish code to turn off renderer
//Parent class for pickups, also handles animating them
public class Pickup : MonoBehaviour
{
    [SerializeField] public AudioClip pickupSFX;
    [SerializeField] public float SFXVolume;
    [SerializeField] public Sprite pickupIcon;
    [SerializeField] public float powerupDuration;
    private float currentDuration;
    public float CurrentDuration {get{return currentDuration;} set{currentDuration = value;}}
    private bool isActive = false; 
    private float originalYPos;
    private PickupCooldown cooldownUI;
    public PickupCooldown CooldownUI {get{return cooldownUI;} set{cooldownUI = value;}}
    AudioSource audioPlayer;

    //Animation Params
    private float floatDistance = 0.8f;
    private float floatDuration = 2f;

    #region class methods
    void Start(){
        currentDuration = powerupDuration;
        originalYPos = transform.position.y;

        Sequence floatSequence = DOTween.Sequence();
        floatSequence.Append(transform.DOMoveY(originalYPos + floatDistance, floatDuration/2).SetEase(Ease.InOutSine));
        floatSequence.Append(transform.DOMoveY(originalYPos, floatDuration/2).SetEase(Ease.InOutSine));
        floatSequence.SetLoops(-1, LoopType.Yoyo);
    }

    //For when you get a pickup that you already have. Resets the timer and color
    public void RestartTimer(){
        currentDuration = powerupDuration;
        if(cooldownUI){
            cooldownUI.ResetColor(); 
        }
    }

    private void OnTriggerEnter(Collider collision){
        PlaySFX();
        isActive = true;
        OnPickup();
        this.gameObject.GetComponent<MeshRenderer>().enabled = false; 
        this.gameObject.GetComponent<Collider>().enabled = false;
    }

    public void PlaySFX(){
        SFXManager.Instance.PlaySFX(pickupSFX, SFXVolume);
    }

    void FixedUpdate(){
        if(isActive){
            currentDuration -= Time.unscaledDeltaTime; 
            if(currentDuration <= 0){
                OnDespawn();
                LevelUIManager.Instance.RemovePickup(this);
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
