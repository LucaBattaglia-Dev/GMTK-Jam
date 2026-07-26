using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

//Also keeps track of the player's scoare
public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private GameObject powerupCooldownParent; 
    [SerializeField] private GameObject powerupDurationPrefab;
    [SerializeField] private SprintDisplay sprintBar; 
    [SerializeField] private Transform distanceTextParent; 
    [Header("Additional Time UI")]
    [SerializeField] public int poolSize;
    [SerializeField] public Transform poolParent;  
    private int poolCounter = 0;
    [Header("From the game over screen")]
    [SerializeField] private GameObject gameOverScreen; 
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private Transform totalDistanceParent;
    private TMP_Text[] distanceTextArray = new TMP_Text[5];
    private TMP_Text[] gameOverTextArray = new TMP_Text[5];
    private float distance;
    private PlayerCam playerCamScript; 
    public float Distance {get{return distance;} set{distance = value;}}
    private Transform playerTracker; 
    private Transform player; 
    public Transform Player {get{return player;}}
    private List<System.Type> currentPickups = new List<System.Type>(); 
    private List<Pickup> currentPickupScripts = new List<Pickup>();

    static private LevelUIManager instance;
    static public LevelUIManager Instance
    {
        get {
            if (instance == null) {
                Debug.LogError("There is no LevelUIManager in the scene.");
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

    void Start(){
        playerCamScript = Camera.main.gameObject.GetComponent<PlayerCam>();
        for(int i = 0; i < distanceTextArray.Length; i++){
            distanceTextArray[i] = distanceTextParent.GetChild(i).GetChild(0).gameObject.GetComponent<TMP_Text>();
        }
        for(int i = 0; i < gameOverTextArray.Length; i++){
            gameOverTextArray[i] = totalDistanceParent.GetChild(i).GetChild(0).gameObject.GetComponent<TMP_Text>();
        }


        player = sprintBar.player.gameObject.transform;
        playerTracker = new GameObject("Player Tracker").transform;
        playerTracker.SetParent(player);
        playerTracker.localPosition = Vector3.zero;
        playerTracker.SetParent(null);
    }

    public void InstantiateCooldownUI(Pickup pickup){
        if(currentPickups.Contains(pickup.GetType())){ //check if it's a pickup we already have
            int index = currentPickups.IndexOf(pickup.GetType());
            currentPickupScripts[index].RestartTimer();
        } else {
            currentPickups.Add(pickup.GetType());
            currentPickupScripts.Add(pickup);
            GameObject newObject = Instantiate(powerupDurationPrefab, powerupCooldownParent.transform);
            newObject.GetComponent<PickupCooldown>().Setup(pickup);
        } 
    }

    //removes a pickup from the list that tracks them
    public void RemovePickup(Pickup pickup){
        if(currentPickups.Contains(pickup.GetType())){ //check if it's a pickup we already have
            int index = currentPickups.IndexOf(pickup.GetType());
            currentPickupScripts.RemoveAt(index);
            currentPickups.RemoveAt(index);
        }
    }

    public void UpdateDistanceUI(Transform uiParent, TMP_Text[] textArray){
        string temp = (int)distance + "";
        char[] scoreCharacter = temp.ToCharArray();

        for(int i = 0; i < scoreCharacter.Length; i++){
            uiParent.GetChild(i).gameObject.SetActive(true);
            textArray[i].text = (scoreCharacter[i] + "");
        }
    }

    void FixedUpdate(){
        //Get Z difference from player tracker and player
        distance = player.position.z - playerTracker.position.z;
        UpdateDistanceUI(distanceTextParent, distanceTextArray); 
    }

    //Show game over screen with total time and distance text
    public void GameOver(){
        totalTimeText.text = TimeManager.Instance.TotalTime.ToString("F2");
        UpdateDistanceUI(totalDistanceParent, gameOverTextArray);
        gameOverScreen.SetActive(true); 
        Time.timeScale = 0.001f;
        playerCamScript.UnlockCursor();
    }

    public void OnRetry(){
        BGMManager.Instance.ChangeToLevelMusic();
        SceneManager.LoadScene("Level1");
    }

    public void AddTimeUI(float value, bool isPositive){
        if(poolCounter == poolSize){
            poolCounter = 0; 
        }
        poolParent.GetChild(poolCounter).gameObject.SetActive(true); 
        poolCounter++;
    }

    #endregion
}
