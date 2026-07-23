using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;    // Required to manipulate UI Images, Colors, and Sliders
using UnityEngine.Audio; // Required for controlling Audio Mixers

public class PauseMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pauseMenuUI;   // The main pause menu panel
    public GameObject optionsMenuUI;  // The options panel containing your sliders

    [Header("Audio Settings")]
    public AudioMixer audioMixer;    // Drag your Audio Mixer here
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Brightness Settings")]
    public Slider brightnessSlider;
    public Image brightnessOverlay;  // Drag your full-screen black Image here

    private static bool isPaused = false;

    void Start()
    {
        // Automatically link the sliders to their respective C# functions when the game starts
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(SetBrightness);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // NEW LOGIC: If options are open, Escape should just back out to the main pause screen
            if (optionsMenuUI.activeSelf)
            {
                CloseOptions();
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(false); 
        Time.timeScale = 1f; // Game unfreezes, enemies resume moving
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        optionsMenuUI.SetActive(false); 
        Time.timeScale = 0f; // Game freezes completely! Enemies cannot attack.
        isPaused = true;
    }

    public void OpenOptions()
    {
        Debug.Log("option panel open");
        pauseMenuUI.SetActive(false);  // Hide main pause menu
        optionsMenuUI.SetActive(true);  // Show options panel
    }

    public void CloseOptions()
    {
        optionsMenuUI.SetActive(false); // Hide options panel
        pauseMenuUI.SetActive(true);   // Bring back main pause menu
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f; // CRITICAL: Always reset time before changing scenes!
        isPaused = false;
        SceneManager.LoadScene("TitleScreen");
    }

    // --- Slider Control Methods ---

    public void SetBGMVolume(float value)
    {
        // Converts slider's 0.0001 to 1 value into the AudioMixer's -80dB to 0dB logarithmic scale
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }

    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            // Invert value: high slider value = low image alpha (bright screen)
            Color color = brightnessOverlay.color;
            color.a = 1f - (value * 0.6f + 0.4f);
            brightnessOverlay.color = color;
        }
    }
}