using UnityEngine;

public class BGMManager : MonoBehaviour
{
    static private BGMManager instance;
    [SerializeField] public AudioClip menuBGM;
    [SerializeField] public AudioClip levelBGM;
    [SerializeField] private bool isLevel = false; 
    float baseMultiplier = 0.75f;
    private float bgmVolume;
    public float BGMVolume {set {bgmVolume = value;source.volume = value;}}
    private AudioSource source;
    public AudioSource Source {get { return source; }}

    static public BGMManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("There is no BGM Manager instance in the scene.");
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
        transform.SetParent(null);
        DontDestroyOnLoad(this.gameObject);
        AudioListener.pause = false;
        source = this.gameObject.GetComponent<AudioSource>();
    }

    private void Start()
    {
        if(isLevel){
            MusicChange(levelBGM, bgmVolume);
        } else {
            MusicChange(menuBGM, bgmVolume);
        }
    }

    public void BackgroundMusicToggle(AudioSource source) //give it a reference to a source, and it toggles it
    {
        if (!source.isPlaying)
        {
            source.Play();
        } else {
            source.Pause();
        }
    }

    public void MusicChange(AudioClip clip, float volume) //change the bgm to something else
    {
        source.clip = clip;
        source.volume = volume * baseMultiplier; //add multiplier here for volume adjustment
        source.loop = true;
        source.Play();
    }

    public void ChangeToLevelMusic() //change to level bgm
    {
        MusicChange(levelBGM, bgmVolume);
    }

    public void ChangeToMenuMusic(){
        MusicChange(menuBGM, bgmVolume);
    }

    public void ChangeBGMVolume(float value){
        bgmVolume = value;
        source.volume = bgmVolume * baseMultiplier;
    }
}
