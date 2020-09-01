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
    private Direction? nextDirection;
    private bool hasCollidedWall;
    private readonly Dictionary<KeyCode, Direction> keyDirections = new Dictionary<KeyCode, Direction>();
    private readonly Dictionary<Direction, int> directionRotationAngles = new Dictionary<Direction, int>();
    private Animator _animator;

    public int points = 0; // CHANGE PLACE?
    private const int POINTS_PER_PELLET = 10;
    private const int POINTS_PER_POWER_PELLET = 50;

    public GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        
        
        keyDirections.Add(KeyCode.LeftArrow, Direction.Left);
        keyDirections.Add(KeyCode.RightArrow, Direction.Right);
        keyDirections.Add(KeyCode.UpArrow, Direction.Up);
        keyDirections.Add(KeyCode.DownArrow, Direction.Down);
        
        directionRotationAngles.Add(Direction.Right, 0);
        directionRotationAngles.Add(Direction.Up, 90);
        directionRotationAngles.Add(Direction.Left, 180);
        directionRotationAngles.Add(Direction.Down, 270);
        
        _animator = GetComponent<Animator>();
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
        gameManager.ValidateDirection(EntityId.Player, inputDirection, currentDirection, hasCollidedWall);
    }

    private void MovePlayer()
    {
        if (hasCollidedWall) return;
        
        Vector3 newPosition = GameManager.GetNewEntityPosition(movSpeed, transform.position, currentDirection, nextDirection);
        transform.position = gameManager.GetValidatedPosition(EntityId.Player, newPosition, currentDirection, nextDirection);
        _animator.transform.rotation = Quaternion.Euler(new Vector3(0,0, directionRotationAngles[currentDirection]));
        _animator.speed = 1;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pellet"))
        {
            points += POINTS_PER_PELLET;
        } else if (other.CompareTag("PowerPellet"))
        {
            points += POINTS_PER_POWER_PELLET;
        } else if (other.CompareTag("Ghost"))
        {
            // DIE, DIE, DIE!
        }
        Destroy(other.gameObject);
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

    public void AnimationPlayback()
    {
        if (hasCollidedWall)
            _animator.speed = 0;
    }

    public Direction currentDirection { get; set; }
}
