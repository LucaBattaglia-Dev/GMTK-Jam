using UnityEngine;

public class StaminaRefill : Pickup
{
    public override void OnPickup(){
        PlayerMovement movementScript = LevelUIManager.Instance.Player.gameObject.GetComponent<PlayerMovement>();
        movementScript.SprintTimer = (movementScript.sprintTime/3f); 
        //LevelUIManager.Instance.GainStaminaUI(); 
    }
}
