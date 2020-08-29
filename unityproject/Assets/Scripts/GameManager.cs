using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityId  
{  
    Player = 0,
    Blinky = 1,
    Pinky = 2,
    Inky  = 3,
    Clyde = 4
};

public class GameManager : MonoBehaviour
{
    public LevelManager levelManager;

    public Player player;
    public Ghost blinky;

    private Dictionary<EntityId, IEntity> entityDict;
    
    private static float _horizontalScreenMarginLimit;
    private static float _verticalScreenMarginLimit;
    
    // Start is called before the first frame update
    void Start()
    {
        // >> 1 divides by 2, 0 is the center
        _horizontalScreenMarginLimit = (Screen.width >> 1);
        _verticalScreenMarginLimit = (Screen.height >> 1);
        entityDict = new Dictionary<EntityId, IEntity>();
        entityDict.Add(EntityId.Player, player);
        entityDict.Add(EntityId.Blinky, blinky);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetValidMovement(EntityId entityId, Vector3 position, Direction currentDirection, Direction? nextDirection) // OR RETURN VECTOR3
    {
        return levelManager.GetValidMovement(entityId, position, currentDirection, nextDirection);
    }

    public void SetPlayerDirection(Direction direction)
    {
        player.CurrentDirection = direction;
        player.NextDirection = null;
    }

    public bool ValidateDirection(EntityId entityId, Direction inputDirection, Direction currentDirection)
    {
        if (!levelManager.ValidateOppositeDirection(entityId, inputDirection, currentDirection)) return false;
        
        entityDict[entityId].currentDirection = inputDirection;
        return true;
    }
    
    public void ValidateDirection(EntityId entityId, Direction inputDirection, Direction currentDirection, bool hasCollidedWall)
    {
        if (!levelManager.ValidateOppositeDirection(entityId, inputDirection, currentDirection, hasCollidedWall)) return;
        
        entityDict[entityId].currentDirection = inputDirection;
    }

    public void SetPlayerCollidedWall(bool hasCollided)
    {
        player.HasCollidedWall = hasCollided;
    }

    public void SetPlayerNextDirection(Direction nextDirection)
    {
        player.NextDirection = nextDirection;
    }

    public Vector2Int GetEntityTargetTileCoordinates(EntityId entityId)
    {
        return levelManager.GetEntityTargetTileCoordinates(entityId);
    }

    public static Vector3 GetEntityPosition(float movSpeed,Vector2 position, Direction currentDirection, Direction? nextDirection)
    {
        Vector3 newPosition;
        switch (currentDirection)
        {
            case Direction.Left:
                newPosition = new Vector3(Mathf.Max(position.x - movSpeed * Time.deltaTime, -_horizontalScreenMarginLimit), position.y, 0);
                break;
            case Direction.Right:
                newPosition = new Vector3(Mathf.Min(position.x + movSpeed * Time.deltaTime, _horizontalScreenMarginLimit), position.y, 0);
                break;
            case Direction.Up:
                newPosition = new Vector3(position.x, Mathf.Min(position.y + movSpeed * Time.deltaTime, _verticalScreenMarginLimit), 0);
                break;
            case Direction.Down:
                newPosition = new Vector3(position.x, Mathf.Max(position.y - movSpeed * Time.deltaTime, -_verticalScreenMarginLimit), 0);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return newPosition;
    }
}
