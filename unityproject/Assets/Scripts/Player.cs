using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movSpeed = 10f;

    private float horizontalScreenMarginLimit;
    private float verticalScreenMarginLimit;
    
    void Awake()
    {
        Debug.Log("Awake");
    }
    // Start is called before the first frame update
    void Start()
    {
        // >> 1 divides by 2, 0 is the center
        horizontalScreenMarginLimit = (Screen.width >> 1);
        verticalScreenMarginLimit = (Screen.height >> 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            var xpos = Mathf.Max(transform.position.x - movSpeed * Time.deltaTime, -horizontalScreenMarginLimit);
            transform.position = new Vector3(xpos, transform.position.y, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            var xpos = Mathf.Min(transform.position.x + movSpeed * Time.deltaTime, horizontalScreenMarginLimit);
            transform.position = new Vector3(xpos, transform.position.y, 0);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            var ypos = Mathf.Min(transform.position.y + movSpeed * Time.deltaTime, verticalScreenMarginLimit);
            transform.position = new Vector3(transform.position.x, ypos, 0);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            var ypos = Mathf.Max(transform.position.y - movSpeed * Time.deltaTime, -verticalScreenMarginLimit);
            transform.position = new Vector3(transform.position.x, ypos, 0);
        }
    }
}
