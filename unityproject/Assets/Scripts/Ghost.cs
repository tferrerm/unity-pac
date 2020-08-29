using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Ghost : MonoBehaviour, IEntity
{
    public float movSpeed = 3.9f;
    public Node startingPosition;
    
    /*
     * Each timer belongs to a different iteration. We have four iterations, thereby four pairs of timers
     */
    public int scatterModeTimer1 = 7;
    public int chaseModeTimer1 = 20;
    
    public int scatterModeTimer2 = 7;
    public int chaseModeTimer2 = 20;
    
    public int scatterModeTimer3 = 7;
    public int chaseModeTimer3 = 20;
    
    public int scatterModeTimer4 = 7;

    public int modeChangeIteration = 1;
    public float modeChangeTimer = 0;

    public enum Mode
    {
        Chase,
        Scatter,
        Frightened
    }

    private Mode _currentMode = Mode.Scatter;
    private Mode _previousMode;

    public GameManager gameManager;
    public LevelManager levelManager;
    private Vector2Int _pacManTile;
    private Vector2Int _currentTile;
    public Vector2Int moveToTile;
    /*
     * Start is called before the first frame update.
     * If this is not working let's make sure that Ghost script is after Pac Man script in execution order
     */
    void Start()
    {
        currentDirection = Direction.Up;
    }

    /*
     * Update is called once per frame
     */
    void Update()
    {
        ModeUpdate();
        Move();
    }
    
    /*
     * This function determines whether a mode needs to be changed or not (in that case calls ChangeMode())
     * Ghosts iterate doing the scatter-chase combination. After every chase period, iteration number is
     * incremented.
     */
    private void ModeUpdate()
    {
        if (_currentMode == Mode.Frightened) return;
        modeChangeTimer += Time.deltaTime;
        switch (modeChangeIteration)
        {
            case 1:
            {
                /*
                 * Checking if it's time to switch from scatter to chase
                 */
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer1)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                /*
                 * Checking if it's time to switch from chase to scatter
                 */
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer1)
                {
                    modeChangeIteration = 2;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 2:
            {
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer2)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer2)
                {
                    modeChangeIteration = 3;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 3:
            {
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer3)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
                if (_currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer3)
                {
                    modeChangeIteration = 4;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }

                break;
            }
            case 4:
            {
                /*
                 * If we're in chase mode in the last iteration we're  in chase mode forever
                 */
                if (_currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer4)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }

                break;
            }
        }
    }

    /**
     * Changes mode
     */
    private void ChangeMode(Mode m)
    {
        _currentMode = m;
    }

    private void Move()
    {
        
        _pacManTile = gameManager.GetEntityTargetTileCoordinates(EntityId.Player);
        _currentTile = gameManager.GetEntityTargetTileCoordinates(EntityId.Blinky);

        var foundDirections = levelManager.GetValidDirectionsForTile(_currentTile);
        Debug.Log(foundDirections);

        /*
         * Iterating through the nodes to see which is closer to targetTile (Pac-man)
         */
        var leastDistance = 100000f;
        Vector2Int nextTile = new Vector2Int();
        foreach (var t in foundDirections)
        {
            var xCoord = _currentTile.x;
            var yCoord = _currentTile.y;
            switch (t)
            {
                case Direction.Down: yCoord += 1;
                    break;
                case Direction.Up: yCoord -= 1;
                    break;
                case Direction.Right: xCoord += 1;
                    break;
                case Direction.Left: xCoord -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Vector2Int projectedTile = new Vector2Int(xCoord, yCoord);
            var distance = GetDistance(_pacManTile, projectedTile);
            if (distance < leastDistance)
            {
                currentDirection = t;
                leastDistance = distance;
            }
        }
        Vector3 newPosition = GameManager.GetEntityPosition(movSpeed, transform.position, currentDirection, null);
        transform.position = gameManager.GetValidMovement(EntityId.Blinky, newPosition, currentDirection, null);;
    }

    private static float GetDistance(Vector2Int posA, Vector2Int posB)
    {
        var dx = posA.x - posB.x;
        var dy = posA.y - posB.y;

        var distance = Mathf.Sqrt(dx * dx + dy * dy);
        return distance;
    }
    
    public Direction currentDirection { get; set; }

}
