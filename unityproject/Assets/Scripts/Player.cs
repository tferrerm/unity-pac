﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public enum Direction  
{  
    Up = 0, Down = 1, Left = 2, Right = 3,
};

public class Player : MonoBehaviour
{
    public float playerAnimationDelta = 0.075f;
    private float _spriteAnimationAcum;
    private int _spriteVariantIndex = 0;
    public float movSpeed = 10f;
    private float _horizontalScreenMarginLimit;
    private float _verticalScreenMarginLimit;
    private SpriteRenderer _spriteRenderer;
    public Sprite[] upSprites;
    public Sprite[] downSprites;
    public Sprite[] rightSprites;
    public Sprite[] leftSprites;
    private int _spritesPerDirection;
    private int _nextSpriteVariant = -1;
    public Sprite[][] playerSprites;
    public Sprite[] fadingPlayerSprites;
    public Direction currentDirection;
    private Direction? nextDirection;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteAnimationAcum = 0;
        _spritesPerDirection = 3;
        playerSprites = new Sprite[Enum.GetNames(typeof(Direction)).Length][];
        playerSprites[(int) Direction.Up] = upSprites;
        playerSprites[(int) Direction.Right] = rightSprites;
        playerSprites[(int) Direction.Down] = downSprites;
        playerSprites[(int) Direction.Left] = leftSprites;
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
        _spriteAnimationAcum += Time.deltaTime;
        if (_spriteAnimationAcum > playerAnimationDelta)
        {
            if (_spriteVariantIndex % (_spritesPerDirection - 1) == 0) // animate from full to open or open to full
                _nextSpriteVariant *= -1;
            _spriteVariantIndex += _nextSpriteVariant; // next sprite for direction
            _spriteRenderer.sprite = playerSprites[(int) currentDirection][_spriteVariantIndex];
            _spriteAnimationAcum -= playerAnimationDelta;
        }

