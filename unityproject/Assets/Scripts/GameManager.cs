using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public LevelManager levelManager;

    public Player player;
    public Ghost[] ghosts = new Ghost[4];

    private readonly List<Direction> oppositeXDirections = new List<Direction>(new [] {Direction.Left, Direction.Right});
    private readonly List<Direction> oppositeYDirections = new List<Direction>(new [] {Direction.Up, Direction.Down});
    
    private float tileMapHalfWidth;
    
    // Start is called before the first frame update
    void Start()
    {
        tileMapHalfWidth = levelManager.TileMapHalfWidth;
    }

    public Vector3 GetValidatedPosition(EntityId entityId, Vector3 position, Direction currentDirection, Direction? nextDirection)
    {
        return entityId == EntityId.Player ?
            levelManager.GetValidatedPlayerPosition(position, currentDirection, nextDirection) :
            levelManager.GetValidatedGhostPosition(entityId, position, currentDirection, nextDirection.GetValueOrDefault());
    }

    public void SetPlayerDirection(Direction direction)
    {
        player.CurrentDirection = direction;
        player.NextDirection = null;
    }

    public Direction GetPlayerDirection()
    {
        return player.currentDirection;
    }

    public void ValidateInputDirection(Direction inputDirection, Direction currentDirection, bool hasCollidedWall)
    {
        levelManager.ValidateInputDirection(inputDirection, currentDirection, hasCollidedWall);
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

    public bool DirectionsAreOpposite(Direction direction1, Direction direction2)
    {
        if (direction1 == direction2) return false;
        
        return (oppositeXDirections.Contains(direction1) && oppositeXDirections.Contains(direction2)) ||
               (oppositeYDirections.Contains(direction1) && oppositeYDirections.Contains(direction2));
    }

    public Vector2Int GetEntityCurrentTileCoordinates(EntityId entityId, Direction currentDirection)
    {
        return levelManager.GetEntityCurrentTileCoordinates(entityId, currentDirection);
    }

    public void DecrementLives()
    {
        int remainingLives = player.DecrementLives();
        player.OnPauseGame();

        if (remainingLives == 0)
        {
            Debug.Log("Game Over!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }

    public void StopGhosts()
    {
        foreach (var ghost in ghosts)
        {
            ghost.OnPauseGame();
        }
    }

    public void StartGhosts()
    {
        foreach (var ghost in ghosts)
        {
            ghost.OnResumeGame();
        }
    }
    
    public void ResetPositions()
    {
        levelManager.InitializePlayerProperties();
        StartGhosts();
    }
}
