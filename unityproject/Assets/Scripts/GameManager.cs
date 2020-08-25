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
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetValidMovement(EntityId entityId, Vector3 position, Direction direction) // OR RETURN VECTOR3
    {
        return levelManager.GetValidMovement(entityId, position, direction);
    }

    public void SetPlayerDirection(Direction direction)
    {
        player.CurrentDirection = direction;
    }

    public bool IsValidDirection(EntityId entityId, Direction direction)
    {
        return levelManager.IsValidDirection(entityId, direction);
    }
}
