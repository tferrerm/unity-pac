using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    void OnGUI ()
    {
        switch (Screen.width)
        {
            case 200:
                Camera.main.orthographicSize = 144;
                break;
            case 480:
                Camera.main.orthographicSize = 120;
                break;
        }
    }
}
