using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public enum Direction  
{  
    Up = 0, Down = 1, Left = 2, Right = 3,
};

public class Player : MonoBehaviour, IEntity
{
    public float playerAnimationDelta = 0.075f;
    private float _spriteAnimationAcum;
    private int _spriteVariantIndex = 0;
    public float movSpeed = 10f;
    private float _horizontalScreenMarginLimit;
    private float _verticalScreenMarginLimit;
    private SpriteRenderer _spriteRenderer;
    public Sprite[] upSprites;
    public Sprite[] downSprites;
    public Sprite[] rightSprites;
    public Sprite[] leftSprites;
    private int _spritesPerDirection;
    private int _nextSpriteVariant = -1;
    public Sprite[][] playerSprites;
    public Sprite[] fadingPlayerSprites;
    //private Direction currentDirection = Direction.Right;
    private Direction? nextDirection;
    private bool hasCollidedWall;
    private Dictionary<KeyCode, Direction> keyDirections;

    public GameManager gameManager;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteAnimationAcum = 0;
        _spritesPerDirection = 3;
        playerSprites = new Sprite[Enum.GetNames(typeof(Direction)).Length][];
        playerSprites[(int) Direction.Up] = upSprites;
        playerSprites[(int) Direction.Right] = rightSprites;
        playerSprites[(int) Direction.Down] = downSprites;
        playerSprites[(int) Direction.Left] = leftSprites;
    }
    
    // Start is called before the first frame update
    private void Start()
    {
        // >> 1 divides by 2, 0 is the center
        _horizontalScreenMarginLimit = (Screen.width >> 1);
        _verticalScreenMarginLimit = (Screen.height >> 1);
        
        if(currentDirection != null) // TODO FIX
            _spriteRenderer.sprite = playerSprites[(int) currentDirection][_spriteVariantIndex];
        
        keyDirections = new Dictionary<KeyCode, Direction>();
        keyDirections.Add(KeyCode.LeftArrow, Direction.Left);
        keyDirections.Add(KeyCode.RightArrow, Direction.Right);
        keyDirections.Add(KeyCode.UpArrow, Direction.Up);
        keyDirections.Add(KeyCode.DownArrow, Direction.Down);
    }

    // Update is called once per frame
    private void Update()
    {
        _spriteAnimationAcum += Time.deltaTime;
        if (_spriteAnimationAcum > playerAnimationDelta)
        {
            if (_spriteVariantIndex % (_spritesPerDirection - 1) == 0) // animate from full to open or open to full
                _nextSpriteVariant *= -1;
            _spriteVariantIndex += _nextSpriteVariant; // next sprite for direction
            _spriteRenderer.sprite = playerSprites[(int) currentDirection][_spriteVariantIndex];
            _spriteAnimationAcum -= playerAnimationDelta;
        }
        
        MovePlayer();

        foreach (var keyValuePair in keyDirections.Where(keyValuePair => Input.GetKey(keyValuePair.Key)))
        {
            HandleInput(keyValuePair.Value);
        }
    }

    // Check if direction can be changed instantly
    private void HandleInput(Direction inputDirection)
    {
        if (!gameManager.ValidateDirection(EntityId.Player, inputDirection, currentDirection))
        {
            nextDirection = inputDirection;
            hasCollidedWall = false;
        }
    }

    private void MovePlayer()
    {
        if (hasCollidedWall) return;
        
        Vector3 newPosition;
        switch (currentDirection)
        {
            case Direction.Left:
                newPosition = new Vector3(Mathf.Max(transform.position.x - movSpeed * Time.deltaTime, -_horizontalScreenMarginLimit), transform.position.y, 0);
                break;
            case Direction.Right:
                newPosition = new Vector3(Mathf.Min(transform.position.x + movSpeed * Time.deltaTime, _horizontalScreenMarginLimit), transform.position.y, 0);
                break;
            case Direction.Up:
                newPosition = new Vector3(transform.position.x, Mathf.Min(transform.position.y + movSpeed * Time.deltaTime, _verticalScreenMarginLimit), 0);
                break;
            case Direction.Down:
                newPosition = new Vector3(transform.position.x, Mathf.Max(transform.position.y - movSpeed * Time.deltaTime, -_verticalScreenMarginLimit), 0);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        transform.position = gameManager.GetValidMovement(EntityId.Player, newPosition, currentDirection, nextDirection);
    }

    public Direction CurrentDirection
    {
        get => currentDirection;
        set => currentDirection = value;
    }

    public Direction? NextDirection
    {
        get => nextDirection;
        set => nextDirection = value;
    }

    public bool HasCollidedWall
    {
        get => hasCollidedWall;
        set => hasCollidedWall = value;
    }

    public Direction currentDirection { get; set; }
}
