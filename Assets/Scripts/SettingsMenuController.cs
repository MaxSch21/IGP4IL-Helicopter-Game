using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";
    private const string FullscreenKey = "Fullscreen";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const float MutedVolume = -80f;
    private const float UnmutedVolume = 0f;
    private static bool sessionDefaultsApplied;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle fullscreenToggle;

    private void OnEnable()
    {
        ApplySessionDefaultsOnce();
        ApplySavedSettings();
    }

    public static void ApplySessionDefaultsOnce()
    {
        if (sessionDefaultsApplied)
            return;

        PlayerPrefs.SetInt(MusicMutedKey, 0);
        PlayerPrefs.SetInt(SfxMutedKey, 0);
        PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();

        sessionDefaultsApplied = true;
    }

    public void OnMusicToggleChanged(bool _)
    {
        ToggleMusicMuted();
    }

    public void OnSfxToggleChanged(bool _)
    {
        ToggleSfxMuted();
    }

    public void OnFullscreenToggleChanged(bool _)
    {
        ToggleFullscreen();
    }

    public void ToggleMusicMuted()
    {
        bool muted = !GetBool(MusicMutedKey);
        SetMusicMuted(muted);
    }

    public void ToggleSfxMuted()
    {
        bool muted = !GetBool(SfxMutedKey);
        SetSfxMuted(muted);
    }

    public void ToggleFullscreen()
    {
        bool fullscreen = !GetBool(FullscreenKey, Screen.fullScreen);
        SetFullscreen(fullscreen);
    }

    private void ApplySavedSettings()
    {
        bool musicMuted = GetBool(MusicMutedKey);
        bool sfxMuted = GetBool(SfxMutedKey);
        bool fullscreen = GetBool(FullscreenKey, Screen.fullScreen);

        SetMusicMuted(musicMuted);
        SetSfxMuted(sfxMuted);
        SetFullscreen(fullscreen);
    }

    private void SetMusicMuted(bool muted)
    {
        SetBool(MusicMutedKey, muted);
        SetMixerVolume(MusicVolumeParameter, muted);
        SetToggleWithoutNotify(musicToggle, muted);
    }

    private void SetSfxMuted(bool muted)
    {
        SetBool(SfxMutedKey, muted);
        SetMixerVolume(SfxVolumeParameter, muted);
        SetToggleWithoutNotify(sfxToggle, muted);
    }

    private void SetFullscreen(bool fullscreen)
    {
        SetBool(FullscreenKey, fullscreen);
        Screen.fullScreen = fullscreen;
        SetToggleWithoutNotify(fullscreenToggle, fullscreen);
    }

    private void SetMixerVolume(string parameterName, bool muted)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(parameterName, muted ? MutedVolume : UnmutedVolume);
    }

    private void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(value);
    }

    private bool GetBool(string key, bool fallback = false)
    {
        return PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
    }

    private void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
