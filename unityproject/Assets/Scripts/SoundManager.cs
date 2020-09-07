﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource playerAudioSource;
    public AudioSource tileMapAudioSource;
    
    public AudioClip disappearingSound;
    public AudioClip wakaWaka;
    public AudioClip eatingGhost;
    
    public AudioClip intro;
    public AudioClip siren;
    public AudioClip frightenedMode;
    public AudioClip consumedGhost;
    
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

    public void PlayIntro()
    {
        tileMapAudioSource.PlayOneShot(intro);
    }

    public float GetIntroWaitTime()
    {
        return intro.length;
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
}