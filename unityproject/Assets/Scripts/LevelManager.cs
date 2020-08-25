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

    private const string PelletId = "o";
    private const string PowerPelletId = "X";
    private const string BlankId = ".";

    // Start is called before the first frame update
    void Start()
    {
        entitiesTargetTileCoordinates = new Dictionary<EntityId, Vector2Int>();
        
        Dictionary<String, Sprite> spriteDict = new Dictionary<String, Sprite>();
        tileSprites.ForEach(sprite => spriteDict.Add(sprite.name, sprite));
        var tileWidth = (int)tileSprites[0].rect.width;
        var tileHeight = (int)tileSprites[0].rect.height;
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

                Vector3 positionPointer = new Vector3(-cols/2 * tileWidth, rows/2 * tileHeight, 0);
                
                for (int i = 0; i < rows; i++)
                {
                    tileMap[i] = new MapComponent[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        input = ((char)reader.Read()).ToString();
                        CreateTile(input, positionPointer, spriteDict, i, j);

                        positionPointer = new Vector3(positionPointer.x + tileWidth, positionPointer.y, 0);
                    }
                    reader.ReadLine();
                    positionPointer = new Vector3(-cols/2 * tileWidth, positionPointer.y - tileHeight, 0);
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
                Direction initDirection = (Direction)Int32.Parse(input);
                GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>().SetPlayerDirection(initDirection);
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
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile);
            Debug.LogError(e.Message);
            Application.Quit();
        }
    }

    private void CreateTile(string input, Vector3 positionPointer, Dictionary<string, Sprite> spriteDict, int row, int col)
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
        go.name = $"Tile ({row}, {col})";
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

    public Vector3 GetValidMovement(EntityId entityId, Vector3 position, Direction direction)
    {
        Vector2Int targetTileCoordinates = this.entitiesTargetTileCoordinates[entityId];
        switch (direction)
        {
            case Direction.Up:
                if (position.y >= tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position.y) // Reached target tile
                {
                    if (tileMap[targetTileCoordinates.y - 1][targetTileCoordinates.x].IsWall) // Reached a wall, returned position must be exact
                    {
                        return tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
                    }
                    entitiesTargetTileCoordinates[entityId] = new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y - 1); // Update target tile
                }
                break;
            case Direction.Down:
                if (position.y <= tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position.y) // Reached target tile
                {
                    if (tileMap[targetTileCoordinates.y + 1][targetTileCoordinates.x].IsWall) // Reached a wall, returned position must be exact
                    {
                        return tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
                    }
                    entitiesTargetTileCoordinates[entityId] = new Vector2Int(targetTileCoordinates.x, targetTileCoordinates.y + 1); // Update target tile
                }
                break;
            case Direction.Left:
                if (position.x <= tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position.x) // Reached target tile
                {
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x - 1].IsWall) // Reached a wall, returned position must be exact
                    {
                        return tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
                    }
                    entitiesTargetTileCoordinates[entityId] = new Vector2Int(targetTileCoordinates.x - 1, targetTileCoordinates.y); // Update target tile
                }
                break;
            case Direction.Right:
                if (position.x >= tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position.x) // Reached target tile
                {
                    if (tileMap[targetTileCoordinates.y][targetTileCoordinates.x + 1].IsWall) // Reached a wall, returned position must be exact
                    {
                        return tileMap[targetTileCoordinates.y][targetTileCoordinates.x].transform.position;
                    }
                    entitiesTargetTileCoordinates[entityId] = new Vector2Int(targetTileCoordinates.x + 1, targetTileCoordinates.y); // Update target tile
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return position;
    }

    public bool IsValidDirection(EntityId entityId, Direction direction)
    {
        throw new NotImplementedException();
    }
}
