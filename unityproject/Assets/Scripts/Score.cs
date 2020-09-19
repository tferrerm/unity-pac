using System;
using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    private const int PointsPerPellet = 10;
    private const int PointsPerPowerPellet = 50;
    private const int PointsPerBonusFruit = 200;
    private const int MaxHighScoreEntries = 5;
    private const String highScoreSeparator = "-";
    
    private int _score;
    private bool _addedExtraLife;
    private const int ExtraLifeScore = 10000;
    public TMP_Text highScoreText;
    public TMP_Text scoreText;

    private LivesManager livesManager;

    private void Awake()
    {
        highScoreText.text = GetHighestScore().ToString();
    }

    void Update()
    {
        scoreText.text = _score.ToString();
    }

    public void AddPelletPoints()
    {
        _score += PointsPerPellet;
        
        if (!_addedExtraLife && _score >= ExtraLifeScore)
        {
            _addedExtraLife = true;
            livesManager.OneLifeUp();
        }
    }

    public void AddPowerPelletPoints()
    {
        _score += PointsPerPowerPellet;
        
        if (!_addedExtraLife && _score >= ExtraLifeScore)
        {
            _addedExtraLife = true;
            livesManager.OneLifeUp();
        }
    }

    public void AddEatenGhostPoints(int eatenGhosts)
    {
        _score += (int)Math.Pow(2, eatenGhosts) * 100;
    }

    public void AddFruitBonusPoints()
    {
        _score += PointsPerBonusFruit;
    }

    public int GetHighestScore()
    {
        return HighScoreFromString(PlayerPrefs.GetString("1", "0"));
    }

    public void SaveScore(String playerName)
    {
        String defaultScore = "0";
        int i;
        for (i = 1; i <= MaxHighScoreEntries; i++)
        {
            String highScore = PlayerPrefs.GetString($"{i}", defaultScore);

            if (_score > HighScoreFromString(highScore))
            {
                ShiftScores(i);
                String newScore = $"{_score}{highScoreSeparator}{playerName}";
                PlayerPrefs.SetString($"{i}", newScore);
                // Debug.Log($"Put high score {i} {newScore}");
                PlayerPrefs.Save();
                return;
            }
        }
    }

    public int HighScoreFromString(String highScoreStr)
    {
        return Int32.Parse(highScoreStr.Split('-')[0]);
    }

    private void ShiftScores(int index)
    {
        int i;
        for (i = MaxHighScoreEntries - 1; i >= index; i--)
        {
            String highScore = PlayerPrefs.GetString($"{i}", "");
            // Debug.Log($"Shifting score {i} to {i+1} {highScore}");
            if (highScore.Length == 0)
                continue;
            PlayerPrefs.SetString($"{i+1}", highScore);
        }
    }

    public LivesManager LivesManager
    {
        set => livesManager = value;
    }
}
