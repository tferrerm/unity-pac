using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HighScoresMenu : MonoBehaviour
{
    private const int MaxHighScoreEntries = 10;

    public Transform highScoresParent;
    public TMP_Text highScorePrefab;
    public TMP_Text highScoreHeader;
    
    private const float HighScoreTopOffset = 10f;
    private const float HighScoreSeparation = 20f;
    
    // Start is called before the first frame update
    void Start()
    {
        List<int> highScores = GetHighScores();

        var currentHighScore = PlayerPrefs.GetInt("currentHighScore");

        Vector3 position = highScorePrefab.transform.position;
        position = new Vector3(position.x, position.y - HighScoreTopOffset, position.z);

        int count = 1;
        foreach (var score in highScores)
        {
            TMP_Text scoreGO = Instantiate(highScorePrefab, position, highScoresParent.rotation);
            scoreGO.transform.SetParent(highScoresParent, false);
            
            if (count == currentHighScore)
            {
                highScoreHeader.gameObject.SetActive(true);
                scoreGO.color = Color.yellow;
                PlayerPrefs.SetInt("currentHighScore", 0);
            }

            scoreGO.text = $"{count} - {score}";
            count++;
            
            position = new Vector3(0, position.y - HighScoreSeparation, 0);
        }
    }

    public List<int> GetHighScores()
    {
        List<int> highScores = new List<int>();
        for (int i = 1; i <= MaxHighScoreEntries; i++)
        {
            var score = PlayerPrefs.GetString($"{i}");
            if (score == "") break; // No more saved scores
            highScores.Add(HighScoreFromString(score));
        }
        
        return highScores;
    }
    
    public int HighScoreFromString(String highScoreStr)
    {
        return Int32.Parse(highScoreStr.Split('-')[0]);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
