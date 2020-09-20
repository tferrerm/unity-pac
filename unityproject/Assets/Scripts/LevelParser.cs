using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class LevelParser : MonoBehaviour
{
    public TextAsset inputFile;

    public LevelManager levelManager;
    
    private int rows;
    private int cols;
    
    private const int MaxRows = 23;
    private const int MaxCols = 21;

    public List<Sprite> tileSprites;
    private float tileMapHalfWidth;

    private Vector2Int boxDoorEntranceCoordinates;
    
    private Vector2Int _initialPlayerPosition;
    private Direction _initialPlayerDirection;
    
    private int _ghostCount;
    private readonly Vector2Int[] _initialGhostPositions = new Vector2Int[4];
    private readonly Direction[] _initialGhostDirections = new Direction[4];
    
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;
    
    private const string BoxId = "B";
    private const string BoxDoorId = "_";
    private const string PelletId = "o";
    private const string PowerPelletId = "X";
    private const string BlankId = ".";

    public MapComponent[][] Parse(List<Transform> pelletTransforms, List<Vector2Int> boxTiles)
    {
        MapComponent[][] tileMap = null;
        try
        {
            using (var reader = new StringReader(inputFile.text))
            {
                ParseRowsAndCols(reader);
                tileMap = new MapComponent[rows][];
                CreateTileMap(reader, tileMap, pelletTransforms, boxTiles);
                InitializeEntitiesProperties(reader, tileMap);
            
                var tileWidth = (int) tileSprites[0].rect.width;
                tileMapHalfWidth = (float)(cols * tileWidth) / 2;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile.name);
            Debug.LogError(e.Message);
            Application.Quit();
        }
        
        return tileMap;
    }
    
    private void ParseRowsAndCols(StringReader reader)
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
    
    private void CreateTileMap(StringReader reader, MapComponent[][] tileMap, List<Transform> pelletTransforms, 
        List<Vector2Int> boxTiles)
    {
        
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
                CreateTile(input, positionPointer, spriteNameDict, i, j, tileMap, pelletTransforms, boxTiles);

                positionPointer = new Vector3(positionPointer.x + tileWidth, positionPointer.y, 0);
            }

            reader.ReadLine();
            positionPointer = new Vector3(-cols / 2 * tileWidth, positionPointer.y - tileHeight, 0);
        }

        if (boxTiles.Count < levelManager.ghosts.Length - 1)
        {
            Debug.LogError("Insufficient box tiles in map file.");
            Application.Quit();
        }
    }
    
    private void CreateTile(string input, Vector3 positionPointer, Dictionary<string, Sprite> spriteDict,
        int row, int col, MapComponent[][] tileMap, List<Transform> pelletTransforms, List<Vector2Int> boxTiles)
    {
        GameObject tileGO = new GameObject();
        tileGO.transform.SetParent(levelManager.transform);
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
        GameObject pellet;
        if (mapComponent.HasPellet)
        {
            // Instantiate pellet
            tileGO.GetComponent<SpriteRenderer>().sprite = spriteDict["."];
            pellet = Instantiate(pelletPrefab, positionPointer, Quaternion.identity, levelManager.pelletPool.transform);
            pelletTransforms.Add(pellet.transform);
        }
        else if (mapComponent.HasPowerPellet)
        {
            // Instantiate power pellet
            tileGO.GetComponent<SpriteRenderer>().sprite = spriteDict["."];
            pellet = Instantiate(powerPelletPrefab, positionPointer, Quaternion.identity, levelManager.pelletPool.transform);
            pelletTransforms.Add(pellet.transform);
        }
        else
        {
            tileGO.GetComponent<SpriteRenderer>().sprite = input == BoxId ? spriteDict[BlankId] : spriteDict[input];
        }
        
        tileGO.name = $"Tile ({col}, {row})";
    }
    
    public void InitializeEntitiesProperties(StringReader reader, MapComponent[][] tileMap)
    {
        reader.ReadLine(); // Player Header
        
        var input = reader.ReadLine();
        var initX = Int32.Parse(input);

        input = reader.ReadLine();
        var initY = Int32.Parse(input);

        ValidateInitialPosition(initX, initY, tileMap[initY][initX]);

        _initialPlayerPosition = new Vector2Int(initX, initY);

        input = reader.ReadLine();
        _initialPlayerDirection = (Direction) Int32.Parse(input);

        reader.ReadLine(); // Ghosts Header
        
        input = reader.ReadLine();
        _ghostCount = Math.Min(Int32.Parse(input), levelManager.GhostOrder.Length);

        for (int i = 0; i < _ghostCount; i++)
        {
            reader.ReadLine(); // Ghost Header
            
            input = reader.ReadLine();
            var ghostX = Int32.Parse(input);
            input = reader.ReadLine();
            var ghostY = Int32.Parse(input);

            ValidateInitialPosition(ghostX, ghostY, tileMap[ghostY][ghostX]);

            _initialGhostPositions[i] = new Vector2Int(ghostX, ghostY);

            input = reader.ReadLine();
            _initialGhostDirections[i] = (Direction) Int32.Parse(input);
        }
    }
    
    private void ValidateInitialPosition(int initX, int initY, MapComponent tile)
    {
        if (tile.IsWall || initX <= 0 || initX >= cols - 1 || initY <= 0 || initY >= rows - 1)
        {
            throw new Exception("Invalid initial player position.");
        }
    }

    public int Rows => rows;

    public int Cols => cols;

    public Vector2Int BoxDoorEntranceCoordinates => boxDoorEntranceCoordinates;

    public Vector2Int InitialPlayerPosition => _initialPlayerPosition;

    public Direction InitialPlayerDirection => _initialPlayerDirection;

    public int GhostCount => _ghostCount;

    public Vector2Int[] InitialGhostPositions => _initialGhostPositions;

    public Direction[] InitialGhostDirections => _initialGhostDirections;

    public float TileMapHalfWidth => tileMapHalfWidth;
}
