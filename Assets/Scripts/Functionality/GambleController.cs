using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GambleController : MonoBehaviour
{
    [SerializeField]
    private GameObject gamble_game;
    [SerializeField]
    private Button doubleButton;
    [SerializeField]
    private SocketIOManager socketManager;
    [SerializeField]
    private AudioController audioController;
    [SerializeField]
    internal List<CardFlip> allcards = new List<CardFlip>();
    [SerializeField]
    private TMP_Text winamount;
    [SerializeField]
    private SlotBehaviour slotController;
    [SerializeField]
    private Sprite[] HeartSpriteList;
    [SerializeField]
    private Sprite[] ClubSpriteList;
    [SerializeField]
    private Sprite[] SpadeSpriteList;
    [SerializeField]
    private Sprite[] DiamondSpriteList;
    [SerializeField]
    private Sprite cardCover;
    [SerializeField]
    private CardFlip DealerCard_Script;

    [SerializeField]
    private GameObject loadingScreen;
    [SerializeField]
    private GameObject GambleEnd_Object;
    [SerializeField]
    private Button DoubleEnd_Button;
    [SerializeField]
    private Button CollectEnd_Button;
    [SerializeField]
    private Image slider;

    private Sprite highcard_Sprite;
    private Sprite lowcard_Sprite;
    private Sprite spare1card_Sprite;
    private Sprite spare2card_Sprite;

    private Tweener Gamble_Tween_Scale = null;
    private Tweener Gamble_Tween_Move = null;

    private Vector3 m_Temp_GambleButton;

    internal bool gambleStart = false;
    internal bool isResult = false;

    private void Start()
    {
        if (GambleEnd_Object) GambleEnd_Object.SetActive(false);
        if (doubleButton) doubleButton.onClick.RemoveAllListeners();
        if (doubleButton) doubleButton.onClick.AddListener(delegate { StartGamblegame(); });
        if (DoubleEnd_Button) DoubleEnd_Button.onClick.RemoveAllListeners();
        if (DoubleEnd_Button) DoubleEnd_Button.onClick.AddListener(delegate { NormalCollectFunction(); StartGamblegame(true); });
        if (CollectEnd_Button) CollectEnd_Button.onClick.RemoveAllListeners();
        if (CollectEnd_Button) CollectEnd_Button.onClick.AddListener(OnReset);
        toggleDoubleButton(false);
    }

    internal void toggleDoubleButton(bool toggle)
    {
        doubleButton.interactable = toggle;
    }

    private void OnReset()
    {
        if (slotController) slotController.GambleCollect();
        NormalCollectFunction();
        if(slotController.WasAutoSpinON){
            slotController.WasAutoSpinON=false;
            slotController.AutoSpin();
        }
    }

    void StartGamblegame(bool isRepeat = false)
    {
        if (GambleEnd_Object) GambleEnd_Object.SetActive(false);
        GambleTweeningAnim(false);
        slotController.DeactivateGamble();
        if (!isRepeat)
        {
            winamount.text = "0";
        }
        if (audioController) audioController.PlayButtonAudio();
        if (gamble_game) gamble_game.SetActive(true);
        loadingScreen.SetActive(true);
        StartCoroutine(loadingRoutine());
        StartCoroutine(GambleCoroutine());
    }

    internal void GambleTweeningAnim(bool IsStart)
    {
        if (IsStart)
        {
            m_Temp_GambleButton = doubleButton.gameObject.transform.position;
            Gamble_Tween_Scale = doubleButton.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.18f, 1.18f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
            //Gamble_Tween_Move = doubleButton.gameObject.GetComponent<RectTransform>().DOMoveY(doubleButton.transform.position.y + .1f, 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
        }
        else
        {
            Gamble_Tween_Scale.Kill();
            //Gamble_Tween_Move.Kill();
            doubleButton.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }


    private void ComputeCards()
    {
        highcard_Sprite = CardSet(socketManager.myMessage.highCard.suit, socketManager.myMessage.highCard.value);
        lowcard_Sprite = CardSet(socketManager.myMessage.lowCard.suit, socketManager.myMessage.lowCard.value);
        spare1card_Sprite = CardSet(socketManager.myMessage.exCards[0].suit, socketManager.myMessage.exCards[0].value);
        spare2card_Sprite = CardSet(socketManager.myMessage.exCards[1].suit, socketManager.myMessage.exCards[1].value);
    }

    private Sprite CardSet(string suit, string value)
    {
        Sprite tempSprite = null;
        if (suit.ToUpper() == "HEARTS")
        {
            if (value.ToUpper() == "A")
            {
                tempSprite = HeartSpriteList[0];
            }
            else if (value.ToUpper() == "K")
            {
                tempSprite = HeartSpriteList[12];
            }
            else if (value.ToUpper() == "Q")
            {
                tempSprite = HeartSpriteList[11];
            }
            else if (value.ToUpper() == "J")
            {
                tempSprite = HeartSpriteList[10];
            }
            else
            {
                int myval = int.Parse(value);
                tempSprite = HeartSpriteList[myval - 1];
            }
        }
        else if (suit.ToUpper() == "DIAMONDS")
        {
            if (value.ToUpper() == "A")
            {
                tempSprite = DiamondSpriteList[0];
            }
            else if (value.ToUpper() == "K")
            {
                tempSprite = DiamondSpriteList[12];
            }
            else if (value.ToUpper() == "Q")
            {
                tempSprite = DiamondSpriteList[11];
            }
            else if (value.ToUpper() == "J")
            {
                tempSprite = DiamondSpriteList[10];
            }
            else
            {
                int myval = int.Parse(value);
                tempSprite = DiamondSpriteList[myval - 1];
            }
        }
        else if (suit.ToUpper() == "CLUBS")
        {
            if (value.ToUpper() == "A")
            {
                tempSprite = ClubSpriteList[0];
            }
            else if (value.ToUpper() == "K")
            {
                tempSprite = ClubSpriteList[12];
            }
            else if (value.ToUpper() == "Q")
            {
                tempSprite = ClubSpriteList[11];
            }
            else if (value.ToUpper() == "J")
            {
                tempSprite = ClubSpriteList[10];
            }
            else
            {
                int myval = int.Parse(value);
                tempSprite = ClubSpriteList[myval - 1];
            }
        }
        else if (suit.ToUpper() == "SPADES")
        {
            if (value.ToUpper() == "A")
            {
                tempSprite = SpadeSpriteList[0];
            }
            else if (value.ToUpper() == "K")
            {
                tempSprite = SpadeSpriteList[12];
            }
            else if (value.ToUpper() == "Q")
            {
                tempSprite = SpadeSpriteList[11];
            }
            else if (value.ToUpper() == "J")
            {
                tempSprite = SpadeSpriteList[10];
            }
            else
            {
                int myval = int.Parse(value);
                tempSprite = SpadeSpriteList[myval - 1];
            }
        }
        else
        {
            Debug.LogError("Bad Value");
        }
        return tempSprite;
    }

    IEnumerator GambleCoroutine()
    {
        for (int i = 0; i < allcards.Count; i++)
        {
            allcards[i].once = false;
        }

        socketManager.OnGamble();

        yield return new WaitUntil(() => socketManager.isResultdone);
        ComputeCards();
        gambleStart = true;
    }

    internal Sprite GetCard()
    {
        if (socketManager.myMessage.playerWon)
        {
            if (DealerCard_Script) DealerCard_Script.cardImage = lowcard_Sprite;
            return highcard_Sprite;
        }
        else
        {
            if (DealerCard_Script) DealerCard_Script.cardImage = highcard_Sprite;
            return lowcard_Sprite;
        }
    }

    internal void RunOnCollect()
    {
        StartCoroutine(NewCollectRoutine());
    }

    private IEnumerator NewCollectRoutine()
    {
        isResult = false;
        socketManager.OnCollect();
        yield return new WaitUntil(() => socketManager.isResultdone);
        isResult = true;
    }

    internal void FlipAllCard()
    {
        int cardVal = 0;
        for (int i = 0; i < allcards.Count; i++)
        {
            if (allcards[i].once)
            {
                continue;
            }
            else
            {
                allcards[i].Card_Button.interactable = false;
                if (cardVal == 0)
                {
                    allcards[i].cardImage = spare1card_Sprite;
                    cardVal++;
                }
                else
                {
                    allcards[i].cardImage = spare2card_Sprite;
                }
                allcards[i].FlipMyObject();
                allcards[i].Card_Button.interactable = false;
            }
        }
        if (DealerCard_Script) DealerCard_Script.FlipMyObject();
        if (socketManager.myMessage.playerWon)
        {
            winamount.text = "YOU WIN" + "\n" + socketManager.myMessage.currentWining.ToString();
            if (GambleEnd_Object) GambleEnd_Object.SetActive(true);
        }
        else
        {
            winamount.text = "YOU LOSE" + "\n" + "0";
            StartCoroutine(Collectroutine());
        }

    }


    IEnumerator Collectroutine()
    {
        yield return new WaitForSeconds(2f);
        gambleStart = false;
        yield return new WaitForSeconds(1f);
        slotController.updateBalance();
        if (gamble_game) gamble_game.SetActive(false);
        if(slotController.WasAutoSpinON){
            slotController.WasAutoSpinON=false;
            slotController.AutoSpin();
        }
        allcards.ForEach((element) =>
        {
            element.Card_Button.image.sprite = cardCover;
            element.Reset();
        });
        DealerCard_Script.Card_Button.image.sprite = cardCover;
        DealerCard_Script.once = false;
        toggleDoubleButton(false);

    }

    private void NormalCollectFunction()
    {
        gambleStart = false;
        slotController.updateBalance();
        if (gamble_game) gamble_game.SetActive(false);
        allcards.ForEach((element) =>
        {
            element.Card_Button.image.sprite = cardCover;
            element.Reset();

        });
        DealerCard_Script.Card_Button.image.sprite = cardCover;
        DealerCard_Script.once = false;
        toggleDoubleButton(false);
    }

    void OnGameOver()
    {
        StartCoroutine(Collectroutine());
    }

    IEnumerator loadingRoutine()
    {
        float fillAmount = 0;
        while (fillAmount < 0.9)
        {
            fillAmount += Time.deltaTime;
            slider.fillAmount = fillAmount;
            if (fillAmount == 0.9) yield break;
            yield return null;
        }
        yield return new WaitUntil(() => gambleStart);
        slider.fillAmount = 1;
        yield return new WaitForSeconds(1f);
        loadingScreen.SetActive(false);
    }
}
