using UnityEngine;
using UnityEngine.UI;

public class SprintDisplay : MonoBehaviour
{
    private RectTransform sprintBar;
    public PlayerMovement player;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprintBar = transform.Find("Fill") as RectTransform;
    }

    // Update is called once per frame
    void Update()
    {
        float currentBar = player.SprintTimer / player.sprintTime;
        sprintBar.localScale = new Vector3(currentBar, 1f, 1f);
    }
}
