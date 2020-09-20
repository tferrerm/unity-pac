using System.Collections;
using UnityEngine;

public class BonusManager : MonoBehaviour
{
    public int fruitAppearanceTime = 50;
    public int fruitDuration = 20;
    
    public GameObject fruitBonus;
    
    public Sprite[] fruitBonusSprites;
    public Sprite bonusFruitScoreSprite;
    private SpriteRenderer _spriteRenderer;
    
    private int _fruitIndex;
    private float _fruitTimer;
    private FruitStatus _fruitStatus = FruitStatus.Waiting;

    public int FruitScoreDisplayTime = 2;

    public SoundManager soundManager;

    private enum FruitStatus
    {
        Waiting = 0,
        Present = 1,
        Appeared = 2
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = fruitBonus.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_fruitStatus == FruitStatus.Waiting)
        {
            _fruitTimer += Time.deltaTime;
            if (_fruitTimer > fruitAppearanceTime)
            {
                fruitBonus.SetActive(true);
                _fruitStatus = FruitStatus.Present;
                _fruitTimer = 0;
            }
        }
        else if (_fruitStatus == FruitStatus.Present)
        {
            _fruitTimer += Time.deltaTime;
            if (_fruitTimer > fruitDuration)
            {
                fruitBonus.SetActive(false);
                _fruitTimer = 0;
            }
        }
    }

    public void SetNextFruit()
    {
        fruitBonus.SetActive(false);
        _fruitStatus = FruitStatus.Appeared;
        _fruitTimer = 0;
        _fruitIndex = (_fruitIndex + 1) % fruitBonusSprites.Length;
        _spriteRenderer.sprite = fruitBonusSprites[_fruitIndex];
    }

    public void SetFruitWaiting()
    {
        _fruitStatus = FruitStatus.Waiting;
    }
    
    public void EatBonus(GameObject bonus)
    {
        IEnumerator coroutine = EatBonusFruitSprite(bonus);
        StartCoroutine(coroutine);
    }
    
    private IEnumerator EatBonusFruitSprite(GameObject bonusFruit)
    {
        var spriteRenderer = bonusFruit.GetComponent<SpriteRenderer>();
        var fruitCollider = bonusFruit.GetComponent<Collider2D>();
        fruitCollider.enabled = false;
        soundManager.PlayConsumedFruit();
        spriteRenderer.sprite = bonusFruitScoreSprite;
        yield return new WaitForSecondsRealtime(FruitScoreDisplayTime);
        bonusFruit.SetActive(false);
        fruitCollider.enabled = true;
    }
}
