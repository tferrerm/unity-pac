using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour, IEntity, IPauseable
{
    public float movSpeed = 10f;
    private Direction? nextDirection;
    private bool hasCollidedWall;
    private readonly Dictionary<KeyCode, Direction> keyDirections = new Dictionary<KeyCode, Direction>();
    private readonly Dictionary<Direction, int> directionRotationAngles = new Dictionary<Direction, int>();
    private Animator _animator;

    private AudioSource _audioSource;
    public AudioClip disappearingSound;
    public AudioClip wakaWaka;
    public AudioClip eatingGhost; 

    private int _eatenGhosts = 0;

    public GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        keyDirections.Add(KeyCode.LeftArrow, Direction.Left);
        keyDirections.Add(KeyCode.RightArrow, Direction.Right);
        keyDirections.Add(KeyCode.UpArrow, Direction.Up);
        keyDirections.Add(KeyCode.DownArrow, Direction.Down);
        
        directionRotationAngles.Add(Direction.Right, 0);
        directionRotationAngles.Add(Direction.Up, 90);
        directionRotationAngles.Add(Direction.Left, 180);
        directionRotationAngles.Add(Direction.Down, 270);
        
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    private void Update()
    {
        MovePlayer();

        foreach (var keyValuePair in keyDirections.Where(keyValuePair => Input.GetKey(keyValuePair.Key)))
        {
            HandleInput(keyValuePair.Value);
        }
    }

    // Check if direction can be changed instantly
    private void HandleInput(Direction inputDirection)
    {
        gameManager.ValidateInputDirection(inputDirection, currentDirection, hasCollidedWall);
    }

    private void MovePlayer()
    {
        if (hasCollidedWall || _animator.GetBool("Disappear")) return;
        
        Vector3 newPosition = gameManager.GetNewEntityPosition(movSpeed, transform.position, currentDirection);
        transform.position = gameManager.GetValidatedPosition(EntityId.Player, newPosition, currentDirection, nextDirection);
        _animator.transform.rotation = Quaternion.Euler(new Vector3(0,0, directionRotationAngles[currentDirection]));
        _animator.speed = 1;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pellet"))
        {
            gameManager.AddPelletPoints();
            Destroy(other.gameObject);
            _audioSource.PlayOneShot(wakaWaka);
        } else if (other.CompareTag("PowerPellet"))
        {
            gameManager.AddPowerPelletPoints();
            Destroy(other.gameObject);
            _audioSource.PlayOneShot(wakaWaka);
            gameManager.SetFrightenedMode();
        } else if (other.CompareTag("Ghost"))
        {
            Ghost ghost = other.GetComponent<Ghost>();
            if (ghost.currentMode == Ghost.Mode.Consumed) return;
            
            if (ghost.currentMode == Ghost.Mode.Frightened)
            {
                // TODO: put ghost in consumed mode
                _eatenGhosts++;
                gameManager.AddEatenGhostPoints(_eatenGhosts);
                _audioSource.PlayOneShot(eatingGhost);
                gameManager.EatGhost(ghost.entityId);
                return;
            }
            _animator.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            _animator.SetBool("Disappear", true);
            gameManager.DecrementLives();
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

    public void AnimationPlayback()
    {
        if (hasCollidedWall || _animator.GetBool("Disappear"))
            _animator.speed = 0;
    }

    public Direction currentDirection { get; set; }
    public void OnPauseGame()
    {
        if (_animator.GetBool("Disappear"))
        {
            _animator.Play("Disappear");
            _animator.speed = 1;
            _audioSource.PlayOneShot(disappearingSound);
        }
    }

    public float GetDisappearingWaitTime()
    {
        return disappearingSound.length;
    }

    public void OnResumeGame()
    {
        _animator.SetBool("Disappear", false);
        _animator.Play("Pacman");

    }

    public void ResetEatenGhosts()
    {
        _eatenGhosts = 0;
    }
}
