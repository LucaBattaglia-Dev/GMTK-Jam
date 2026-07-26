using UnityEngine;
using UnityEngine.UI; 

public class SprintDisplay : MonoBehaviour
{
    private RectTransform sprintBar;
    private Image fillImage; 
    public PlayerMovement player;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color superSpeedColor = Color.red;
    public Color exhaustedColor = new Color(1f, 0.5f, 0f); // Orange

    void Start()
    {
        sprintBar = transform.Find("Fill") as RectTransform;
        
        if (sprintBar != null)
        {
            fillImage = sprintBar.GetComponent<Image>();
        }
    }

    void Update()
    {
        float currentBar = player.SprintTimer / player.sprintTime;
        sprintBar.localScale = new Vector3(currentBar, 1f, 1f);

        // Handle Color Changes
        if (fillImage != null)
        {
            // Super speed takes top priority so it instantly turns red even during regen/orange states
            if (player.IsSuperSpeeding)
            {
                fillImage.color = superSpeedColor;
            }
            else if (player.IsExhausted)
            {
                fillImage.color = exhaustedColor;
            }
            else
            {
                fillImage.color = normalColor;
            }
        }
    }
}