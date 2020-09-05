using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class Ghost : MonoBehaviour, IEntity
{
    public enum Mode
    {
        Chase = 0,
        Scatter = 1,
        Frightened = 2,
        Consumed = 3,
    }
    
    /*
     * Iteration phases
     */
    public int scatterModeTimer1 = 7;
    public int chaseModeTimer1 = 20;
    public int scatterModeTimer2 = 7;
    public int chaseModeTimer2 = 20;
    public int scatterModeTimer3 = 7;
    public int chaseModeTimer3 = 20;
    public int scatterModeTimer4 = 7;
    public int modeChangeIteration = 1;
    
    /*
     * Timers
     */
    private float _modeChangeTimer;
    private float _frightenedModeTimer;
    private float _blinkTimer;
    private bool _frightenedModeIsWhite;

    /*
     * Durations
     */
    public float frightenedModeDuration = 10;
    public float startBlinkingAt = 7;
    public float blinkFrequency = 0.1f;
    
    /*
     * Speeds
     */
    public float movSpeed = 3.9f;
    public float frightenedModeSpeed = 3.9f;
    private float _previousSpeed;

    /*
     * Modes
     */
    public Mode currentMode = Mode.Scatter;
    private Mode _previousMode = Mode.Scatter;

    /*
     * References to other managers 
     */
    public GameManager gameManager;
    public LevelManager levelManager;
    
    private Animator _animator;
    
    /*
     * Start is called before the first frame update.
     */
    void Start()
    {
        currentDirection = Direction.Right;
        _animator = GetComponent<Animator>();
    }

    /*
     * Update is called once per frame
     */
    void Update()
    {
        ModeUpdate();
        Move();
    }
    
    /*
     * This function determines whether a mode needs to be changed or not (in that case calls ChangeMode())
     * Ghosts iterate doing the scatter-chase combination. After every chase period, iteration number is
     * incremented.
     */
    private void ModeUpdate()
    {
        _modeChangeTimer += Time.deltaTime;
        if (currentMode != Mode.Frightened)
        {
            switch (modeChangeIteration)
            {
                case 1:
                {
                    /*
                     * Checking if it's time to switch from scatter to chase
                     */
                    if (currentMode == Mode.Scatter && _modeChangeTimer > scatterModeTimer1)
                    {
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }
                    /*
                     * Checking if it's time to switch from chase to scatter
                     */
                    if (currentMode == Mode.Chase && _modeChangeTimer > chaseModeTimer1)
                    {
                        modeChangeIteration = 2;
                        ChangeMode(Mode.Scatter);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
                case 2:
                {
                    if (currentMode == Mode.Scatter && _modeChangeTimer > scatterModeTimer2)
                    {
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }
                    if (currentMode == Mode.Chase && _modeChangeTimer > chaseModeTimer2)
                    {
                        modeChangeIteration = 3;
                        ChangeMode(Mode.Scatter);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
                case 3:
                {
                    if (currentMode == Mode.Scatter && _modeChangeTimer > scatterModeTimer3)
                    {
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }
                    if (currentMode == Mode.Chase && _modeChangeTimer > chaseModeTimer3)
                    {
                        modeChangeIteration = 4;
                        ChangeMode(Mode.Scatter);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
                case 4:
                {
                    /*
                     * If we're in chase mode in the last iteration we're  in chase mode forever
                     */
                    if (currentMode == Mode.Scatter && _modeChangeTimer > scatterModeTimer4)
                    {
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
            }
        }
        /*
         * Handling frightened mode. If less than 3 seconds are left, ghosts start blinking (switching between blue and white)
         */
        else if (currentMode == Mode.Frightened)
        {
            _frightenedModeTimer += Time.deltaTime;

            if (_frightenedModeTimer > frightenedModeDuration)
            {
                _frightenedModeTimer = 0;
                ChangeMode(_previousMode);
            }

            if (_frightenedModeTimer > startBlinkingAt)
            {
                _blinkTimer += Time.deltaTime;

                if (_blinkTimer >= blinkFrequency)
                {
                    _blinkTimer = 0f;
                    if (_frightenedModeIsWhite)
                    {
                        //@paton: animar al fantasmita para que pase de blanco a azul
                        _frightenedModeIsWhite = false;
                    }
                    else
                    {
                        //@paton: animar al fantasmita para que pase de azul a blanco
                        _frightenedModeIsWhite = true;
                    }
                }
            }
        }
        
    }

    public void SetFrightenedMode()
    {
        _frightenedModeTimer = 0;
        ChangeMode(Mode.Frightened);
    }
    
    private void ChangeMode(Mode m)
    {
        if (currentMode == Mode.Frightened)
        {
            movSpeed = _previousSpeed;
        }

        if (m == Mode.Frightened)
        {
            _previousSpeed = movSpeed;
            movSpeed = frightenedModeSpeed;
        }

        if (m != _previousMode)
        {
            _previousMode = currentMode;
            currentMode = m;
        }
        
    }

    private void Move()
    {
        Vector3 newPosition = gameManager.GetNewEntityPosition(movSpeed, transform.position, currentDirection);
        if (levelManager.ReachedTargetTile(EntityId.Blinky, newPosition, currentDirection))
        {
            levelManager.UpdateTargetTile(EntityId.Blinky, currentDirection);
            var pacManTile = gameManager.GetEntityCurrentTileCoordinates(EntityId.Player, gameManager.GetPlayerDirection());
            var currentTile = gameManager.GetEntityCurrentTileCoordinates(EntityId.Blinky, currentDirection);
            var validDirections = levelManager.GetValidDirectionsForTile(currentTile);
            var chosenDirection = ChooseNewDirection(currentTile, pacManTile, validDirections);
            transform.position = gameManager.GetValidatedPosition(EntityId.Blinky, newPosition, currentDirection, chosenDirection);
            currentDirection = chosenDirection;
            _animator.SetInteger("Direction", (int)currentDirection);
        }
        else
        {
            transform.position = newPosition;
        }
    }

    /*
     * Iterating through the nodes to see which is closer to targetTile (Pac-man)
     */
    private Direction ChooseNewDirection(Vector2Int currentTile, Vector2Int pacManTile, List<Direction> validDirections)
    {
        Direction chosenDirection = currentDirection; // Dummy value
        
        switch (currentMode)
        {
            case Mode.Chase:
                chosenDirection = ChooseChaseModeDirection(currentTile, pacManTile, validDirections);
                break;
            case Mode.Scatter:
                chosenDirection = ChooseScatterModeDirection(currentTile, pacManTile, validDirections);
                break;
            case Mode.Frightened:
                var randomIndex = Random.Range(0, validDirections.Count);
                chosenDirection = validDirections[randomIndex];
                break;
            case Mode.Consumed:
                chosenDirection = ChooseConsumedModeDirection(currentTile, validDirections);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        

        return chosenDirection;
    }

    private Direction ChooseChaseModeDirection(Vector2Int currentTile, Vector2Int pacManTile, List<Direction> validDirections)
    {
        Direction chosenDirection = currentDirection; // Dummy value
        var leastDistance = float.MaxValue;
        
        foreach (var direction in validDirections)
        {
            if(gameManager.DirectionsAreOpposite(currentDirection, direction))
                continue;
            
            var xCoord = currentTile.x;
            var yCoord = currentTile.y;
            switch (direction)
            {
                case Direction.Down:
                    yCoord += 1;
                    break;
                case Direction.Up:
                    yCoord -= 1;
                    break;
                case Direction.Right:
                    xCoord += 1;
                    break;
                case Direction.Left:
                    xCoord -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Vector2Int projectedTile = new Vector2Int(xCoord, yCoord);
            var distance = Vector2Int.Distance(pacManTile, projectedTile);
            if (distance < leastDistance)
            {
                chosenDirection = direction;
                leastDistance = distance;
            }
        }

        return chosenDirection;
    }
    
    private Direction ChooseScatterModeDirection(Vector2Int currentTile, Vector2Int pacManTile, List<Direction> validDirections)
    {
        var filteredValidDirection = validDirections.FindAll(
            dir => !gameManager.DirectionsAreOpposite(currentDirection, dir));
        var index = Random.Range(0, filteredValidDirection.Count);
        return filteredValidDirection[index];
    }
    
    private Direction ChooseConsumedModeDirection(Vector2Int currentTile, List<Direction> validDirections)
    {
        var filteredValidDirection = validDirections.FindAll(
            dir => !gameManager.DirectionsAreOpposite(currentDirection, dir));
        var index = Random.Range(0, filteredValidDirection.Count);
        return filteredValidDirection[index];
    }

    public Direction currentDirection { get; set; }

}
