using System;
using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    private const int PointsPerPellet = 10;
    private const int PointsPerPowerPellet = 50;
    
    private int _score;
    public TMP_Text scoreText;
    
    void Update()
    {
        scoreText.text = _score.ToString();
    }

    public void AddPelletPoints()
    {
        _score += PointsPerPellet;
    }

    public void AddPowerPelletPoints()
    {
        _score += PointsPerPowerPellet;
    }

    public void AddEatenGhostPoints(int eatenGhosts)
    {
        _score += (int)Math.Pow(2, eatenGhosts) * 100;
    }
}
