using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PickupCooldown : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cooldownCircle;
    [SerializeField] private Image pickupIcon;
    private Pickup powerup; //reference to the powerup it represents
    private Color startColor;
    private Sequence colorSequence;
    [SerializeField] private Color endColor;

    public void Setup(Pickup powerup){
        Debug.Log("Setup");
        startColor = cooldownCircle.color; 
        this.powerup = powerup;
        powerup.CooldownUI = this;
        pickupIcon.sprite = powerup.pickupIcon;

        colorSequence = DOTween.Sequence();
        colorSequence.Append(cooldownCircle.DOColor(endColor, powerup.powerupDuration).SetEase(Ease.Linear).SetUpdate(true));
    }

    public void ResetColor(){
        colorSequence.Pause();
        cooldownCircle.color = startColor;
        colorSequence.Restart(); 
    }

    void Update(){
        if(powerup){
            cooldownCircle.fillAmount = powerup.CurrentDuration/powerup.powerupDuration; 
        }
    }
    
    
}
