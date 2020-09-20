using TMPro;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    public int maxLives = 3;

    private int _lives;
    public TMP_Text livesText;

    private void Start()
    {
        _lives = maxLives;
    }

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
    
    // Used when reaching 10k points
    public void OneLifeUp()
    {
        _lives++;
    }

    public int Lives => _lives;
}
