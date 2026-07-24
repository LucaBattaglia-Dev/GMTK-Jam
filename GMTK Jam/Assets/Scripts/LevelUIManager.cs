using UnityEngine;

public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private GameObject powerupCooldownParent; 
    [SerializeField] private GameObject powerupDurationPrefab;

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

    public void InstantiateCooldownUI(Pickup pickup){
        GameObject newObject = Instantiate(powerupDurationPrefab, powerupCooldownParent.transform);
        newObject.GetComponent<PickupCooldown>().Setup(pickup); 
    }

    #endregion
}
