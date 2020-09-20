using UnityEngine;

public class CameraManager : MonoBehaviour
{
    void OnGUI ()
    {
        Debug.Assert(Camera.main != null);
        
        switch (Screen.width)
        {
            case 200:
                Camera.main.orthographicSize = 156;
                break;
            case 480:
                Camera.main.orthographicSize = 120;
                break;
        }
    }
}
