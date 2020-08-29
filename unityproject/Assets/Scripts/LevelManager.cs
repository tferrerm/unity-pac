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

        Dictionary<String, Sprite> spriteDict = new Dictionary<String, Sprite>();
        tileSprites.ForEach(sprite => spriteDict.Add(sprite.name, sprite));
        var tileWidth = (int) tileSprites[0].rect.width;
        var tileHeight = (int) tileSprites[0].rect.height;
        try
        {
            using (StreamReader reader = new StreamReader(inputFile))
            {
                var input = reader.ReadLine();
                rows = Int32.Parse(input);

                input = reader.ReadLine();
                cols = Int32.Parse(input);

                ValidateRowsAndCols(); // TODO CHECK NEGATIVES

                tileMap = new MapComponent[rows][];

                Vector3 positionPointer = new Vector3(-cols / 2 * tileWidth, rows / 2 * tileHeight, 0);

                for (int i = 0; i < rows; i++)
                {
                    tileMap[i] = new MapComponent[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        input = ((char) reader.Read()).ToString();
                        CreateTile(input, positionPointer, spriteDict, i, j);

                        positionPointer = new Vector3(positionPointer.x + tileWidth, positionPointer.y, 0);
                    }

                    reader.ReadLine();
                    positionPointer = new Vector3(-cols / 2 * tileWidth, positionPointer.y - tileHeight, 0);
                }

                input = reader.ReadLine();
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
                entitiesTargetTileCoordinates.Add(EntityId.Blinky, new Vector2Int(11, 9));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile);
            Debug.LogError(e.Message);
            Application.Quit();
        }
    }

    private void CreateTile(string input, Vector3 positionPointer, Dictionary<string, Sprite> spriteDict, int row,
        int col)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(this.transform);
        go.transform.position = positionPointer;

        MapComponent mp = go.AddComponent<MapComponent>();
        switch (input)
        {
            case "o":
                mp.HasPellet = true;
                break;
            case "X":
                mp.HasPowerPellet = true;
                break;
            case ".":
                break;
            default:
                mp.IsWall = true;
                break;
        }

        tileMap[row][col] = mp;

        go.AddComponent<SpriteRenderer>();
        go.GetComponent<SpriteRenderer>().sprite = spriteDict[input];
        go.name = $"Tile ({col}, {row})";
    }

    private void ValidateRowsAndCols()
    {
        if (rows % 2 == 0 || cols % 2 == 0)
        {
            throw new Exception("Uneven rows or columns.");
        }

        if (rows > MaxRows || cols > MaxCols)
        {
            throw new Exception($"Cannot exceed {MaxRows} rows and {MaxCols} columns.");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Check if target was reached, otherwise return position parameter
    public Vector3 GetValidatedPlayerPosition(Vector3 position, Direction currentDirection, Direction? nextDirection)
    {
        var targetTileCoordinates = entitiesTargetTileCoordinates[EntityId.Player];
        var targetTilePosition = tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
        
        switch (currentDirection)
        {
            case Direction.Up:
                if (position.y >= targetTilePosition.y) // Reached target tile
                {
                    // Entity must be player if true
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerCollidedWall(false);

                        var delta = position.y - targetTilePosition.y;
                        return GetPositionAfterRedirection(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Reached a wall, returned position must be exact
                    if (tileMap[targetTileCoordinates.y - 1][targetTileCoordinates.x].IsWall)
                    {
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Down:
                if (position.y <= targetTilePosition.y) // Reached target tile
                {
                    // Entity must be player if true
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault())) 
                    {
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = targetTilePosition.y - position.y;
                        return GetPositionAfterRedirection(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Reached a wall, returned position must be exact
                    if (tileMap[targetTileCoordinates.y + 1][targetTileCoordinates.x].IsWall)
                    {
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Left:
                if (position.x <= targetTilePosition.x) // Reached target tile
                {
                    // Entity must be player if true
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = position.x - targetTilePosition.x;
                        return GetPositionAfterRedirection(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }
                    
                    // Reached a wall, returned position must be exact
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x - 1].IsWall)
                    {
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

                    UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, currentDirection);
                }

                break;
            case Direction.Right:
                if (position.x >= targetTilePosition.x) // Reached target tile
                {
                    // Entity must be player if true
                    if (nextDirection != null && IsValidDirection(targetTileCoordinates, nextDirection.GetValueOrDefault()))
                    {
                        UpdateTargetTileCoordinates(EntityId.Player, targetTileCoordinates, nextDirection.GetValueOrDefault());
                        gameManager.SetPlayerDirection(nextDirection.GetValueOrDefault());

                        var delta = targetTilePosition.x - position.x;
                        return GetPositionAfterRedirection(targetTilePosition, nextDirection.GetValueOrDefault(), delta);
                    }

                    // Reached a wall, returned position must be exact
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x + 1].IsWall)
                    {
                        gameManager.SetPlayerCollidedWall(true);
                        return targetTilePosition;
                    }

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
        var reachedTileCoordinates = GetEntityCurrentTileCoordinates(entityId, currentDirection);// entitiesTargetTileCoordinates[entityId];
        var reachedTilePosition = tileMap[reachedTileCoordinates.y][reachedTileCoordinates.x].transform.position;
        if(currentDirection != nextDirection)
            UpdateTargetTileCoordinates(entityId, reachedTileCoordinates, nextDirection);
        
        switch (currentDirection)
        {
            case Direction.Up:
                return GetPositionAfterRedirection(reachedTilePosition, nextDirection, position.y - reachedTilePosition.y);
            case Direction.Down:
                return GetPositionAfterRedirection(reachedTilePosition, nextDirection, reachedTilePosition.y - position.y);
            case Direction.Left:
                return GetPositionAfterRedirection(reachedTilePosition, nextDirection, position.x - reachedTilePosition.x);
            case Direction.Right:
                return GetPositionAfterRedirection(reachedTilePosition, nextDirection, reachedTilePosition.x - position.x);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Vector3
        GetPositionAfterRedirection(Vector3 tilePosition, Direction newDirection,
            float delta) // Refers to reached tile when switched direction
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
                break;
            case Direction.Down:
                return !tileMap[tileCoordinates.y + 1][tileCoordinates.x].IsWall;
                break;
            case Direction.Left:
                return !tileMap[tileCoordinates.y][tileCoordinates.x - 1].IsWall;
                break;
            case Direction.Right:
                return !tileMap[tileCoordinates.y][tileCoordinates.x + 1].IsWall;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // Check if entity can instantly move in one direction (only opposite), and update target if true
    public bool ValidateOppositeDirection(EntityId entityId, Direction inputDirection, Direction currentDirection)
    {
        switch (currentDirection)
        {
            case Direction.Up:
                if (inputDirection != Direction.Down)
                    return false;
                break;
            case Direction.Down:
                if (inputDirection != Direction.Up)
                    return false;
                break;
            case Direction.Left:
                if (inputDirection != Direction.Right)
                    return false;
                break;
            case Direction.Right:
                if (inputDirection != Direction.Left)
                    return false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        UpdateTargetTileCoordinates(entityId, entitiesTargetTileCoordinates[entityId], inputDirection);
        return true;
    }
    
    public bool ValidateOppositeDirection(EntityId entityId, Direction inputDirection, Direction currentDirection, bool hasCollidedWall)
    {
        if (!hasCollidedWall)
        {
            if (!ValidateOppositeDirection(entityId, inputDirection, currentDirection))
            {
                gameManager.SetPlayerCollidedWall(false);
                gameManager.SetPlayerNextDirection(inputDirection);
                return false;
            }

            return true;
        }
        
        Vector2Int tileCoordinates = entitiesTargetTileCoordinates[entityId];
        bool isValidDirection;
        switch (inputDirection)
        {
            case Direction.Up:
                isValidDirection = !tileMap[tileCoordinates.y - 1][tileCoordinates.x].IsWall;
                break;
            case Direction.Down:
                isValidDirection = !tileMap[tileCoordinates.y + 1][tileCoordinates.x].IsWall;
                break;
            case Direction.Left:
                isValidDirection = !tileMap[tileCoordinates.y][tileCoordinates.x - 1].IsWall;
                break;
            case Direction.Right:
                isValidDirection = !tileMap[tileCoordinates.y][tileCoordinates.x + 1].IsWall;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (isValidDirection)
        {
            UpdateTargetTileCoordinates(entityId, entitiesTargetTileCoordinates[entityId], inputDirection);
            gameManager.SetPlayerCollidedWall(false);
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
    