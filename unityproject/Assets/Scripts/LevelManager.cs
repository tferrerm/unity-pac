using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelManager : MonoBehaviour
{
    public LevelParser levelParser;
    public GameManager gameManager;
    
    /*
     * Tilemap
     */
    private MapComponent[][] tileMap;
    private int rows;
    private int cols;
    
    public GameObject pelletPool;
    private List<Transform> pelletTransforms = new List<Transform>();
    private List<Vector2Int> boxTiles = new List<Vector2Int>();
    private Vector2Int boxDoorEntranceCoordinates;
    private float tileMapHalfWidth;
    
    /*
     * Entities
     */
    public Player player;
    public Ghost[] ghosts = new Ghost[4];
    private readonly EntityId[] _ghostOrder = {EntityId.Blinky, EntityId.Inky, EntityId.Pinky, EntityId.Clyde};
    private Dictionary<EntityId, Vector2Int> entitiesTargetTileCoordinates;
    
    private bool finishedGame;

    void Awake()
    {
        tileMap = levelParser.Parse(pelletTransforms, boxTiles);

        rows = levelParser.Rows;
        cols = levelParser.Cols;
        boxDoorEntranceCoordinates = levelParser.BoxDoorEntranceCoordinates;
        tileMapHalfWidth = levelParser.TileMapHalfWidth;
        
        entitiesTargetTileCoordinates = new Dictionary<EntityId, Vector2Int>();
        var entityIds = (EntityId[]) Enum.GetValues(typeof(EntityId));
        foreach (var entityId in entityIds)
        {
            entitiesTargetTileCoordinates.Add(entityId, Vector2Int.zero);
        }

        InitializeEntitiesProperties();
    }

    private void Update()
    {
        if (finishedGame)
            return;
        
        foreach (Transform child in pelletPool.transform)
        {
            if (child.gameObject.activeSelf)
                return;
        }
        
        finishedGame = true;
        gameManager.GameOver(true);
    }

    public void InitializeEntitiesProperties()
    {
        int initX = levelParser.InitialPlayerPosition.x;
        int initY = levelParser.InitialPlayerPosition.y;
        player.transform.position = tileMap[initY][initX].gameObject.transform.position;
        gameManager.SetPlayerDirection(levelParser.InitialPlayerDirection);
        Vector2Int targetTileCoordinates = TargetTileFromInitDirection(levelParser.InitialPlayerDirection, initX, initY);
        entitiesTargetTileCoordinates[EntityId.Player] = targetTileCoordinates;
        
        for (int i = 0; i < levelParser.GhostCount; i++)
        {
            var ghost = ghosts[i];
            var ghostX = levelParser.InitialGhostPositions[i].x;
            var ghostY = levelParser.InitialGhostPositions[i].y;
            levelParser.InitialGhostPositions[i] = new Vector2Int(ghostX, ghostY);
            
            ghost.transform.position = tileMap[ghostY][ghostX].gameObject.transform.position;
            
            ghost.currentDirection = levelParser.InitialGhostDirections[i];
            var ghostTargetTile = TargetTileFromInitDirection(levelParser.InitialGhostDirections[i], ghostX, ghostY);
            entitiesTargetTileCoordinates[_ghostOrder[i]] = ghostTargetTile;
        }
    }

    private Vector2Int TargetTileFromInitDirection(Direction initDirection, int initX, int initY)
    {
        switch (initDirection)
        {
            case Direction.Up:
                return new Vector2Int(initX, initY - 1);
            case Direction.Down:
                return new Vector2Int(initX, initY + 1);
            case Direction.Left:
                return new Vector2Int(initX == 0 ? cols - 1 : initX - 1, initY);
            case Direction.Right:
                return new Vector2Int(initX == cols - 1 ? 0 : initX + 1, initY);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    // Get new position based on direction, speed and frame delta time
    public Vector3 GetNewEntityPosition(float movSpeed,Vector2 position, Direction currentDirection)
    {
        Vector3 newPosition;
        float posX;
        switch (currentDirection)
        {
            case Direction.Left:
                posX = position.x - movSpeed * Time.deltaTime;
                newPosition = new Vector3(posX < -tileMapHalfWidth ? tileMapHalfWidth : posX, position.y, 0);
                break;
            case Direction.Right:
                posX = position.x + movSpeed * Time.deltaTime;
                newPosition = new Vector3(posX > tileMapHalfWidth ? -tileMapHalfWidth : posX, position.y, 0);
                break;
            case Direction.Up:
                newPosition = new Vector3(position.x, position.y + movSpeed * Time.deltaTime, 0);
                break;
            case Direction.Down:
                newPosition = new Vector3(position.x, position.y - movSpeed * Time.deltaTime, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return newPosition;
    }
    
    // Check if target was reached and update variables, otherwise simply return input 'position' parameter
    public Vector3 GetValidatedPlayerPosition(Vector3 position, Direction currentDirection, Direction? nextDirection)
    {
        var targetTileCoordinates = entitiesTargetTileCoordinates[EntityId.Player];
        var targetTilePosition = tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
        
        switch (currentDirection)
        {
            case Direction.Up:
                if (position.y >= targetTilePosition.y) // Reached target tile
                {
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        // Next direction is valid, update target and direction
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerCollidedWall(false);
                        
                        var delta = position.y - targetTilePosition.y;
                        return GetNewPositionWithOffset(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Next direction is not valid
                    if (!IsValidDirection(targetTileCoordinates, currentDirection))
                    {
                        // Reached a wall, returned position must be exact
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }
                    
                    // Next tile is not a wall, update target
                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Down:
                if (position.y <= targetTilePosition.y) // Reached target tile
                {
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault())) 
                    {
                        // Next direction is valid, update target and direction
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = targetTilePosition.y - position.y;
                        return GetNewPositionWithOffset(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Next direction is not valid
                    if (!IsValidDirection(targetTileCoordinates, currentDirection))
                    {
                        // Reached a wall, returned position must be exact
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    // Next tile is not a wall, update target
                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Left:
                if (targetTileCoordinates.x == cols - 1 && position.x < 0) break;
                if (position.x <= targetTilePosition.x) // Reached target tile
                {
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        // Next direction is valid, update target and direction
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = position.x - targetTilePosition.x;
                        return GetNewPositionWithOffset(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Next direction is not valid
                    if (!IsValidDirection(targetTileCoordinates, currentDirection))
                    {
                        // Reached a wall, returned position must be exact
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    // Next tile is not a wall, update target
                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Right:
                if (targetTileCoordinates.x == 0 && position.x > 0) break;
                if (position.x >= targetTilePosition.x) // Reached target tile
                {
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        // Next direction is valid, update target and direction
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = targetTilePosition.x - position.x;
                        return GetNewPositionWithOffset(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }

                    // Next direction is not valid
                    if (!IsValidDirection(targetTileCoordinates, currentDirection))
                    {
                        // Reached a wall, returned position must be exact
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    // Next tile is not a wall, update target
                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return position;
    }
    
    
    public Vector3 GetValidatedGhostPosition(EntityId entityId, Vector3 position, Direction currentDirection, Direction nextDirection)
    {
        // Target tile has already been updated to next one just reached
        var reachedTileCoordinates = GetEntityCurrentTileCoordinates(entityId, currentDirection);
        var reachedTilePosition = tileMap[reachedTileCoordinates.y][reachedTileCoordinates.x].transform.position;
        if(currentDirection != nextDirection)
            UpdateTargetTileCoordinates(entityId, reachedTileCoordinates, nextDirection);
        
        switch (currentDirection)
        {
            case Direction.Up:
                return GetNewPositionWithOffset(reachedTilePosition, nextDirection, position.y - reachedTilePosition.y);
            case Direction.Down:
                return GetNewPositionWithOffset(reachedTilePosition, nextDirection, reachedTilePosition.y - position.y);
            case Direction.Left:
                return GetNewPositionWithOffset(reachedTilePosition, nextDirection, position.x - reachedTilePosition.x);
            case Direction.Right:
                return GetNewPositionWithOffset(reachedTilePosition, nextDirection, reachedTilePosition.x - position.x);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    private Vector3 GetNewPositionWithOffset(Vector3 tilePosition, Direction newDirection, float delta)
    {
        switch (newDirection)
        {
            case Direction.Up:
                return new Vector3(tilePosition.x, tilePosition.y + delta, 0);
            case Direction.Down:
                return new Vector3(tilePosition.x, tilePosition.y - delta, 0);
            case Direction.Left:
                return new Vector3(tilePosition.x - delta, tilePosition.y, 0);
            case Direction.Right:
                return new Vector3(tilePosition.x + delta, tilePosition.y, 0);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    private void UpdateTargetTileCoordinates(EntityId entityId, Vector2Int currentTileCoordinates, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(currentTileCoordinates.x, currentTileCoordinates.y - 1);
                break;
            case Direction.Down:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(currentTileCoordinates.x, currentTileCoordinates.y + 1);
                break;
            case Direction.Left:
                if (currentTileCoordinates.x == 0)
                    entitiesTargetTileCoordinates[entityId] = 
                        new Vector2Int(cols - 1, currentTileCoordinates.y);
                else
                    entitiesTargetTileCoordinates[entityId] = 
                        new Vector2Int(currentTileCoordinates.x - 1, currentTileCoordinates.y);
                break;
            case Direction.Right:
                if (currentTileCoordinates.x == cols - 1)
                    entitiesTargetTileCoordinates[entityId] = 
                        new Vector2Int(0, currentTileCoordinates.y);
                else
                    entitiesTargetTileCoordinates[entityId] = 
                        new Vector2Int(currentTileCoordinates.x + 1, currentTileCoordinates.y);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    private bool IsValidDirection(Vector2Int tileCoordinates, Direction nextDirection)
    {
        switch (nextDirection)
        {
            case Direction.Up:
                return !tileMap[tileCoordinates.y - 1][tileCoordinates.x].IsWall;
            case Direction.Down:
                return !tileMap[tileCoordinates.y + 1][tileCoordinates.x].IsWall;
            case Direction.Left:
                if(tileCoordinates.x == 0)
                    return !tileMap[tileCoordinates.y][cols - 1].IsWall;
                else
                    return !tileMap[tileCoordinates.y][tileCoordinates.x - 1].IsWall;
            case Direction.Right:
                if(tileCoordinates.x == cols - 1)
                    return !tileMap[tileCoordinates.y][0].IsWall;
                else
                    return !tileMap[tileCoordinates.y][tileCoordinates.x + 1].IsWall;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    // If input direction is opposite to current direction, update target and return true
    private bool ValidateOppositeInputDirection(Direction inputDirection, Direction currentDirection)
    {
        if (!DirectionHelper.DirectionsAreOpposite(inputDirection, currentDirection))
            return false;
        UpdateTargetTileCoordinates(EntityId.Player, entitiesTargetTileCoordinates[EntityId.Player], inputDirection);
        return true;
    }
    
    
    public bool ValidateInputDirection(Direction inputDirection, Direction currentDirection, bool hasCollidedWall)
    {
        if (!hasCollidedWall)
        {
            if (!ValidateOppositeInputDirection(inputDirection, currentDirection))
            {
                // Input direction cannot be instantly changed
                gameManager.SetPlayerNextDirection(inputDirection);
                return false;
            }
            // Input direction can be instantly changed
            gameManager.SetPlayerDirection(inputDirection);
            return true;
        }
        
        // Player has collided against a wall, so validate if input direction leads to another wall or not
        Vector2Int tileCoordinates = entitiesTargetTileCoordinates[EntityId.Player];
        bool isValidDirection = IsValidDirection(tileCoordinates, inputDirection);
        if (isValidDirection)
        {
            UpdateTargetTileCoordinates(EntityId.Player, entitiesTargetTileCoordinates[EntityId.Player], inputDirection);
            gameManager.SetPlayerCollidedWall(false);
            gameManager.SetPlayerDirection(inputDirection);
        }

        return isValidDirection;
    }

    
    public Vector2Int GetEntityTargetTileCoordinates(EntityId entityId)
    {
        return entitiesTargetTileCoordinates[entityId];
    }

    
    public List<Direction> GetValidDirectionsForTile(Vector2Int position, bool ignoreBoxDoorWall)
    {
        var validDirections = new List<Direction>();
        
        if ((ignoreBoxDoorWall && tileMap[position.y - 1][position.x].IsBoxDoor) || !tileMap[position.y - 1][position.x].IsWall)
            validDirections.Add(Direction.Up);
        
        if ((ignoreBoxDoorWall && tileMap[position.y + 1][position.x].IsBoxDoor) || !tileMap[position.y + 1][position.x].IsWall)
            validDirections.Add(Direction.Down);

        if (position.x == 0)
        {
            if (!tileMap[position.y][cols - 1].IsWall)
                validDirections.Add(Direction.Left);
        }
        else
        {
            if (!tileMap[position.y][position.x - 1].IsWall)
                validDirections.Add(Direction.Left);
        }
        
        if (position.x == cols - 1)
        {
            if (!tileMap[position.y][0].IsWall)
                validDirections.Add(Direction.Right);
        }
        else
        {
            if (!tileMap[position.y][position.x + 1].IsWall)
                validDirections.Add(Direction.Right);
        }
        
        return validDirections;
    }

    
    public bool ReachedTargetTile(EntityId entityId, Vector3 newPosition, Direction currentDirection)
    {
        var targetTileCoordinates = entitiesTargetTileCoordinates[entityId];
        var targetTilePosition = tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
        
        switch (currentDirection)
        {
            case Direction.Up:
                return newPosition.y >= targetTilePosition.y;
            case Direction.Down:
                return newPosition.y <= targetTilePosition.y;
            case Direction.Left:
                if(targetTileCoordinates.x == cols - 1)
                    return newPosition.x > 0 && newPosition.x <= targetTilePosition.x;
                return newPosition.x <= targetTilePosition.x;
            case Direction.Right:
                if(targetTileCoordinates.x == 0)
                    return newPosition.x < 0 && newPosition.x >= targetTilePosition.x;
                return newPosition.x >= targetTilePosition.x;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    // Get entity current tile inferred by target tile
    public Vector2Int GetEntityCurrentTileCoordinates(EntityId entityId, Direction currentDirection)
    {
        var targetTileCoordinates = entitiesTargetTileCoordinates[entityId];
        
        switch (currentDirection)
        {
            case Direction.Up:
                return new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y + 1);
            case Direction.Down:
                return new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y - 1);
            case Direction.Left:
                return new Vector2Int(targetTileCoordinates.x == cols - 1 ? 0 : targetTileCoordinates.x + 1, targetTileCoordinates.y);
            case Direction.Right:
                return new Vector2Int(targetTileCoordinates.x == 0 ? cols - 1 : targetTileCoordinates.x - 1, targetTileCoordinates.y);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    // Called if ghost has reached target tile and no target update was done
    public void UpdateTargetTile(EntityId entityId, Direction currentDirection)
    {
        var targetTileCoordinates = entitiesTargetTileCoordinates[entityId];
        Vector2Int updatedTargetTile;
        
        switch (currentDirection)
        {
            case Direction.Up:
                updatedTargetTile = new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y - 1);
                break;
            case Direction.Down:
                updatedTargetTile = new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y + 1);
                break;
            case Direction.Left:
                if(targetTileCoordinates.x == 0)
                    updatedTargetTile = new Vector2Int(cols - 1, targetTileCoordinates.y);
                else
                    updatedTargetTile = new Vector2Int(targetTileCoordinates.x - 1, targetTileCoordinates.y);
                break;
            case Direction.Right:
                if(targetTileCoordinates.x == cols - 1)
                    updatedTargetTile = new Vector2Int(0, targetTileCoordinates.y);
                else
                    updatedTargetTile = new Vector2Int(targetTileCoordinates.x + 1, targetTileCoordinates.y);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        entitiesTargetTileCoordinates[entityId] = updatedTargetTile;
    }

    public Vector2Int GetOwnCorner(EntityId entityId)
    {
        switch (entityId)
        {
            case EntityId.Blinky:
                return new Vector2Int(cols - 2,1);
            case EntityId.Pinky:
                return new Vector2Int(1,1);
            case EntityId.Inky:
                return new Vector2Int(cols - 2,rows - 2);
            case EntityId.Clyde:
                return new Vector2Int(1,rows - 2);
            default:
                throw new ArgumentOutOfRangeException(nameof(entityId), entityId, null);
        }
    }

    public bool ReachedBoxDoorEntrance(EntityId entityId)
    {
        return entitiesTargetTileCoordinates[entityId].Equals(boxDoorEntranceCoordinates);
    }

    public Vector2Int GetRandomBoxTileCoordinates()
    {
        return boxTiles[Random.Range(0, boxTiles.Count)];
    }

    public bool ReachedTile(EntityId entityId, Vector2Int tile)
    {
        return entitiesTargetTileCoordinates[entityId].Equals(tile);
    }

    public void ResetPellets()
    {
        pelletTransforms.ForEach(pellet => pellet.gameObject.SetActive(true));
        finishedGame = false;
    }

    public EntityId[] GhostOrder => _ghostOrder;
    
    public Vector2Int BoxDoorEntranceCoordinates => boxDoorEntranceCoordinates;
}
