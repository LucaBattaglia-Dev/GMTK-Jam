using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TimeWarp : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float newTimeScale;
    [SerializeField] private AudioClip timeWarpLoopSFX;
    [SerializeField] private float timeWarpVolume = 1f;

    private AudioSource warpAudioSource;

    private void Awake()
    {
        warpAudioSource = GetComponent<AudioSource>();
        warpAudioSource.clip = timeWarpLoopSFX;
        warpAudioSource.loop = true;
        warpAudioSource.volume = timeWarpVolume;
        warpAudioSource.pitch = 2f; // Play SFX at 2x speed
    }
    
    public override void OnPickup(){
        LevelUIManager.Instance.InstantiateCooldownUI(this);
        TruckDriver.GlobalSpeedMultiplier = 0.5f; // 2x slow down

        // Turn on black and white overlay
        LevelUIManager.Instance.Player.GetChild(0).gameObject.SetActive(true);

        // Play looping time warp sound effect at 2x pitch
        if (warpAudioSource != null && timeWarpLoopSFX != null)
        {
            warpAudioSource.pitch = 2f;
            warpAudioSource.Play();
        }
    }

    public override void RestartTimer()
    {
        base.RestartTimer();

        // Restart sound effect playback from the beginning when picking up another time warp
        if (warpAudioSource != null && timeWarpLoopSFX != null)
        {
            warpAudioSource.Stop();
            warpAudioSource.Play();
        }
    }

    public override void OnDespawn(){
        TruckDriver.GlobalSpeedMultiplier = 1f;

        // Turn off black and white overlay
        LevelUIManager.Instance.Player.GetChild(0).gameObject.SetActive(false);

        // Stop sound effect
        if (warpAudioSource != null)
        {
            warpAudioSource.Stop();
        }
    }
}