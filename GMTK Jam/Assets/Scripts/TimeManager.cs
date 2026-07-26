using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float startingTime; 
    [SerializeField] private float lowTimeThreshold; //When will the timer turn red
    [SerializeField] private Color lowTimeColor; 
    private Color originalTextColor; 
    private float currentTime;
    private float totalTime = 0;
    public float TotalTime {get{return totalTime;}}
    private bool gameOver = false; 

    [Header("UI Elements")]
    [SerializeField] private TMP_Text timeText; 

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
        //Singleton Stuff
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        }
        else {
            instance = this;
        }
    }

    void Start()
    {
        originalTextColor = timeText.color; 
        currentTime = startingTime; 
    }

    void FixedUpdate()
    {
        if(!gameOver){
            currentTime -= Time.deltaTime;  
            if(currentTime <=0){
                gameOver = true; 
                EndLevel();
            } else {
                totalTime += Time.deltaTime;
                UpdateTime(); 
            }
        }
    }

    public void EndLevel(){
        timeText.text = "0";
        LevelUIManager.Instance.GameOver();
    }

    //Updates the time string and checks for certain thresholds
    public void UpdateTime(){
       timeText.text = currentTime.ToString("F2"); 

       if(currentTime < lowTimeThreshold){
            timeText.color = lowTimeColor;
       }
    }

    //Can be used to add or subtract time
    public void AddTime(float time){
        currentTime += time; 
        UpdateTime();

        if(currentTime > lowTimeThreshold){
            timeText.color = originalTextColor;
       }
    }

    #endregion
}