        MovePlayer();
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            HandleInput(Direction.Left, Direction.Right,
                playerSprites[(int) Direction.Left][_spriteVariantIndex]);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            HandleInput(Direction.Right, Direction.Left,
                playerSprites[(int) Direction.Right][_spriteVariantIndex]);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            HandleInput(Direction.Up, Direction.Down,
                playerSprites[(int) Direction.Up][_spriteVariantIndex]);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            HandleInput(Direction.Down, Direction.Up,
                playerSprites[(int) Direction.Down][_spriteVariantIndex]);
        }
    }

    private void MovePlayer()
    {
        var pos = 0f;
        switch (currentDirection)
        {
            /*case Direction.Left:
                pos = Mathf.Max(transform.position.x - movSpeed * Time.deltaTime, -_horizontalScreenMarginLimit);
                transform.position = new Vector3(pos, transform.position.y, 0);
                break;
            case Direction.Right:
                pos = Mathf.Min(transform.position.x + movSpeed * Time.deltaTime, _horizontalScreenMarginLimit);
                if (pos >= waypointGO.transform.position.x) // REACHED WAYPOINT
                {
                    if (nextDirection != null)
                    {
                        ChangeDirection(Math.Abs(pos - waypointGO.transform.position.x));
                    }
                    else if (waypointGO.GetComponent<Waypoint>().HasRightNeighbor)
                    {
                        waypointGO = waypointGO.GetComponent<Waypoint>().rightNeighbor;
                        transform.position = new Vector3(pos, transform.position.y, 0);
                    }
                }
                else
                {
                    transform.position = new Vector3(pos, transform.position.y, 0);
                }
                break;
            case Direction.Up:
                pos = Mathf.Min(transform.position.y + movSpeed * Time.deltaTime, _verticalScreenMarginLimit);
                if (pos >= waypointGO.transform.position.y) // REACHED WAYPOINT
                {
                    if (nextDirection != null)
                    {
                        ChangeDirection(Math.Abs(pos - waypointGO.transform.position.y));
                    }
                    else if (waypointGO.GetComponent<Waypoint>().HasUpNeighbor)
                    {
                        waypointGO = waypointGO.GetComponent<Waypoint>().upNeighbor;
                        transform.position = new Vector3(transform.position.x, pos, 0);
                    }
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, pos, 0);
                }
                break;
            case Direction.Down:
                pos = Mathf.Max(transform.position.y - movSpeed * Time.deltaTime, -_verticalScreenMarginLimit);
                if (pos <= waypointGO.transform.position.y) // REACHED WAYPOINT
                {
                    if (nextDirection != null)
                    {
                        ChangeDirection(Math.Abs(pos - waypointGO.transform.position.y));
                    }
                    else if (waypointGO.GetComponent<Waypoint>().HasDownNeighbor)
                    {
                        waypointGO = waypointGO.GetComponent<Waypoint>().downNeighbor;
                        transform.position = new Vector3(transform.position.x, pos, 0);
                    }
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, pos, 0);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();*/
        }
    }

    private void HandleInput(Direction inputDirection, Direction oppositeInputDirection, Sprite sprite)
    {
        /*if (currentDirection == inputDirection)
        {
            nextDirection = null;
        }
        else if (currentDirection == oppositeInputDirection)
        {
            currentDirection = inputDirection;
            nextDirection = null;
            var waypoint = waypointGO.GetComponent<Waypoint>();
            switch (inputDirection)
            {
                case Direction.Up:
                    waypointGO = waypoint.upNeighbor;
                    break;
                case Direction.Down:
                    waypointGO = waypoint.downNeighbor;
                    break;
                case Direction.Left:
                    waypointGO = waypoint.leftNeighbor;
                    break;
                case Direction.Right:
                    waypointGO = waypoint.rightNeighbor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _spriteRenderer.sprite = sprite;
        }
        else
        {
            var waypoint = waypointGO.GetComponent<Waypoint>();
            switch (inputDirection)
            {
                case Direction.Up:
                    if(waypoint.HasUpNeighbor)
                        nextDirection = inputDirection;
                    break;
                case Direction.Down:
                    if(waypoint.HasDownNeighbor)
                        nextDirection = inputDirection;
                    break;
                case Direction.Left:
                    if(waypoint.HasLeftNeighbor)
                        nextDirection = inputDirection;
                    break;
                case Direction.Right:
                    if(waypoint.HasRightNeighbor)
                        nextDirection = inputDirection;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }*/
    }

    private void ChangeDirection(float extraDistance)
    {
        /*var waypoint = waypointGO.GetComponent<Waypoint>();
        switch (nextDirection)
        {
            case Direction.Up:
                transform.position = new Vector3(waypointGO.transform.position.x, waypointGO.transform.position.y + extraDistance, 0);
                currentDirection = Direction.Up;
                waypointGO = waypoint.upNeighbor;
                _spriteRenderer.sprite = playerSprites[(int) Direction.Up][_spriteVariantIndex];
                break;
            case Direction.Down:
                transform.position = new Vector3(waypointGO.transform.position.x, waypointGO.transform.position.y - extraDistance, 0);
                currentDirection = Direction.Down;
                waypointGO = waypoint.downNeighbor;
                _spriteRenderer.sprite = playerSprites[(int)Direction.Down][_spriteVariantIndex];
                break;
            case Direction.Left:
                transform.position = new Vector3(waypointGO.transform.position.x - extraDistance, waypointGO.transform.position.y, 0);
                currentDirection = Direction.Left;
                waypointGO = waypoint.leftNeighbor;
                _spriteRenderer.sprite = playerSprites[(int)Direction.Left][_spriteVariantIndex];
                break;
            case Direction.Right:
                transform.position = new Vector3(waypointGO.transform.position.x + extraDistance, waypointGO.transform.position.y, 0);
                currentDirection = Direction.Right;
                waypointGO = waypoint.rightNeighbor;
                _spriteRenderer.sprite = playerSprites[(int)Direction.Right][_spriteVariantIndex];
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        nextDirection = null;*/
    }
}
