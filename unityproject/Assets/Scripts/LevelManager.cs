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
    public Ghost[] ghosts = new Ghost[4];
    private int _ghostCount;
    private readonly EntityId[] _ghostOrder = {EntityId.Blinky, EntityId.Inky,
        EntityId.Pinky, EntityId.Clyde};

    private Vector2Int _initialPlayerPosition;
    private Direction _initialPlayerDirection;
    private readonly Vector2Int[] _initialGhostPositions = new Vector2Int[4];
    private readonly Direction[] _initialGhostDirections = new Direction[4];

    private List<Vector2Int> boxTiles = new List<Vector2Int>();
    private Vector2Int boxDoorCoordinates;
    private Vector2Int boxDoorEntranceCoordinates;
    public Vector2Int BoxDoorEntranceCoordinates => boxDoorEntranceCoordinates;
    private const string BoxId = "B";
    private const string BoxDoorId = "_";

    private const string PelletId = "o";
    private const string PowerPelletId = "X";
    private const string BlankId = ".";

    private float tileMapHalfWidth;
    public float TileMapHalfWidth => tileMapHalfWidth;

    private AudioSource _audioSource;
    public AudioClip siren;
    public AudioClip frightenedMode;
    public AudioClip consumedGhost;

    // Start is called before the first frame update
    void Awake()
    {
        entitiesTargetTileCoordinates = new Dictionary<EntityId, Vector2Int>();

        try
        {
            using (StreamReader reader = new StreamReader(inputFile))
            {
                ParseRowsAndCols(reader);
                CreateTileMap(reader);
                InitializePlayerProperties(reader);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile);
            Debug.LogError(e.Message);
            Application.Quit();
        }
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        PlaySiren();
    }

    private void CreateTileMap(StreamReader reader)
    {
        tileMap = new MapComponent[rows][];
        var tileWidth = (int) tileSprites[0].rect.width;
        var tileHeight = (int) tileSprites[0].rect.height;
        tileMapHalfWidth = (float)(cols * tileWidth) / 2;
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

        if (boxTiles.Count < ghosts.Length - 1)
        {
            Debug.LogError("Insufficient box tiles in map file.");
            Application.Quit();
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
            case BoxId:
                boxTiles.Add(new Vector2Int(col, row));
                break;
            case BoxDoorId:
                /*if (boxDoorCoordinates != null)
                {
                    Debug.LogError("Only one box door tile is allowed.");
                    Application.Quit();
                }*/
                boxDoorCoordinates = new Vector2Int(col, row);
                boxDoorEntranceCoordinates = new Vector2Int(col, row - 1);
                mapComponent.IsBoxDoor = true;
                mapComponent.IsWall = true;
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
            tileGO.GetComponent<SpriteRenderer>().sprite = input == BoxId ? spriteDict[BlankId] : spriteDict[input];
        }
        
        tileGO.name = $"Tile ({col}, {row})";
    }
    
    
    public void InitializePlayerProperties(StreamReader reader)
    {
        var input = reader.ReadLine();
        var initY = Int32.Parse(input);

        input = reader.ReadLine();
        var initX = Int32.Parse(input);

        ValidateInitialPosition(initX, initY);

        _initialPlayerPosition = new Vector2Int(initX, initY);
        player.transform.position = tileMap[initY][initX].gameObject.transform.position;

        input = reader.ReadLine();
        _initialPlayerDirection = (Direction) Int32.Parse(input);
        gameManager.SetPlayerDirection(_initialPlayerDirection);
        Vector2Int targetTileCoordinates = TargetTileFromInitDirection(_initialPlayerDirection, initX, initY);

        entitiesTargetTileCoordinates.Add(EntityId.Player, targetTileCoordinates);

        input = reader.ReadLine();
        _ghostCount = Math.Min(Int32.Parse(input), _ghostOrder.Length);

        for (int i = 0; i < _ghostCount; i++)
        {
            input = reader.ReadLine();
            var ghostX = Int32.Parse(input);
            input = reader.ReadLine();
            var ghostY = Int32.Parse(input);

            ValidateInitialPosition(ghostX, ghostY);

            var ghost = ghosts[i];
            _initialGhostPositions[i] = new Vector2Int(ghostX, ghostY);

            ghost.transform.position = tileMap[ghostY][ghostX].gameObject.transform.position;

            input = reader.ReadLine();
            _initialGhostDirections[i] = (Direction) Int32.Parse(input);

            ghost.currentDirection = _initialGhostDirections[i];
            var ghostTargetTile = TargetTileFromInitDirection(_initialGhostDirections[i], ghostX, ghostY);
            entitiesTargetTileCoordinates.Add(_ghostOrder[i], ghostTargetTile);
            
            Debug.Log($"{_ghostOrder[i]} {ghostX},{ghostY} {ghost.transform.position} {ghost.currentDirection} Target: {ghostTargetTile}");
        }
    }

    public void InitializePlayerProperties()
    {
        int initX = _initialPlayerPosition.x;
        int initY = _initialPlayerPosition.y;
        player.transform.position = tileMap[initY][initX].gameObject
            .transform.position;
        gameManager.SetPlayerDirection(_initialPlayerDirection);
        Vector2Int targetTileCoordinates = TargetTileFromInitDirection(_initialPlayerDirection, initX, initY);
        entitiesTargetTileCoordinates[EntityId.Player] = targetTileCoordinates;
        
        for (int i = 0; i < _ghostCount; i++)
        {
            var ghost = ghosts[i];
            var ghostX = _initialGhostPositions[i].x;
            var ghostY = _initialGhostPositions[i].y;
            _initialGhostPositions[i] = new Vector2Int(ghostX, ghostY);
            
            ghost.currentDirection = _initialGhostDirections[i];
            var ghostTargetTile = TargetTileFromInitDirection(_initialGhostDirections[i], ghostX, ghostY);
            entitiesTargetTileCoordinates[_ghostOrder[i]] = ghostTargetTile;
        }
    }

    private void ValidateInitialPosition(int initX, int initY)
    {
        if (tileMap[initY][initX].IsWall || initX <= 0 || initX >= cols - 1 || initY <= 0 || initY >= rows - 1)
        {
            throw new Exception("Invalid initial player position.");
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

    public void PlaySiren()
    {
        PlayLoop(siren);
    }

    public void PlayFrightenedMode()
    {
        PlayLoop(frightenedMode);
    }

    public void PlayConsumedGhost()
    {
        PlayLoop(consumedGhost);
    }

    public void StopSiren()
    {
        _audioSource.Stop();
    }

    private void PlayLoop(AudioClip clip)
    {
        _audioSource.clip = clip;
        _audioSource.loop = true;
        _audioSource.Play();
    }

    public bool ReachedBoxDoorEntrance(EntityId entityId)
    {
        return entitiesTargetTileCoordinates[entityId].Equals(boxDoorEntranceCoordinates);
    }
}


    