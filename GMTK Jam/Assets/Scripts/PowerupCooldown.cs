using UnityEngine;
using UnityEngine.UI;

public class PowerupCooldown : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cooldownCircle;
    private Pickup powerup; //reference to the powerup it represents

    void Setup(Pickup powerup){
        this.powerup = powerup;
    }

    void Update(){
        if(powerup){
            cooldownCircle.fillAmount = powerup.CurrentDuration/powerup.powerupDuration; 
        }
    }
    
    
}
