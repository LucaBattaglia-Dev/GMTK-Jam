using UnityEngine;

public class StartingLine : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object passing through has the PlayerMovement component attached
        if (!hasTriggered && other.GetComponent<PlayerMovement>() != null)
        {
            hasTriggered = true;

            // Tell the TimeManager singleton to start the countdown
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.StartCountdown();
            }

            // Disable this starting line object so it only triggers once
            gameObject.SetActive(false);
        }
    }
}