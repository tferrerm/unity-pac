using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private const int MaxRows = 23;
    private const int MaxCols = 21;

    private int rows;
    private int cols;

    public String inputFile = "Assets/Resources/level_classic.txt";

    public List<Sprite> tileSprites;
    
    public GameObject pelletPool;
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;
    
    private MapComponent[][] tileMap;
    private Dictionary<EntityId, Vector2Int> entitiesTargetTileCoordinates;

    public Player player;
    public GameManager gameManager;
    public Ghost blinky;

    private const string PelletId = "o";
    private const string PowerPelletId = "X";
    private const string BlankId = ".";

    // Start is called before the first frame update
    void Start()
    {
        entitiesTargetTileCoordinates = new Dictionary<EntityId, Vector2Int>();

        try
        {
            using (StreamReader reader = new StreamReader(inputFile))
            {
                ParseRowsAndCols(reader);
                CreateTileMap(reader);
                InitializePlayerProperties(reader);
                
                entitiesTargetTileCoordinates.Add(EntityId.Blinky, new Vector2Int(11, 9)); // TODO HARDCODED
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile);
            Debug.LogError(e.Message);
            Application.Quit();
        }
    }
    

    private void CreateTileMap(StreamReader reader)
    {
        tileMap = new MapComponent[rows][];
        var tileWidth = (int) tileSprites[0].rect.width;
        var tileHeight = (int) tileSprites[0].rect.height;
        Dictionary<String, Sprite> spriteNameDict = new Dictionary<String, Sprite>();
        tileSprites.ForEach(sprite => spriteNameDict.Add(sprite.name, sprite));

        Vector3 positionPointer = new Vector3(-cols / 2 * tileWidth, rows / 2 * tileHeight, 0);

        for (int i = 0; i < rows; i++)
        {
            tileMap[i] = new MapComponent[cols];
            for (int j = 0; j < cols; j++)
            {
                var input = ((char) reader.Read()).ToString();
                CreateTile(input, positionPointer, spriteNameDict, i, j);

                positionPointer = new Vector3(positionPointer.x + tileWidth, positionPointer.y, 0);
            }

            reader.ReadLine();
            positionPointer = new Vector3(-cols / 2 * tileWidth, positionPointer.y - tileHeight, 0);
        }
    }


    private void CreateTile(string input, Vector3 positionPointer, Dictionary<string, Sprite> spriteDict, int row,
        int col)
    {
        GameObject tileGO = new GameObject();
        tileGO.transform.SetParent(transform);
        tileGO.transform.position = positionPointer;

        MapComponent mapComponent = tileGO.AddComponent<MapComponent>();
        switch (input)
        {
            case PelletId:
                mapComponent.HasPellet = true;
                break;
            case PowerPelletId:
                mapComponent.HasPowerPellet = true;
                break;
            case BlankId:
                break;
            default:
                mapComponent.IsWall = true;
                break;
        }
        tileMap[row][col] = mapComponent;

        tileGO.AddComponent<SpriteRenderer>();
        if (mapComponent.HasPellet)
        {
            // Instantiate pellet
            tileGO.GetComponent<SpriteRenderer>().sprite = spriteDict["."];
            Instantiate(pelletPrefab, positionPointer, Quaternion.identity, pelletPool.transform);
        }
        else if (mapComponent.HasPowerPellet)
        {
            // Instantiate power pellet
            tileGO.GetComponent<SpriteRenderer>().sprite = spriteDict["."];
            Instantiate(powerPelletPrefab, positionPointer, Quaternion.identity, pelletPool.transform);
        }
        else
        {
            tileGO.GetComponent<SpriteRenderer>().sprite = spriteDict[input];
        }
        
        tileGO.name = $"Tile ({col}, {row})";
    }
    
    
    private void InitializePlayerProperties(StreamReader reader)
    {
        var input = reader.ReadLine();
        var initY = Int32.Parse(input);

        input = reader.ReadLine();
        var initX = Int32.Parse(input);

        if (tileMap[initY][initX].IsWall || initX <= 0 || initX >= cols - 1 || initY <= 0 || initY >= rows - 1)
        {
            throw new Exception("Invalid initial player position.");
        }

        player.transform.position = tileMap[initY][initX].gameObject.transform.position;

        input = reader.ReadLine();
        Direction initDirection = (Direction) Int32.Parse(input);
        gameManager.SetPlayerDirection(initDirection);
        Vector2Int targetTileCoordinates;
        switch (initDirection)
        {
            case Direction.Up:
                targetTileCoordinates = new Vector2Int(initX, initY - 1);
                break;
            case Direction.Down:
                targetTileCoordinates = new Vector2Int(initX, initY + 1);
                break;
            case Direction.Left:
                targetTileCoordinates = new Vector2Int(initX - 1, initY);
                break;
            case Direction.Right:
                targetTileCoordinates = new Vector2Int(initX + 1, initY);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        entitiesTargetTileCoordinates.Add(EntityId.Player, targetTileCoordinates);
    }

    
    private void ParseRowsAndCols(StreamReader reader)
    {
        var input = reader.ReadLine();
        rows = Int32.Parse(input);

        input = reader.ReadLine();
        cols = Int32.Parse(input);
        
        if (rows <= 0 || cols <= 0)
        {
            throw new Exception("Negative rows or columns.");
        }
        if (rows % 2 == 0 || cols % 2 == 0)
        {
            throw new Exception("Uneven rows or columns.");
        }
        if (rows > MaxRows || cols > MaxCols)
        {
            throw new Exception($"Cannot exceed {MaxRows} rows and {MaxCols} columns.");
        }
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
                    if (tileMap[targetTileCoordinates.y - 1][targetTileCoordinates.x].IsWall)
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
                    if (tileMap[targetTileCoordinates.y + 1][targetTileCoordinates.x].IsWall)
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
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x - 1].IsWall)
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
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x + 1].IsWall)
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

    
    private void UpdateTargetTileCoordinates(EntityId entityId, Vector2Int targetTileCoordinates,
        Direction nextDirection)
    {
        switch (nextDirection)
        {
            case Direction.Up:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y - 1);
                break;
            case Direction.Down:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y + 1);
                break;
            case Direction.Left:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(targetTileCoordinates.x - 1, targetTileCoordinates.y);
                break;
            case Direction.Right:
                entitiesTargetTileCoordinates[entityId] =
                    new Vector2Int(targetTileCoordinates.x + 1, targetTileCoordinates.y);
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
                return !tileMap[tileCoordinates.y][tileCoordinates.x - 1].IsWall;
            case Direction.Right:
                return !tileMap[tileCoordinates.y][tileCoordinates.x + 1].IsWall;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    // If input direction is opposite to current direction, update target and return true
    private bool ValidateOppositeInputDirection(Direction inputDirection, Direction currentDirection)
    {
        if (!gameManager.DirectionsAreOpposite(inputDirection, currentDirection))
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

    
    public List<Direction> GetValidDirectionsForTile(Vector2Int position)
    {
        var validDirections = new List<Direction>();
        if (!tileMap[position.y - 1][position.x].IsWall)
        {
            validDirections.Add(Direction.Up);
        }
        if (!tileMap[position.y + 1][position.x].IsWall)
        {
            validDirections.Add(Direction.Down);
        }
        if (!tileMap[position.y][position.x - 1].IsWall)
        {
            validDirections.Add(Direction.Left);
        }
        if (!tileMap[position.y][position.x + 1].IsWall)
        {
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
                return newPosition.x <= targetTilePosition.x;
            case Direction.Right:
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
                return new Vector2Int(targetTileCoordinates.x + 1, targetTileCoordinates.y);
            case Direction.Right:
                return new Vector2Int(targetTileCoordinates.x - 1, targetTileCoordinates.y);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    // Called if ghost has reached target tile and no update was done
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
                updatedTargetTile = new Vector2Int(targetTileCoordinates.x - 1, targetTileCoordinates.y);
                break;
            case Direction.Right:
                updatedTargetTile = new Vector2Int(targetTileCoordinates.x + 1, targetTileCoordinates.y);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        entitiesTargetTileCoordinates[entityId] = updatedTargetTile;
    }
}
    