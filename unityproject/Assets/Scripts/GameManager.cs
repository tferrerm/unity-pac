using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public LevelManager levelManager;
    public Score score;
    public LivesManager livesManager;
    public ModeManager modeManager;
    public BonusManager bonusManager;
    
    public Player player;

    private const float WaitingTimeAfterReset = 2f;

    public TMP_Text centerText;
    public GameObject pauseMenu;
    private SoundManager soundManager;

    // Start is called before the first frame update
    void Start()
    {
        soundManager = GetComponent<SoundManager>();
        score.LivesManager = livesManager;
        
        IEnumerator coroutine = WaitForIntroMusic();
        StartCoroutine(coroutine);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = pauseMenu.activeSelf ? 1 : 0;
            pauseMenu.SetActive(!pauseMenu.activeSelf);
            AudioListener.pause = pauseMenu.activeSelf;
        }
    }

    public void PauseMenuResume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }
    
    public void GoToMainMenu()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public Vector3 GetNewEntityPosition(float movSpeed, Vector2 position, Direction currentDirection)
    {
        return levelManager.GetNewEntityPosition(movSpeed, position, currentDirection);
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
        player.HasCollidedWall = false;
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

    public Vector2Int GetEntityCurrentTileCoordinates(EntityId entityId, Direction currentDirection)
    {
        return levelManager.GetEntityCurrentTileCoordinates(entityId, currentDirection);
    }

    public void DecrementLives()
    {
        int remainingLives = livesManager.DecrementLives();

        if (remainingLives == 0)
        {
            GameOver(false);
        }
        else
        {
            DisappearAndReset();
        }
    }

    public void GameOver(bool wonGame)
    {
        StopGhosts();
        if (wonGame)
        {
            player.gameObject.SetActive(false);
            centerText.text = "ROUND OVER!";
            centerText.color = Color.green;
            centerText.gameObject.SetActive(true);
            
            soundManager.PlayOutro();
            IEnumerator coroutine = WaitForNextRound();
            StartCoroutine(coroutine);
        }
        else
        {
            score.SaveScore("PAC");
            IEnumerator coroutine = WaitForOutro();
            StartCoroutine(coroutine);
        }
    }

    private IEnumerator WaitForNextRound()
    {
        Time.timeScale = 0;
        
        player.CanReadInput = false;
        
        bonusManager.SetNextFruit();

        var time = soundManager.GetOutroWaitTime();
        yield return new WaitForSecondsRealtime(time);
        
        centerText.text = "READY!";
        centerText.color = Color.yellow;
        
        levelManager.InitializeEntitiesProperties();
        levelManager.ResetPellets();
        modeManager.OnResetToNextRound();
        player.gameObject.SetActive(true);
        
        yield return new WaitForSecondsRealtime(WaitingTimeAfterReset);
        
        centerText.gameObject.SetActive(false);
        soundManager.PlaySiren();
        player.CanReadInput = true;

        bonusManager.SetFruitWaiting();

        Time.timeScale = 1;
    }

    private IEnumerator WaitForOutro()
    {
        StopGhosts();
        soundManager.StopTileMapSound();
        
        var time = soundManager.GetDisappearingWaitTime();
        player.OnPauseGame();
        yield return new WaitForSeconds(time);
        
        player.gameObject.SetActive(false);
        
        centerText.text = "GAME OVER";
        centerText.color = Color.red;
        centerText.gameObject.SetActive(true);
        
        time = soundManager.GetOutroWaitTime();
        yield return new WaitForSecondsRealtime(time);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void DisappearAndReset()
    {
        IEnumerator coroutine = WaitForDisappearing();
        StartCoroutine(coroutine);
    }
    
    private IEnumerator WaitForDisappearing()
    {
        StopGhosts();
        soundManager.StopTileMapSound();
        var time = soundManager.GetDisappearingWaitTime();
        player.OnPauseGame();
        yield return new WaitForSeconds(time);
        player.OnResumeGame();
        ResetPositions();
        centerText.gameObject.SetActive(true);
        IEnumerator coroutine = WaitAfterResetEntities();
        StartCoroutine(coroutine);
    }

    private IEnumerator WaitAfterResetEntities()
    {
        player.CanReadInput = false;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(WaitingTimeAfterReset);
        centerText.gameObject.SetActive(false);
        soundManager.PlaySiren();
        player.CanReadInput = true;
        Time.timeScale = 1;
    }
    
    private IEnumerator WaitForGhostConsumption(Ghost ghost)
    {
        player.OnPauseGameWhenEating();
        modeManager.OnPauseGameWhenEaten(ghost, player.EatenGhosts);
        var time = soundManager.GetConsumptionWaitTime();
        soundManager.PlayEatingGhostSound();
        yield return new WaitForSeconds(time);
        EatGhost(ghost);
        player.OnResumeGameWhenEating();
        modeManager.OnResumeGameWhenEaten(ghost);
    }

    private IEnumerator WaitForIntroMusic()
    {
        player.CanReadInput = false;
        Time.timeScale = 0;
        var time = soundManager.GetIntroWaitTime();
        soundManager.PlayIntro();
        yield return new WaitForSecondsRealtime(time);
        centerText.gameObject.SetActive(false);
        soundManager.PlaySiren();
        player.CanReadInput = true;
        Time.timeScale = 1;
    }

    private void StopGhosts()
    {
        modeManager.OnPauseGame();
    }

    private void StartGhosts()
    {
        modeManager.OnResumeGame();
    }

    public void SetFrightenedMode()
    {
        soundManager.PlayFrightenedMode();
        modeManager.SetFrightenedMode();
    }

    public void StopFrightenedMode()
    {
        player.ResetEatenGhosts();
        soundManager.PlaySiren();
    }

    private void ResetPositions()
    {
        levelManager.InitializeEntitiesProperties();
        StartGhosts();
    }

    public void CollideGhost(Ghost ghost)
    {
        if (ghost.currentState == Ghost.GhostState.Consumed) return;
            
        if (modeManager.currentMode == ModeManager.Mode.Frightened && !ghost.hasBeenEaten)
        {
            player.IncrementEatenGhost();
            AddEatenGhostPoints(player.EatenGhosts);
            IEnumerator coroutine = WaitForGhostConsumption(ghost);
            StartCoroutine(coroutine);
            return;
        }
        player.PlayDisappearingAnimation();
        DecrementLives();
    }

    public void EatGhost(Ghost ghost)
    {
        ghost.Consume();
        if(modeManager.currentMode == ModeManager.Mode.Frightened)
            soundManager.PlayConsumedGhost();
    }

    public void AddEatenGhostPoints(int eatenGhosts)
    {
        score.AddEatenGhostPoints(eatenGhosts);
    }

    public void EatBonus(GameObject bonus)
    {
        score.AddFruitBonusPoints();
        
        bonusManager.EatBonus(bonus);
    }

    public void EatPellet(GameObject pelletGO, bool isPowerPellet)
    {
        if (isPowerPellet)
        {
            score.AddPowerPelletPoints();
            SetFrightenedMode();
        }
        else
        {
            score.AddPelletPoints();
        }
        
        pelletGO.SetActive(false);
        soundManager.PlayWakaWakaSound();
    }

    public void PlayFrightenedModeMelody()
    {
        soundManager.PlayFrightenedMode();
    }
}
