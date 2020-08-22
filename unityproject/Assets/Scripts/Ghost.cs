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
     * Start is called before the first frame update.
     * If this is not working let's make sure that Ghost script is after Pac Man script in execution order
     */
    void Start()
    {
        _pacMan = GameObject.FindGameObjectWithTag("Player");

        var node = GetNodeAtPosition(transform.localPosition);
        if (node != null)
        {
            _currentNode = node;
        }
        _direction = Vector2.right;

        _previousNode = _currentNode;
        Vector2 pacmanPosition = _pacMan.transform.position;
        var targetTile = new Vector2(Mathf.RoundToInt(pacmanPosition.x), Mathf.RoundToInt(pacmanPosition.y));
        _targetNode = GetNodeAtPosition(targetTile);
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
     * 
     */
    private void Move()
    {
        if (_targetNode != _currentNode && _targetNode != null)
        {
            if (OverShotTarget())
            {
                _currentNode = _targetNode;
                transform.localPosition = _currentNode.transform.position;
                var otherPortal = GetPortal(_currentNode.transform.position);
                if (otherPortal != null)
                {
                    transform.localPosition = otherPortal.transform.position;
                    _currentNode = otherPortal.GetComponent<Node>();
                }

                _targetNode = ChooseNextNode();
                _previousNode = _currentNode;
                _currentNode = null;
            }
            else
            {
                transform.localPosition += (Vector3) _direction * (movSpeed * Time.deltaTime);
            }
        }
            
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

    private Node ChooseNextNode()
    {
        Vector2 pacmanPosition = _pacMan.transform.position;
        var targetTile = new Vector2(Mathf.RoundToInt(pacmanPosition.x), Mathf.RoundToInt(pacmanPosition.y));

        Node moveToNode = null;
        var foundNodes = new Node[4];
        var foundNodesDirection = new Vector2[4];
        var nodeCounter = 0;

        /*
         * At every moment a ghost could possibly move in 4 directions. Here we need to check which directions
         * are really available considering walls and the fact that sometimes the ghost can't turn around
         */
        for (var i = 0; i < _currentNode.neighbors.Lenght; i++)
        {
            if (_currentNode.validDirections[i] != _direction * -1)
            {
                foundNodes[nodeCounter] = _currentNode.neighbors[i];
                foundNodesDirection[nodeCounter] = _currentNode.validDirections[i];
                nodeCounter++;
            }
        }

        if (foundNodes.Length == 1)
        {
            moveToNode = foundNodes[0];
        }

        /*
         * Iterating through the nodes to see which is closer to targetTile (Pac-man)
         */
        if (foundNodes.Length <= 1) return moveToNode;
        
        var leastDistance = 100000f;
        for (var i = 0; i < foundNodes.Length; i++)
        {
            if (foundNodesDirection[i] == Vector2.zero) continue;
            var distance = GetDistance(foundNodes[i].transform.position, targetTile);
            if (!(distance < leastDistance)) continue;
            leastDistance = distance;
            moveToNode = foundNodes[i];
            _direction = foundNodesDirection[i];
        }
        
        return moveToNode;
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


    private bool OverShotTarget()
    {
        var nodeToTarget = LengthFromNode(_targetNode.transform.position);
        var nodeToSelf = LengthFromNode(transform.localPosition);

        return nodeToSelf > nodeToTarget;
    }

    private static float GetDistance(Vector2 posA, Vector2 posB)
    {
        var dx = posA.x - posB.x;
        var dy = posA.y - posB.y;

        var distance = Mathf.Sqrt(dx * dx + dy * dy);
        return distance;
    }
    
}
