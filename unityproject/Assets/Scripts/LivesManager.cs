using TMPro;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    public const int MaxLives = 3;

    private int _lives = MaxLives;
    public TMP_Text livesText;

    void Update()
    {
        livesText.text = _lives.ToString();
    }

    public int DecrementLives()
    {
        if (_lives == 0) return 0;
        _lives--;
        return _lives;
    }
    
    // to be used when reaching 10k points
    public void OneLifeUp()
    {
        _lives++;
    }

    public int Lives => _lives;
}
