using UnityEngine;

public class TimeWarp : Pickup
{
    [Header("Unique Parameters")]
    [SerializeField] private float newTimeScale;
    [SerializeField] private AudioClip timeWarpLoopSFX;

    private AudioSource warpAudioSource;

    public override void OnPickup(){
        LevelUIManager.Instance.InstantiateCooldownUI(this);
        TruckDriver.GlobalSpeedMultiplier = 0.5f; // 2x slow down

        // Turn on black and white overlay
        LevelUIManager.Instance.Player.GetChild(0).gameObject.SetActive(true);

        warpAudioSource = SFXManager.Instance.PlaySFX(timeWarpLoopSFX, SFXVolume, false);
        warpAudioSource.pitch = 2f;
        warpAudioSource.loop = true;
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