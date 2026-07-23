using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;    // Required to manipulate UI Images, Colors, and Sliders
using UnityEngine.Audio; // Required for controlling Audio Mixers

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuUI;    // The main title screen panel
    public GameObject optionsMenuUI;  // The options panel with your sliders

    [Header("Audio Settings")]
    public AudioMixer audioMixer;    // Drag your Audio Mixer here
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Brightness Settings")]
    public Slider brightnessSlider;
    public Image brightnessOverlay;  // Drag your full-screen black Image here

    void Start()
    {
        // Ensure the main menu is visible and options are hidden when the game starts
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);

        // Automatically link the sliders to their respective C# functions
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(SetBrightness);
    }

    // Call this when clicking the "Start Game" or "Play" button
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    // Call this when clicking the "Options" button
    public void OpenOptions()
    {
        mainMenuUI.SetActive(false);  // Hide main title screen
        optionsMenuUI.SetActive(true);  // Show options panel
    }

    // Call this when clicking the "Back" button inside the Options Menu
    public void CloseOptions()
    {
        optionsMenuUI.SetActive(false); // Hide options panel
        mainMenuUI.SetActive(true);   // Bring back main title screen
    }

    // Call this when clicking the "Quit" button
    public void QuitGame()
    {
        Debug.Log("Game is shutting down..."); // Confirms it works inside the Unity Editor
        Application.Quit(); // Closes the actual built application
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