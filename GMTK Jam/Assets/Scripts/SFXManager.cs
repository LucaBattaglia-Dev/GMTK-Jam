using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] public AudioSource tempAudioSource;
    public float sfxVolumeMultiplier = 1;
    private AudioSource superSpeedSource; 

    [Header("Specific Audio Sources")]
    [SerializeField] private AudioClip superSpeedSFX; 

    private static SFXManager instance;
    public static SFXManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("There is no SFXManager instance in the scene.");
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            this.transform.parent = null;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    void Start(){
        superSpeedSource = Instantiate(tempAudioSource, transform.position, Quaternion.identity); 
        superSpeedSource.Pause();
        superSpeedSource.loop = true; 
        superSpeedSource.clip = superSpeedSFX; 
        superSpeedSource.volume = 1 * sfxVolumeMultiplier; 
    }

    public void PlaySuperSpeed(){
        superSpeedSource.Play();
    }

    public void StopSuperSpeed(){
        superSpeedSource.Stop(); 
    }


    public void PlaySFX(AudioClip clip, float volume)
    {
        AudioSource source = Instantiate(tempAudioSource, transform.position, Quaternion.identity);
        source.clip = clip;
        source.volume = volume * sfxVolumeMultiplier;

        source.Play();

        float length = source.clip.length;

        Destroy(source.gameObject, length);
    }
}
