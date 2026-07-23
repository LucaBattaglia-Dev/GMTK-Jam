using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PickupCooldown : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cooldownCircle;
    [SerializeField] private Image pickupIcon;
    private Pickup powerup; //reference to the powerup it represents
    [SerializeField] private Color endColor;

    public void Setup(Pickup powerup){
        this.powerup = powerup;
        pickupIcon.sprite = powerup.pickupIcon;
        cooldownCircle.DOColor(endColor, powerup.powerupDuration).SetEase(Ease.Linear).SetUpdate(true);
    }

    void Update(){
        if(powerup){
            cooldownCircle.fillAmount = powerup.CurrentDuration/powerup.powerupDuration; 
            if(powerup.CurrentDuration - Time.deltaTime <= 0){
                Destroy(this.gameObject);
            }
        }
    }
    
    
}
