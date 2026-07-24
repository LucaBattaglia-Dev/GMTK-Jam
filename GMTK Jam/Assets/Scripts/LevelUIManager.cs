using UnityEngine;
using TMPro;

//Also keeps track of the player's scoare
public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private GameObject powerupCooldownParent; 
    [SerializeField] private GameObject powerupDurationPrefab;
    [SerializeField] private SprintDisplay sprintBar; 
    [SerializeField] private Transform distanceTextParent; 
    private TMP_Text[] distanceTextArray = new TMP_Text[5];
    private float distance;
    public float Distance {get{return distance;} set{distance = value;}}
    private Transform playerTracker; 
    private Transform player; 

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
        for(int i = 0; i < distanceTextArray.Length; i++){
            distanceTextArray[i] = distanceTextParent.GetChild(i).GetChild(0).gameObject.GetComponent<TMP_Text>();
        }


        player = sprintBar.player.gameObject.transform;
        playerTracker = new GameObject("Player Tracker").transform;
        playerTracker.SetParent(player);
        playerTracker.localPosition = Vector3.zero;
        playerTracker.SetParent(null);
    }

    public void InstantiateCooldownUI(Pickup pickup){
        GameObject newObject = Instantiate(powerupDurationPrefab, powerupCooldownParent.transform);
        newObject.GetComponent<PickupCooldown>().Setup(pickup); 
    }

    public void UpdateDistanceUI(){
        string temp = (int)distance + "";
        char[] scoreCharacter = temp.ToCharArray();

        for(int i = 0; i < scoreCharacter.Length; i++){
            distanceTextParent.GetChild(i).gameObject.SetActive(true);
            distanceTextArray[i].text = (scoreCharacter[i] + "");
        }

    }

    void FixedUpdate(){
        //Get Z difference from player tracker and player
        distance = player.position.z - playerTracker.position.z;
        UpdateDistanceUI(); 


    }

    #endregion
}
