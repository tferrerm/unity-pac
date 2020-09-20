using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModeManager : MonoBehaviour, IPauseable
{
    public enum Mode
    {
        Chase = 0,
        Scatter = 1,
        Frightened = 2
    }
    
    /*
     * Durations
     */
    public int chaseModeDuration = 20;
    public int firstTwoScatterDuration = 7;
    public int lastTwoScatterDuration = 5;
    public int modeChangeIteration = 1;
    public float frightenedModeDuration = 10;
    public float startBlinkingAt = 7;
    
    /*
     * Timers
     */
    private float _modeChangeTimer;
    private float _frightenedModeTimer;
    private float _blinkTimer;
    private bool _frightenedModeIsWhite;
    
    /*
     * Modes
     */
    public Mode currentMode;
    private Mode _previousMode = Mode.Scatter;
    public Mode initialMode = Mode.Scatter;
    
    /*
     * Speeds
     */
    public float movSpeed = 25f;
    public float normalSpeed = 25f;
    public float frightenedModeSpeed = 15f;
    public float consumedStateSpeed = 40f;
    private float maxNormalSpeed;
    private float maxFrightenedModeSpeed;
    private float maxConsumedStateSpeed;
    private float roundSpeedMultiplier = 1.1f;
    private bool maxSpeedReached;
    private bool waitingForConsumption;

    public List<Ghost> ghosts;

    public GameManager gameManager;
    
    // Start is called before the first frame update
    void Start()
    {
        currentMode = initialMode;

        maxNormalSpeed = 2 * normalSpeed;
        maxFrightenedModeSpeed = 2 * frightenedModeSpeed;
        maxConsumedStateSpeed = 2 * consumedStateSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        ModeUpdate();
    }
    
    /*
     * This function determines whether a mode needs to be changed or not (in that case calls ChangeMode())
     * Ghosts iterate doing the scatter-chase combination. After every chase period, iteration number is
     * incremented.
     */
    private void ModeUpdate()
    {
        _modeChangeTimer += Time.deltaTime;

        if (currentMode != Mode.Frightened)
        {
            switch (modeChangeIteration)
            {
                case 1:
                case 2:
                {
                    /*
                     * Checking if it's time to switch from scatter to chase
                     */
                    if (currentMode == Mode.Scatter && _modeChangeTimer > firstTwoScatterDuration)
                    {
                        ghosts.ForEach(ghost => ghost.Reverse());
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }
                    /*
                     * Checking if it's time to switch from chase to scatter
                     */
                    if (currentMode == Mode.Chase && _modeChangeTimer > chaseModeDuration)
                    {
                        ghosts.ForEach(ghost => ghost.Reverse());
                        modeChangeIteration++;
                        ChangeMode(Mode.Scatter);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
                case 3:
                {
                    if (currentMode == Mode.Scatter && _modeChangeTimer > lastTwoScatterDuration)
                    {
                        ghosts.ForEach(ghost => ghost.Reverse());
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }
                    if (currentMode == Mode.Chase && _modeChangeTimer > chaseModeDuration)
                    {
                        ghosts.ForEach(ghost => ghost.Reverse());
                        modeChangeIteration = 4;
                        ChangeMode(Mode.Scatter);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
                case 4:
                {
                    /*
                     * If we're in chase mode in the last iteration we're  in chase mode forever
                     */
                    if (currentMode == Mode.Scatter && _modeChangeTimer > lastTwoScatterDuration)
                    {
                        ghosts.ForEach(ghost => ghost.Reverse());
                        ChangeMode(Mode.Chase);
                        _modeChangeTimer = 0;
                    }

                    break;
                }
            }
        }
        /*
         * Handling frightened mode. If less than 3 seconds are left, ghosts start blinking (switching between blue and white)
         */
        else if (currentMode == Mode.Frightened)
        {
            _frightenedModeTimer += Time.deltaTime;
            
            if (_frightenedModeTimer > startBlinkingAt)
            {
                ghosts.ForEach(ghost => ghost.SetFrightenedEndingAnimation());
            }

            if (_frightenedModeTimer > frightenedModeDuration)
            {
                ghosts.ForEach(ghost => ghost.SetStandardAnimation());
                _frightenedModeTimer = 0;
                gameManager.StopFrightenedMode();
                ChangeMode(_previousMode);
            }
        }
    }
    
    public void SetFrightenedMode()
    {
        ghosts.Where(ghost => ghost.currentState != Ghost.GhostState.Consumed).ToList().ForEach(ghost => ghost.SetFrightenedMode());
        _frightenedModeTimer = 0;
        ghosts.ForEach(ghost => ghost.Reverse());
        ChangeMode(Mode.Frightened);
    }
    
    private void ChangeMode(Mode m)
    {
        if (currentMode == Mode.Frightened)
        {
            movSpeed = normalSpeed;
        }

        if (m == Mode.Frightened)
        {
            movSpeed = frightenedModeSpeed;
        }

        if (m != currentMode)
        {
            _previousMode = currentMode;
            currentMode = m;
        }
    }
    
    // Called when ghost is eaten
    public void OnPauseGameWhenEaten(Ghost ghost, int ghostsEaten)
    {
        waitingForConsumption = true;
        movSpeed = 0;
        ghost.SetPointsAnimation(ghostsEaten);
    }
    
    public void OnResumeGameWhenEaten(Ghost ghost)
    {
        waitingForConsumption = false;
        movSpeed = frightenedModeSpeed;
    }

    // Called when player is disappearing
    public void OnPauseGame()
    {
        movSpeed = 0;
        ghosts.ForEach(ghost => ghost.gameObject.SetActive(false));
    }
    
    // Called after player has disappeared
    public void OnResumeGame()
    {
        ghosts.ForEach(ghost => ghost.Reset());
        movSpeed = normalSpeed;
        ghosts.ForEach(ghost => ghost.gameObject.SetActive(true));
        if(currentMode == Mode.Frightened)
            ChangeMode(_previousMode);
    }

    public void OnResetToNextRound()
    {
        currentMode = initialMode;
        _modeChangeTimer = 0;

        if (!maxSpeedReached)
        {
            var newNormalSpeed = normalSpeed * roundSpeedMultiplier;
            if (newNormalSpeed <= maxNormalSpeed)
            {
                normalSpeed = newNormalSpeed;
                frightenedModeSpeed *= roundSpeedMultiplier;
                consumedStateSpeed *= roundSpeedMultiplier;
            }
            else
            {
                maxSpeedReached = true;
                normalSpeed = maxNormalSpeed;
                frightenedModeSpeed = maxFrightenedModeSpeed;
                consumedStateSpeed = maxConsumedStateSpeed;
            }
        }

        ghosts.ForEach(ghost => ghost.Reset());
        movSpeed = normalSpeed;
        ghosts.ForEach(ghost => ghost.gameObject.SetActive(true));
    }
    
    public bool WaitingForConsumption => waitingForConsumption;
}
