using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movSpeed = 200f;
    void Awake()
    {
        Debug.Log("Awake");
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            var xpos = transform.position.x - movSpeed * Time.deltaTime;
            transform.position = new Vector3(xpos, transform.position.y, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            var xpos = transform.position.x + movSpeed * Time.deltaTime;
            transform.position = new Vector3(xpos, transform.position.y, 0);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            var ypos = transform.position.y + movSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, ypos, 0);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            var ypos = transform.position.y - movSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, ypos, 0);
        }
    }
}
