using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    private const int POINTS_PER_PELLET = 10;
    private const int POINTS_PER_POWER_PELLET = 50;
    
    private int _score;
    public TMP_Text scoreText;
    
    void Update()
    {
        scoreText.text = _score.ToString();
    }

    public void AddPelletPoints()
    {
        _score += POINTS_PER_PELLET;
    }

    public void AddPowerPelletPoints()
    {
        _score += POINTS_PER_POWER_PELLET;
    }

    public void AddEatenGhostPoints(int eatenGhosts)
    {
        _score += (int)Math.Pow(2, eatenGhosts) * 100;
    }
}
