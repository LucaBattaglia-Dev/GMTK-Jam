using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class UniversalUIManager : MonoBehaviour
{
    //Pause Configurations: If you can pause the scene or not using escape key//
    //-----------------------------------------------------------------------------------------------//
    [Header("Pause Settings")]
    [Tooltip("Check this ONLY if the scene is allowed to be paused (e.g., gameplay scenes).")]
    [SerializeField] private bool canThisSceneBePaused = false;
    
    //-----------------------------------------------------------------------------------------------//
    //Scene Settings: Where Start & MainMenu Buttons direct you//
    [Header("Scene Settings")]
    [Tooltip("The name of your Title Screen scene. Fallback is 'TitleScreen'.")]
    [SerializeField] private string mainMenuSceneName = "TitleScreen";
    [Tooltip("The name of your gameplay level scene. Fallback is 'Level1'.")]
    [SerializeField] private string gameplaySceneName = "Level1";

    //-----------------------------------------------------------------------------------------------//
    //Audio Configurations: Names of sound sources & Volume//
    [Header("Audio Configurations")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameterName = "MasterVolume";

    //-----------------------------------------------------------------------------------------------//
    //Variables & Objects List:
    private GameObject titleScreenPanel;
    private GameObject pauseMenuPanel;
    private GameObject optionsMenuPanel;
    private Slider masterVolumeSlider;
    private Slider brightnessSlider;
    private Image brightnessOverlay;

    private bool isPaused = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        AutoAssignReferences();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AutoAssignReferences();
        InitializeUI();
    }

    private void Start()
    {
        InitializeUI();
        LoadSettings();
    }

    private void Update()
    {
        if (canThisSceneBePaused && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private void AutoAssignReferences()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTransform = canvas.transform;

        // --- Find the Live Panel GameObjects ---
        Transform mainMenuTransform = canvasTransform.Find("MainMenuPanel");
        if (mainMenuTransform != null) titleScreenPanel = mainMenuTransform.gameObject;

        Transform pauseMenuTransform = canvasTransform.Find("PauseMenuPanel");
        if (pauseMenuTransform != null) pauseMenuPanel = pauseMenuTransform.gameObject;

        Transform optionsMenuTransform = canvasTransform.Find("OptionsMenuPanel");
        if (optionsMenuTransform != null) optionsMenuPanel = optionsMenuTransform.gameObject;

        Transform overlayTransform = canvasTransform.Find("BrightnessOverlay");
        if (overlayTransform != null) brightnessOverlay = overlayTransform.GetComponent<Image>();

        // --- Find Sliders inside Options Panel ---
        if (optionsMenuPanel != null)
        {
            Transform volumeSliderTransform = optionsMenuPanel.transform.Find("OptionsPanel/VolumePanel/Slider");
            if (volumeSliderTransform != null) masterVolumeSlider = volumeSliderTransform.GetComponent<Slider>();

            Transform brightnessSliderTransform = optionsMenuPanel.transform.Find("OptionsPanel/BrightnessPanel/Slider");
            if (brightnessSliderTransform != null) brightnessSlider = brightnessSliderTransform.GetComponent<Slider>();
        }

        // --- Fallback Safe Strings ---
        string finalTitleScene = string.IsNullOrWhiteSpace(mainMenuSceneName) ? "TitleScreen" : mainMenuSceneName;
        string finalGameplayScene = string.IsNullOrWhiteSpace(gameplaySceneName) ? "Level1" : gameplaySceneName;

        // ==========================================
        // DYNAMIC BUTTON BINDING (OVERRIDING PREFABS)
        // ==========================================

        // --- 1. Pause Menu Panel Buttons ---
        if (pauseMenuPanel != null)
        {
            SetupButton(pauseMenuPanel.transform.Find("ResumeButton"), ResumeGame);
            SetupButton(pauseMenuPanel.transform.Find("OptionsButton"), OpenPauseOptions);
            SetupButton(pauseMenuPanel.transform.Find("TitleButton"), () => GoToMainMenu(finalTitleScene)); 
        }

        // --- 2. Title Screen Menu Panel Buttons ---
        if (titleScreenPanel != null)
        {
            SetupButton(titleScreenPanel.transform.Find("StartButton"), () => StartGame(finalGameplayScene)); 
            SetupButton(titleScreenPanel.transform.Find("OptionsButton"), OpenTitleOptions);
            SetupButton(titleScreenPanel.transform.Find("ExitButton"), QuitGame);
        }

        // --- 3. Options Menu Navigation ---
        if (optionsMenuPanel != null)
        {
            SetupButton(optionsMenuPanel.transform.Find("BackButton"), HandleUniversalBackAction);
            SetupButton(optionsMenuPanel.transform.Find("OptionsPanel/BackButton"), HandleUniversalBackAction);
        }
    }

    private void SetupButton(Transform buttonTransform, UnityEngine.Events.UnityAction action)
    {
        if (buttonTransform != null)
        {
            Button btn = buttonTransform.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }
    }

    private void HandleUniversalBackAction()
    {
        if (canThisSceneBePaused)
        {
            BackPauseButton();
        }
        else
        {
            BackTitleButton();
        }
    }

    private void InitializeUI()
    {
        if (canThisSceneBePaused)
        {
            if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
            Time.timeScale = 1f; 
        }
        else
        {
            if (titleScreenPanel != null) titleScreenPanel.SetActive(true);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        }
    }

    private void LoadSettings()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.value = savedVol;
            SetMasterVolume(savedVol);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0f);
            brightnessSlider.value = savedBrightness;
            SetBrightness(savedBrightness);
        }
    }

    #region Sliders & Audio

    public void SetMasterVolume(float value)
    {
        if (audioMixer == null) return;
        float dB = value > 0 ? Mathf.Log10(value) * 20f : -80f;
        audioMixer.SetFloat(masterVolumeParameterName, dB);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetBrightness(float value)
    {
        if (brightnessOverlay == null) return;
        Color color = brightnessOverlay.color;
        color.a = 1f - value; 
        brightnessOverlay.color = color;
        PlayerPrefs.SetFloat("Brightness", value);
    }

    #endregion

    #region Navigation / Button Controls

    public void BackPauseButton()
    {
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void BackTitleButton()
    {
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (titleScreenPanel != null) titleScreenPanel.SetActive(true);
    }

    public void StartGame(string targetScene)
    {
        if (DoesSceneExist(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning($"UniversalUI Warning: Scene '{targetScene}' was not found in Build Settings! Loading fallback: 'Level1'");
            SceneManager.LoadScene("Level1");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting Game...");
    }

    public void PauseGame()
    {
        if (!canThisSceneBePaused) return;
        isPaused = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenPauseOptions()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(true);
    }
    
    public void OpenTitleOptions()
    {
        if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(true);
    }

    public void GoToMainMenu(string targetScene)
    {
        Time.timeScale = 1f; 
        if (DoesSceneExist(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning($"UniversalUI Warning: Scene '{targetScene}' was not found in Build Settings! Loading fallback: 'TitleScreen'");
            SceneManager.LoadScene("TitleScreen");
        }
    }

    // Helper function that scans your project build settings to verify if a scene is real
    private bool DoesSceneExist(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string parsedName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (parsedName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}