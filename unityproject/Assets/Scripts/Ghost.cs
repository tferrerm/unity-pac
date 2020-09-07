using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ghost : MonoBehaviour, IEntity
{
    public enum GhostState
    {
        Roaming = 0,
        Waiting = 1,
        LeavingBox = 2,
        Consumed = 3
    }
    
    public float waitingDuration;
    private float _waitingTimer;

    public GhostState currentState;
    public EntityId entityId;
    public bool isInBox;
    private Vector2Int consumedBoxTile;

    /*
     * References to other managers 
     */
    public GameManager gameManager;
    public LevelManager levelManager;
    public ModeManager modeManager;
    
    private Animator _animator;
    private bool _reverseDirection = false;

    /*
     * Start is called before the first frame update.
     */
    void Start()
    {
        _animator = GetComponent<Animator>();
        _animator.SetInteger("Direction", (int)currentDirection);
    }

    /*
     * Update is called once per frame
     */
    void Update()
    {
        StateUpdate();
        Move();
    }
    
    /*
     * This function determines whether a mode needs to be changed or not (in that case calls ChangeMode())
     * Ghosts iterate doing the scatter-chase combination. After every chase period, iteration number is
     * incremented.
     */
    private void StateUpdate()
    {
        if (currentState == GhostState.Waiting)
        {
            if (_waitingTimer >= waitingDuration)
            {
                currentState = GhostState.LeavingBox;
                _waitingTimer = 0;
            }
            else
            {
                _waitingTimer += Time.deltaTime;
            }
        } else if (currentState == GhostState.LeavingBox && !isInBox)
        {
            currentState = GhostState.Roaming;
        }
    }

    public void SetFrightenedAnimation()
    {
        _animator.SetBool("FrightenedEnding", false);
        _animator.SetBool("Frightened", true);
    }
    
    public void SetFrightenedEndingAnimation()
    {
        _animator.SetBool("FrightenedEnding", true);
    }

    public void SetStandardAnimation()
    {
        _animator.SetBool("Frightened", false);
        _animator.SetBool("FrightenedEnding", false);
    }

    private void Move()
    {
        if (currentState == GhostState.Waiting) return;

        var speed = (currentState == GhostState.Consumed) ? modeManager.consumedStateSpeed : modeManager.movSpeed;
        Vector3 newPosition = gameManager.GetNewEntityPosition(speed, transform.position, currentDirection);
        if (levelManager.ReachedTargetTile(entityId, newPosition, currentDirection))
        {
            if ((currentState == GhostState.LeavingBox || currentState == GhostState.Consumed) && levelManager.ReachedBoxDoorEntrance(entityId))
            {
                isInBox = (currentState == GhostState.Consumed);
            }

            if (currentState == GhostState.Consumed && levelManager.ReachedTile(entityId, consumedBoxTile))
            {
                _animator.SetBool("Eaten", false);
                currentState = GhostState.LeavingBox;
            }

            levelManager.UpdateTargetTile(entityId, currentDirection);
            var chosenDirection = ChooseNewDirection();
            transform.position = gameManager.GetValidatedPosition(entityId, newPosition, currentDirection, chosenDirection);
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
    private Direction ChooseNewDirection()
    {
        if (_reverseDirection)
        {
            _reverseDirection = false;
            return GetOppositeDirection(currentDirection);
        }
        
        Direction chosenDirection = currentDirection; // Dummy value
        var currentTile = gameManager.GetEntityCurrentTileCoordinates(entityId, currentDirection);
        
        var validDirections = levelManager.GetValidDirectionsForTile(currentTile,
            currentState == GhostState.LeavingBox || currentState == GhostState.Consumed);
        
        if (validDirections.Count == 1)
        {
            return validDirections[0];
        }

        if (currentState == GhostState.LeavingBox)
        {
            chosenDirection = ChooseDirection(currentTile, levelManager.BoxDoorEntranceCoordinates, validDirections);
        }
        else if (currentState == GhostState.Consumed)
        {
            consumedBoxTile = levelManager.GetRandomBoxTileCoordinates();
            chosenDirection = ChooseDirection(currentTile, consumedBoxTile, validDirections);
        }
        else if(currentState == GhostState.Roaming)
        {
            var mode = modeManager.currentMode;
            switch (mode)
            {
                case ModeManager.Mode.Chase:
                    Vector2Int targetTile = ChooseTargetTile();
                    chosenDirection = ChooseDirection(currentTile, targetTile, validDirections);
                    break;
                case ModeManager.Mode.Scatter:
                    chosenDirection = ChooseDirection(currentTile, levelManager.GetOwnCorner(entityId), validDirections);
                    break;
                case ModeManager.Mode.Frightened:
                    chosenDirection = ChooseFrightenedModeDirection(validDirections);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return chosenDirection;
    }

    private Direction ChooseDirection(Vector2Int currentTile, Vector2Int targetTile, List<Direction> validDirections)
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
            var distance = Vector2Int.Distance(targetTile, projectedTile);
            if (distance < leastDistance)
            {
                chosenDirection = direction;
                leastDistance = distance;
            }
        }
        return chosenDirection;
    }
    
    private Direction ChooseFrightenedModeDirection(List<Direction> validDirections)
    {
        var filteredValidDirection = validDirections.FindAll(
            dir => !gameManager.DirectionsAreOpposite(currentDirection, dir));
        var index = Random.Range(0, filteredValidDirection.Count);
        return filteredValidDirection[index];
    }

    private Vector2Int ChooseTargetTile()
    {
        var pacManTile = gameManager.GetEntityCurrentTileCoordinates(EntityId.Player, gameManager.GetPlayerDirection());
        switch (entityId)
        {
            case EntityId.Blinky:
                return ChooseBlinkyTargetTile(pacManTile);
            case EntityId.Pinky:
                return ChoosePinkyTargetTile(pacManTile);
            case EntityId.Inky:
                return ChooseInkyTargetTile(pacManTile);
            case EntityId.Clyde:
                return ChooseClydeTargetTile(pacManTile);
            default:
                return new Vector2Int(0,0);
        }
    }

    private Vector2Int ChooseBlinkyTargetTile(Vector2Int pacManTile)
    {
        return pacManTile;
    }
    
    private Vector2Int ChoosePinkyTargetTile(Vector2Int pacManTile)
    {
        var playerDirection = gameManager.GetPlayerDirection();
        var xTarget = pacManTile.x;
        var yTarget = pacManTile.y;
        switch (playerDirection)
        {
            case Direction.Down:
                yTarget += 4;
                break;
            case Direction.Up:
                yTarget += 4;
                break;
            case Direction.Left:
                xTarget -= 4;
                break;
            case Direction.Right:
                xTarget -= 4;
                break;
        }
        return new Vector2Int(xTarget, yTarget);
    }
    
    private Vector2Int ChooseInkyTargetTile(Vector2Int pacManTile)
    {
        var playerDirection = gameManager.GetPlayerDirection();
        var xPivot = pacManTile.x;
        var yPivot = pacManTile.y;
        switch (playerDirection)
        {
            case Direction.Down:
                yPivot += 2;
                break;
            case Direction.Up:
                yPivot += 2;
                break;
            case Direction.Left:
                xPivot -= 2;
                break;
            case Direction.Right:
                xPivot -= 2;
                break;
        }
        Vector2Int blinkyPosition =  gameManager.GetEntityCurrentTileCoordinates(EntityId.Blinky, gameManager.GetPlayerDirection());
        var xDifference = (xPivot - blinkyPosition.x);
        var yDifference = (yPivot - blinkyPosition.y);
        var xTarget = xPivot + xDifference;
        var yTarget = yPivot + yDifference;
        
        return new Vector2Int(xTarget, yTarget);
    }

    private Vector2Int ChooseClydeTargetTile(Vector2Int pacManTile)
    {
        var distance = Vector2Int.Distance(pacManTile,
            gameManager.GetEntityCurrentTileCoordinates(EntityId.Clyde, gameManager.GetPlayerDirection()));

        if (distance > 8)
        {
            return pacManTile;
        }
        return levelManager.GetOwnCorner(entityId);
    }

    public Direction currentDirection { get; set; }

    private Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return Direction.Down;
            case Direction.Down:
                return Direction.Up;
            case Direction.Left:
                return Direction.Right;
            case Direction.Right:
                return Direction.Left;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    public void Consume()
    {
        _animator.SetBool("Eaten", true);
        _animator.SetBool("Frightened", false);
        _animator.SetBool("FrightenedEnding", false);
        currentState = GhostState.Consumed;
    }

    public void Reverse()
    {
        if(currentState == GhostState.Roaming)
            _reverseDirection = true;
    }
}
