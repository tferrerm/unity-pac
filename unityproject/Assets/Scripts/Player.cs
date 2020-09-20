using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour, IEntity, IPauseable
{
    private float movSpeed;
    public float normalSpeed = 30f;
    private Direction? nextDirection;
    private bool hasCollidedWall;
    private readonly Dictionary<KeyCode, Direction> keyDirections = new Dictionary<KeyCode, Direction>();
    private readonly Dictionary<Direction, int> directionRotationAngles = new Dictionary<Direction, int>();
    private bool canReadInput;
    private Direction? inputWhileFrozen;
    private Animator _animator;
    private int _animatorDisappearId;
    private SpriteRenderer _spriteRenderer;

    private int _eatenGhosts;

    public GameManager gameManager;
    private SoundManager soundManager;

    // Start is called before the first frame update
    private void Start()
    {
        _animatorDisappearId = Animator.StringToHash("Disappear");
        movSpeed = normalSpeed;
        
        keyDirections.Add(KeyCode.LeftArrow, Direction.Left);
        keyDirections.Add(KeyCode.RightArrow, Direction.Right);
        keyDirections.Add(KeyCode.UpArrow, Direction.Up);
        keyDirections.Add(KeyCode.DownArrow, Direction.Down);
        
        directionRotationAngles.Add(Direction.Right, 0);
        directionRotationAngles.Add(Direction.Up, 90);
        directionRotationAngles.Add(Direction.Left, 180);
        directionRotationAngles.Add(Direction.Down, 270);
        
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        soundManager = gameManager.GetComponent<SoundManager>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (inputWhileFrozen != null && canReadInput)
        {
            gameManager.ValidateInputDirection(inputWhileFrozen.GetValueOrDefault(), currentDirection, hasCollidedWall);
            inputWhileFrozen = null;
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
        if(canReadInput)
            gameManager.ValidateInputDirection(inputDirection, currentDirection, hasCollidedWall);
        else if(!gameManager.IsPauseMenuActive())
            inputWhileFrozen = inputDirection;
    }

    private void MovePlayer()
    {
        if (hasCollidedWall || _animator.GetBool(_animatorDisappearId)) return;
        
        Vector3 newPosition = gameManager.GetNewEntityPosition(movSpeed, transform.position, currentDirection);
        transform.position = gameManager.GetValidatedPosition(EntityId.Player, newPosition, currentDirection, nextDirection);
        _animator.transform.rotation = Quaternion.Euler(new Vector3(0,0, directionRotationAngles[currentDirection]));
        _animator.speed = 1;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pellet"))
        {
            gameManager.EatPellet(other.gameObject, false);
        } else if (other.CompareTag("PowerPellet"))
        {
            gameManager.EatPellet(other.gameObject, true);
        } else if (other.CompareTag("Ghost"))
        {
            Ghost ghost = other.GetComponent<Ghost>();
            gameManager.CollideGhost(ghost);
        } else if (other.CompareTag("FruitBonus"))
        {
            gameManager.EatBonus(other.gameObject);
        }
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

    public void AnimationPlayback() // Used from Unity in animation key frames
    {
        if (hasCollidedWall || _animator.GetBool(_animatorDisappearId))
            _animator.speed = 0;
    }

    public Direction currentDirection { get; set; }
    public void OnPauseGame()
    {
        if (_animator.GetBool(_animatorDisappearId))
        {
            _animator.Play("Disappear");
            _animator.speed = 1;
            soundManager.PlayDisappearingSound();
        }
    }

    public void OnResumeGame()
    {
        _animator.SetBool(_animatorDisappearId, false);
        _animator.Play("Pacman");
    }

    public void ResetEatenGhosts()
    {
        _eatenGhosts = 0;
    }

    public void IncrementEatenGhost()
    {
        _eatenGhosts++;
    }

    public void PlayDisappearingAnimation()
    {
        _animator.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        _animator.SetBool(_animatorDisappearId, true);
    }

    public void OnPauseGameWhenEating()
    {
        movSpeed = 0;
        _spriteRenderer.enabled = false;
    }
    
    public void OnResumeGameWhenEating()
    {
        _spriteRenderer.enabled = true;
        movSpeed = normalSpeed;
    }

    public int EatenGhosts => _eatenGhosts;

    public bool CanReadInput
    {
        get => canReadInput;
        set => canReadInput = value;
    }
}
