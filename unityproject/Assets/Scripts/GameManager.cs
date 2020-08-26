using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityId  
{  
    Player = 0
};

public class GameManager : MonoBehaviour
{
    public LevelManager levelManager;

    public Player player;

    private Dictionary<EntityId, IEntity> entityDict;
    
    // Start is called before the first frame update
    void Start()
    {
        entityDict = new Dictionary<EntityId, IEntity>();
        entityDict.Add(EntityId.Player, player);
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
        if (inputDirection == currentDirection || !levelManager.ValidateOppositeDirection(entityId, inputDirection, currentDirection)) return false;
        
        entityDict[entityId].currentDirection = inputDirection;
        return true;
    }

    public void SetPlayerCollidedWall(bool hasCollided)
    {
        player.HasCollidedWall = hasCollided;
    }
}
