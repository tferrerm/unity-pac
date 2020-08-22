using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelParser : MonoBehaviour
{
    private const int MaxRows = 23;
    private const int MaxCols = 21;

    private int rows;
    private int cols;

    public String inputFile = "Assets/Resources/level_classic.txt";

    private Vector3 positionPointer = Vector3.zero;
    public List<Sprite> tileSprites;

    // Start is called before the first frame update
    void Start()
    {
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

                ValidateRowsAndCols();
                
                positionPointer = new Vector3(-cols/2 * tileWidth, rows/2 * tileHeight, 0);
                
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        input = ((char)reader.Read()).ToString();
                        GameObject go = new GameObject();
                        go.transform.SetParent(this.transform);
                        go.transform.position = positionPointer;
                        go.AddComponent<SpriteRenderer>();
                        go.GetComponent<SpriteRenderer>().sprite = spriteDict[input];
                        go.name = $"Tile ({i}, {j})";
                        
                        positionPointer = new Vector3(positionPointer.x + tileWidth, positionPointer.y, 0);
                    }
                    reader.ReadLine();
                    positionPointer = new Vector3(-cols/2 * tileWidth, positionPointer.y - tileHeight, 0);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + inputFile);
            Debug.LogError(e.Message);
            Application.Quit();
        }
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
}
