using TMPro;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public TMP_Text readyText;
    private RectTransform _readyTextTransform;

    void Start()
    {
        _readyTextTransform = readyText.GetComponent<RectTransform>();
    }
    
    void OnGUI ()
    {
        Debug.Assert(Camera.main != null);
        
        switch (Screen.width)
        {
            case 200:
                Camera.main.orthographicSize = 156;
                _readyTextTransform.anchoredPosition = new Vector2(0, -16f);
                break;
            case 480:
                Camera.main.orthographicSize = 120;
                _readyTextTransform.anchoredPosition = new Vector2(0, -21.5f);
                break;
        }
    }
}
