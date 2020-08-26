using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public enum Direction  
{  
    Up = 0, Down = 1, Left = 2, Right = 3,
};

public class Player : MonoBehaviour, IEntity
{
    public float movSpeed = 10f;
    private float _horizontalScreenMarginLimit;
    private float _verticalScreenMarginLimit;
    private Direction? nextDirection;
    private bool hasCollidedWall;
    private Dictionary<KeyCode, Direction> keyDirections = new Dictionary<KeyCode, Direction>();
    private Dictionary<Direction, int> directionRotationAngles = new Dictionary<Direction, int>();
    private Animator animator;

    public GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        // >> 1 divides by 2, 0 is the center
        _horizontalScreenMarginLimit = (Screen.width >> 1);
        _verticalScreenMarginLimit = (Screen.height >> 1);
        
        keyDirections.Add(KeyCode.LeftArrow, Direction.Left);
        keyDirections.Add(KeyCode.RightArrow, Direction.Right);
        keyDirections.Add(KeyCode.UpArrow, Direction.Up);
        keyDirections.Add(KeyCode.DownArrow, Direction.Down);
        
        directionRotationAngles.Add(Direction.Right, 0);
        directionRotationAngles.Add(Direction.Up, 90);
        directionRotationAngles.Add(Direction.Left, 180);
        directionRotationAngles.Add(Direction.Down, 270);
        
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    private void Update()
    {
        MovePlayer();

        foreach (var keyValuePair in keyDirections.Where(keyValuePair => Input.GetKey(keyValuePair.Key)))
        {
            HandleInput(keyValuePair.Value);
        }
    }

    // Check if direction can be changed instantly
    private void HandleInput(Direction inputDirection)
    {
        if (!gameManager.ValidateDirection(EntityId.Player, inputDirection, currentDirection))
        {
            nextDirection = inputDirection;
            hasCollidedWall = false;
        }
    }

    private void MovePlayer()
    {
        if (hasCollidedWall) return;
        
        Vector3 newPosition;
        switch (currentDirection)
        {
            case Direction.Left:
                newPosition = new Vector3(Mathf.Max(transform.position.x - movSpeed * Time.deltaTime, -_horizontalScreenMarginLimit), transform.position.y, 0);
                break;
            case Direction.Right:
                newPosition = new Vector3(Mathf.Min(transform.position.x + movSpeed * Time.deltaTime, _horizontalScreenMarginLimit), transform.position.y, 0);
                break;
            case Direction.Up:
                newPosition = new Vector3(transform.position.x, Mathf.Min(transform.position.y + movSpeed * Time.deltaTime, _verticalScreenMarginLimit), 0);
                break;
            case Direction.Down:
                newPosition = new Vector3(transform.position.x, Mathf.Max(transform.position.y - movSpeed * Time.deltaTime, -_verticalScreenMarginLimit), 0);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        transform.position = gameManager.GetValidMovement(EntityId.Player, newPosition, currentDirection, nextDirection);
        animator.transform.rotation = Quaternion.Euler(new Vector3(0,0, directionRotationAngles[currentDirection]));
    }

    public Direction CurrentDirection
    {
        get => currentDirection;
        set => currentDirection = value;
    }

    public Direction? NextDirection
    {
        get => nextDirection;
        set => nextDirection = value;
    }

    public bool HasCollidedWall
    {
        get => hasCollidedWall;
        set => hasCollidedWall = value;
    }

    public Direction currentDirection { get; set; }
}
