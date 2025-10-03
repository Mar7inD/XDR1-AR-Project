using UnityEngine;

public class IOSAudioManager : MonoBehaviour
{
    void Start()
    {
        #if UNITY_IOS && !UNITY_EDITOR
        // Ensure audio plays even when device is on silent
        AudioSettings.Mobile.muteOtherAudioSources = false;
        AudioSettings.Mobile.stopAudioOutputOnMute = false;
        audioSource.ignoreListenerPause = true;
        #endif
    }
}