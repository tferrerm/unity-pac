using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource playerAudioSource;
    public AudioSource tileMapAudioSource;
    
    public AudioClip disappearingSound;
    public AudioClip wakaWaka;
    public AudioClip eatingGhost;
    public AudioClip eatingFruit;
    
    public AudioClip intro;
    public AudioClip siren;
    public AudioClip frightenedMode;
    public AudioClip consumedGhost;

    public AudioClip outro;

    private void Awake()
    {
        MenuMusicManager.instance.StopMusic();
    }

    public void PlayEatingGhostSound()
    {
        playerAudioSource.PlayOneShot(eatingGhost);
    }

    public void PlayWakaWakaSound()
    {
        playerAudioSource.PlayOneShot(wakaWaka);
    }

    public void PlayDisappearingSound()
    {
        playerAudioSource.PlayOneShot(disappearingSound);
    }
    
    public float GetDisappearingWaitTime()
    {
        return disappearingSound.length;
    }
    
    public void PlaySiren()
    {
        PlayLoop(tileMapAudioSource, siren);
    }

    public void PlayFrightenedMode()
    {
        PlayLoop(tileMapAudioSource, frightenedMode);
    }

    public void PlayConsumedGhost()
    {
        PlayLoop(tileMapAudioSource, consumedGhost);
    }

    public void PlayConsumedFruit()
    {
        playerAudioSource.PlayOneShot(eatingFruit);
    }

    public void PlayIntro()
    {
        tileMapAudioSource.PlayOneShot(intro);
    }
    
    public void PlayOutro()
    {
        tileMapAudioSource.clip = outro;
        tileMapAudioSource.loop = false;
        tileMapAudioSource.Play();
    }

    public float GetIntroWaitTime()
    {
        return intro.length;
    }
    
    public float GetConsumptionWaitTime()
    {
        return eatingGhost.length;
    }

    public void StopTileMapSound()
    {
        tileMapAudioSource.Stop();
    }

    private void PlayLoop(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    public float GetOutroWaitTime()
    {
        return outro.length;
    }
}
