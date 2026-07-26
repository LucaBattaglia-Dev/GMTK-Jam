using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float startingTime; 
    [SerializeField] private float lowTimeThreshold; 
    [SerializeField] private Color lowTimeColor; 
    private float currentTime;
    private float totalTime = 0;
    public float TotalTime { get { return totalTime; } }
    private bool gameOver = false; 
    private bool isCountingDown = false;

    [Header("Super Speed Timer Settings")]
    [SerializeField] private float superSpeedTimeMultiplier = 3f; 
    [SerializeField] private Color superSpeedColor = Color.red;
    [SerializeField] private Color maroonColor = new Color(0.5f, 0f, 0f); 

    [Header("UI Elements")]
    [SerializeField] private TMP_Text timeText; 

    private PlayerMovement playerMovement;
    private Color defaultTextColor;

    static private TimeManager instance;
    static public TimeManager Instance
    {
        get {
            if (instance == null) {
                Debug.LogError("There is no TimeManager in the scene.");
            }
            return instance;
        }
    }

    #region Methods
    void Awake(){
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        }
        else {
            instance = this;
        }
    }

    void Start()
    {
        currentTime = startingTime; 
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (timeText != null)
        {
            defaultTextColor = timeText.color;
        }

        UpdateTime();
    }

    void FixedUpdate()
    {
        if(!gameOver && isCountingDown){
            float timeMultiplier = 1f;

            // Apply Super Speed multiplier if active (drains 3x faster)
            if (playerMovement != null && playerMovement.IsSuperSpeeding)
            {
                timeMultiplier = superSpeedTimeMultiplier;
            }

            // Apply TimeWarp / Slow-mo multiplier if active (e.g., 0.1f for 10x slow down)
            if (TruckDriver.GlobalSpeedMultiplier < 1f)
            {
                timeMultiplier *= TruckDriver.GlobalSpeedMultiplier;
            }

            currentTime -= Time.deltaTime * timeMultiplier;  
            
            if(currentTime <= 0){
                currentTime = 0;
                gameOver = true; 
                EndLevel();
            } else {
                totalTime += Time.deltaTime;
                UpdateTime(); 
            }
        }
    }

    public void StartCountdown()
    {
        isCountingDown = true;
    }

    public void EndLevel(){
        timeText.text = "0.00";
        LevelUIManager.Instance.GameOver();
    }

    public void UpdateTime(){
        timeText.text = currentTime.ToString("F2"); 

        bool isSuperSpeed = (playerMovement != null && playerMovement.IsSuperSpeeding);
        bool isLowTime = currentTime < lowTimeThreshold;

        if (timeText != null)
        {
            if (isSuperSpeed && isLowTime)
            {
                timeText.color = maroonColor; 
            }
            else if (isSuperSpeed)
            {
                timeText.color = superSpeedColor; 
            }
            else if (isLowTime)
            {
                timeText.color = lowTimeColor; 
            }
            else
            {
                timeText.color = defaultTextColor; 
            }
        }
    }

    public void AddTime(float time){
        currentTime += time; 
        UpdateTime();
    }

    #endregion
}