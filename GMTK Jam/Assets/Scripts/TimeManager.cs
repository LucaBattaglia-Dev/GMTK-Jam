using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float startingTime; 
    [SerializeField] private float lowTimeThreshold; //When will the timer turn red
    [SerializeField] private Color lowTimeColor; 
    private float currentTime;
    private float totalTime = 0;

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
        currentTime = startingTime; 
    }

    void FixedUpdate()
    {
        currentTime -= Time.deltaTime;  
        totalTime += Time.deltaTime;
        UpdateTime(); 
    }

    //Updates the time string and checks for certain thresholds
    public void UpdateTime(){
       timeText.text = currentTime.ToString("F2"); 

       if(currentTime < lowTimeThreshold){
            timeText.color = lowTimeColor;
       } else if (currentTime <= 0){
            //gameOver
       }
    }

    //Can be used to add or subtract time
    public void AddTime(float time){
        currentTime += time; 
        UpdateTime();
    }

    #endregion
}
