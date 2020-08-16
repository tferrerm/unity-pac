using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private const int DirectionUp = 0;
    private const int DirectionRight = 1;
    private const int DirectionDown = 2;
    private const int DirectionLeft = 3;
    public float movSpeed = 10f;
    private float _horizontalScreenMarginLimit;
    private float _verticalScreenMarginLimit;
    private SpriteRenderer _spriteRenderer;
    public Sprite[] playerSprites;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    // Start is called before the first frame update
    private void Start()
    {
        // >> 1 divides by 2, 0 is the center
        _horizontalScreenMarginLimit = (Screen.width >> 1);
        _verticalScreenMarginLimit = (Screen.height >> 1);
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            var xpos = Mathf.Max(transform.position.x - movSpeed * Time.deltaTime, -_horizontalScreenMarginLimit);
            transform.position = new Vector3(xpos, transform.position.y, 0);
            _spriteRenderer.sprite = playerSprites[DirectionLeft];
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            var xpos = Mathf.Min(transform.position.x + movSpeed * Time.deltaTime, _horizontalScreenMarginLimit);
            transform.position = new Vector3(xpos, transform.position.y, 0);
            _spriteRenderer.sprite = playerSprites[DirectionRight];

        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            var ypos = Mathf.Min(transform.position.y + movSpeed * Time.deltaTime, _verticalScreenMarginLimit);
            transform.position = new Vector3(transform.position.x, ypos, 0);
            _spriteRenderer.sprite = playerSprites[DirectionUp];

        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            var ypos = Mathf.Max(transform.position.y - movSpeed * Time.deltaTime, -_verticalScreenMarginLimit);
            transform.position = new Vector3(transform.position.x, ypos, 0);
            _spriteRenderer.sprite = playerSprites[DirectionDown];

        }
    }
}
