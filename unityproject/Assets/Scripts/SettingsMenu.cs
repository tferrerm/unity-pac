using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TMP_Dropdown resolutionDropdown;
    public Resolution[] resolutions = new Resolution[2];
    private int _height1 = 640;
    private int _width1 = 480;
    private int _height2 = 320;
    private int _width2 = 200;

    public void Start()
    {
        resolutionDropdown.ClearOptions();

        resolutions[0] = new Resolution {height = _height1, width = _width1};
        resolutions[1] = new Resolution {height = _height2, width = _width2};
        Resolution currentResolution = new Resolution {height = Screen.height, width = Screen.width};
        int currentResolutionIndex = 0;

        if (equalsResolution(resolutions[1], currentResolution))
            currentResolutionIndex = 1;
        
        resolutionDropdown.AddOptions(new List<string>(new String[]
        {
            $"{_width1}x{_height1}", $"{_width2}x{_height2}"
        }));
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // normally ranges between -80 and 0 dB, but this calculation makes the volume linear
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", Mathf.Log10(volume) * 20);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, false);
    }

    private bool equalsResolution(Resolution resolution, Resolution other)
    {
        return resolution.width == other.width && resolution.height == other.height;
    }
}
