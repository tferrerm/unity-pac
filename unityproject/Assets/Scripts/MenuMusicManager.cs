using System;
using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioClip clip;
    public static MenuMusicManager instance = null;
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy (gameObject);

        instance.PlayMusic();
        DontDestroyOnLoad (gameObject);
    }
    
    public void PlayMusic()
    {
        if (musicSource.isPlaying) return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
