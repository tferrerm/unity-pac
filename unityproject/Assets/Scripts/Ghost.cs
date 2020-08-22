using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Ghost : MonoBehaviour
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

    private GameObject _pacMan;

    private Node _currentNode, _targetNode, _previousNode;
    private Vector2 _direction, _nextDirection;
    
    
    
    /*
     * Start is called before the first frame update
     */
    void Start()
    {
        
    }

    /*
     * Update is called once per frame
     */
    void Update()
    {
        
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

    private GameObject GetPortal(Vector2 pos)
    {
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[(int) pos.x, (int) pos.y];
        if (tile == null) return null;
        if (!tile.GetComponent<Tile>().isPortal) return null;
        GameObject otherPortal = tile.GetComponent<Tile>().portalReceiver;
        return otherPortal;

    }

    private Node GetNodeAtPosition(Vector2 pos)
    {
        /*
         * TODO: CAMBIAR GameBoard por el nombre del board real
         */
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[(int) pos.x, (int) pos.y];
        if (tile == null) return null;
        return tile.GetComponent<Node>() ?? null;
    }

    private float LengthFromNode(Vector2 targetPosition)
    {
        var vec = targetPosition - (Vector2) _previousNode.transform.position;
        return vec.sqrMagnitude;
    }
}
