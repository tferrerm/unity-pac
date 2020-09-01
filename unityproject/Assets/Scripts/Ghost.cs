using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

public class Ghost : MonoBehaviour, IEntity
{
    public float movSpeed = 3.9f;
    public Node startingPosition;
    
    /*
     * Each timer belongs to a different iteration. We have four iterations, thereby four pairs of timers
     */
    public int scatterModeTimer1 = 7;
    public int chaseModeTimer1 = 20;
    
    public int scatterModeTimer2 = 7;
    public int chaseModeTimer2 = 20;
    
    public int scatterModeTimer3 = 7;
    public int chaseModeTimer3 = 20;
    
    public int scatterModeTimer4 = 7;

    public int modeChangeIteration = 1;
    public float modeChangeTimer = 0;

    public enum Mode
    {
        Chase = 0,
        Scatter = 1,
        Frightened = 2,
    }

    public Mode _currentMode = Mode.Scatter;
    private Mode _previousMode;

    public GameManager gameManager;
    public LevelManager levelManager;
    public Vector2Int moveToTile;
    private Animator _animator;

    private readonly Random _random = new Random();
    
    /*
     * Start is called before the first frame update.
     * If this is not working let's make sure that Ghost script is after Pac Man script in execution order
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
        if (_currentMode == Mode.Frightened) return;
        modeChangeTimer += Time.deltaTime;
        switch (modeChangeIteration)
        {
            case 1:
            {
                /*
                 * Checking if it's time to switch from scatter to chase
                 */
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer1)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                /*
                 * Checking if it's time to switch from chase to scatter
                 */
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer1)
                {
                    modeChangeIteration = 2;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 2:
            {
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer2)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer2)
                {
                    modeChangeIteration = 3;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 3:
            {
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer3)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer3)
                {
                    modeChangeIteration = 4;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 4:
            {
                /*
                 * If we're in chase mode in the last iteration we're  in chase mode forever
                 */
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer4)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }

                break;
            }
        }
    }

    /**
     * Changes mode
     */
    private void ChangeMode(Mode m)
    {
        _currentMode = m;
    }

    private void Move()
    {
        Vector3 newPosition = GameManager.GetNewEntityPosition(movSpeed, transform.position, currentDirection, null);
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
        
        switch (_currentMode)
        {
            case Mode.Chase:
                chosenDirection = ChooseChaseModeDirection(currentTile, pacManTile, validDirections);
                break;
            case Mode.Scatter:
                chosenDirection = ChooseScatterModeDirection(currentTile, pacManTile, validDirections);
                break;
            case Mode.Frightened:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        

        return chosenDirection;
    }

    private Direction ChooseChaseModeDirection(Vector2Int currentTile, Vector2Int pacManTile, List<Direction> validDirections)
    {
        Direction chosenDirection = currentDirection; // Dummy value
        var leastDistance = 100000f;
        
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
            var distance = GetDistance(pacManTile, projectedTile);
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
            dir => currentDirection == dir || !gameManager.DirectionsAreOpposite(currentDirection, dir));
        var index = _random.Next(0, filteredValidDirection.Count - 1);
        return filteredValidDirection[index];
    }

    private static float GetDistance(Vector2Int posA, Vector2Int posB)
    {
        var dx = posA.x - posB.x;
        var dy = posA.y - posB.y;

        var distance = Mathf.Sqrt(dx * dx + dy * dy);
        return distance;
    }
    
    public Direction currentDirection { get; set; }

}
