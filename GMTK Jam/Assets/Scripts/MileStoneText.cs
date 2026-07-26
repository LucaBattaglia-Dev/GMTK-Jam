using UnityEngine;
using TMPro;

public class MilestoneText : MonoBehaviour
{
    [Tooltip("Drag the '1000 Text' TMP object here")]
    [SerializeField] private TMP_Text distanceText;

    void Start()
    {
        // If you forgot to assign it in the inspector, try to find one automatically
        if (distanceText == null)
        {
            distanceText = GetComponentInChildren<TMP_Text>();
        }

        if (distanceText != null)
        {
            // Grab the Z position of this prefab, round it, and add "m"
            int zDistance = Mathf.RoundToInt(transform.position.z);
            distanceText.text = zDistance.ToString() + "m";
        }
        else
        {
            Debug.LogWarning("MilestoneText script is missing a reference to a TMP_Text component!");
        }
    }
}