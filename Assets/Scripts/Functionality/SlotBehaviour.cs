using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotBehaviour : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> images;     //class to store total images
    [SerializeField] private List<SlotImage> Tempimages;     //class to store the result matrix

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] Slot_Transform;

    [Header("Buttons")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button MaxBet_Button;
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button BetOne_button;
    [SerializeField] private Button Turbo_Button;
    [SerializeField] private Button StopSpin_Button;

    [Header("Animated Sprites")]
    [SerializeField] private Sprite[] Monkey_Sprite;
    [SerializeField] private Sprite[] Banana_Sprite;
    [SerializeField] private Sprite[] Uncle_Sprite;
    [SerializeField] private Sprite[] Bird_Sprite;
    [SerializeField] private Sprite[] Lion_Sprite;
    [SerializeField] private Sprite[] Rhino_Sprite;
    [SerializeField] private Sprite[] Crocodile_Sprite;
    [SerializeField] private Sprite[] Coconut_Sprite;
    [SerializeField] private Sprite[] Bonus_Sprite;
    [SerializeField] private Sprite[] Wild_Sprite;
    [SerializeField] private Sprite TurboToggleSprite;

    [Header("Miscellaneous UI")]
    [SerializeField] private TMP_Text Balance_text;
    [SerializeField] private TMP_Text TotalBet_text;
    [SerializeField] private TMP_Text LineBet_text;
    [SerializeField] private TMP_Text TotalWin_text;

    [Header("Audio Management")]
    [SerializeField] private AudioController audioController;
    [SerializeField] private PayoutCalculation PayCalculator;
    [SerializeField] private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 
    [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing
    [SerializeField] int verticalVisibility = 3;
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BonusController _bonusManager;
    [SerializeField] private GambleController gambleController;
    private List<Tweener> alltweens = new List<Tweener>();
    private Tweener WinTween = null;
    int tweenHeight = 0;  //calculate the height at which tweening is done
    private float SpinDelay=0.2f;
    private int numberOfSlots = 5;          //number of columns
    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine;
    Coroutine FreeSpinRoutine = null;
    bool IsFreeSpin = false;
    bool IsAutoSpin = false;
    bool IsSpinning = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;
    private int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    internal double currentBet = 0;
    internal bool WasAutoSpinON;
    private bool StopSpinToggle;
    protected int Lines = 9;
    private bool IsTurboOn;

    private void Start()
    {
        IsAutoSpin = false;
        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

        if (BetOne_button) BetOne_button.onClick.RemoveAllListeners();
        if (BetOne_button) BetOne_button.onClick.AddListener(ChangeBet);

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        if(Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if(Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

        if(StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
        if(StopSpin_Button) StopSpin_Button.onClick.AddListener(()=> {StopSpinToggle=true; StopSpin_Button.gameObject.SetActive(false);});
        tweenHeight = (15 * IconSizeFactor) - 280;
    }

    internal void AutoSpin()
    {
        if (!IsAutoSpin)
        {
            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
        }
    }

    void TurboToggle(){
        if(IsTurboOn){
            IsTurboOn=false;
            Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
            Turbo_Button.image.sprite=TurboToggleSprite;
        }
        else{
            IsTurboOn=true;
            Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
        }
    }

    internal void GambleCollect()
    {
        SocketManager.GambleCollectCall();
    }

    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {

            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));

        }
    }

    internal void SetInitialUI()
    {
        BetCounter = 0;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f3");
        if (TotalWin_text) TotalWin_text.text = "0.000";
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        uiManager.InitialiseUIData(SocketManager.initUIData.AbtLogo.link, SocketManager.initUIData.AbtLogo.logoSprite, SocketManager.initUIData.ToULink, SocketManager.initUIData.PopLink, SocketManager.initUIData.paylines);
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        {
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }

    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(SpinDelay);
        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        while (i < spinchances)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            i++;
        }
        ToggleButtonGrp(true);
        IsFreeSpin = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }

    private void MaxBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        BetCounter = SocketManager.initialData.Bets.Count - 1;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
    }

    private void ChangeBet()
    {
        if (audioController) audioController.PlayButtonAudio();
        if (BetCounter < SocketManager.initialData.Bets.Count - 1)
        {
            BetCounter++;
        }
        else
        {
            BetCounter = 0;
        }
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
    }

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
        }
    }


    //just for testing purposes delete on production
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && SlotStart_Button.interactable)
    //    {
    //        StartSlots();
    //    }
    //}

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            case 0:
                for (int i = 0; i < Monkey_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Monkey_Sprite[i]);
                }
                break;
            case 1:
                for (int i = 0; i < Banana_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Banana_Sprite[i]);
                }
                break;
            case 2:
                for (int i = 0; i < Uncle_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Uncle_Sprite[i]);
                }
                break;
            case 3:
                for (int i = 0; i < Bird_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Bird_Sprite[i]);
                }
                break;
            case 4:
                for (int i = 0; i < Lion_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Lion_Sprite[i]);
                }
                break;
            case 5:
                for (int i = 0; i < Rhino_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Rhino_Sprite[i]);
                }
                break;
            case 6:
                for (int i = 0; i < Crocodile_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Crocodile_Sprite[i]);
                }
                break;

            case 7:
                for (int i = 0; i < Coconut_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Coconut_Sprite[i]);  
                }
                break;
            case 8:
                for (int i = 0; i < Bonus_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Bonus_Sprite[i]);
                }
                break;
            case 9:
                for (int i = 0; i < Wild_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Wild_Sprite[i]);
                }
                break;
        }
    }
    //starts the spin process
    private void StartSlots(bool autoSpin = false)
    {
        if (audioController) audioController.PlaySpinButtonAudio();
        if (gambleController) gambleController.toggleDoubleButton(false);
        if (gambleController) gambleController.GambleTweeningAnim(false);
        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }

        }
        PayCalculator.DontDestroyLines.Clear();
        WinningsAnim(false);
        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }
        PayCalculator.ResetStaticLine();
        tweenroutine = StartCoroutine(TweenRoutine());
    }

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }


    private IEnumerator TweenRoutine()
    {
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }
        if (audioController) audioController.PlayWLAudio("spin");
        CheckSpinAudio = true;
        IsSpinning = true;
        ToggleButtonGrp(false);
        if(!IsTurboOn && !IsFreeSpin && !IsAutoSpin){
            StopSpin_Button.gameObject.SetActive(true);
        }
        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }
        SocketManager.AccumulateResult(BetCounter);
        yield return new WaitUntil(() => SocketManager.isResultdone);
        yield return new WaitForSeconds(0.9f);

        for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++)
        {
            List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
            for (int i = 0; i < 5; i++)
            {
                if (images[i].slotImages[images[i].slotImages.Count - 5 + j]) images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = myImages[resultnum[i]];
                PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
            }
        }
        if(IsTurboOn){
            yield return new WaitForSeconds(0.1f);
        }
        else{
            for(int i=0;i<10;i++)
            {
                yield return new WaitForSeconds(0.1f);
                if(StopSpinToggle){
                    break;
                }
            }
            StopSpin_Button.gameObject.SetActive(false);
        }

        for (int i = 0; i < numberOfSlots; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i, StopSpinToggle);
        }
        StopSpinToggle=false;
        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();

        if(SocketManager.playerdata.currentWining>0){
            SpinDelay=1.2f;
        }
        else{
            SpinDelay=0.2f;
        }

        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot);

        CheckPopups = true;

        if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString("f3");

        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f3");

        currentBalance = SocketManager.playerdata.Balance;

        currentBet = SocketManager.initialData.Bets[BetCounter];

        yield return new WaitForSeconds(0.5f);
        CheckBonusGame();

        print("checkpopups, " + CheckPopups);
        yield return new WaitUntil(() => !CheckPopups);
        if (!IsAutoSpin)
        {
            ActivateGamble();
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            ActivateGamble();
            // yield return new WaitForSeconds(0.1f);
            IsSpinning = false;
        }
    }

    private void ActivateGamble()
    {
        if (SocketManager.playerdata.currentWining > 0 && SocketManager.playerdata.currentWining <= SocketManager.GambleLimit)
        {
            gambleController.GambleTweeningAnim(true);
            gambleController.toggleDoubleButton(true);
        }
    }

    internal void DeactivateGamble()
    {
        if(IsAutoSpin){
            WasAutoSpinON=true;
        }
        StopAutoSpin();
    }


    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f3");
        });
    }

    private void WinningsAnim(bool IsStart)
    {
        if (IsStart)
        {
            WinTween = TotalWin_text.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
        }
        else
        {
            WinTween.Kill();
            TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }

    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }

    internal void CheckBonusGame()
    {
        if (SocketManager.resultData.isBonus)
        {
            _bonusManager.GetBailCaseList(SocketManager.resultData.BonusResult);
        }
        else
        {
            CheckPopups = false;
        }
    }

    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (BetOne_button) BetOne_button.interactable = toggle;
    }

    internal void updateBalance()
    {
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f3");
        if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString("f3");
    }

    //start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        temp.StartAnimation();
        TempList.Add(temp);
    }

    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            TempList[i].StopAnimation();
        }
        TempList.Clear();
        TempList.TrimExcess();
    }

    
    private void CheckPayoutLineBackend(List<int> LineId, List<string> points_AnimString, double jackpot = 0)
    {
        List<int> points_anim = null;
        if (LineId.Count > 0 || points_AnimString.Count > 0) 
        {
            if (audioController) audioController.PlayWLAudio("win");


            for (int i = 0; i < LineId.Count; i++)
            {
                PayCalculator.DontDestroyLines.Add(LineId[i]);
                PayCalculator.GeneratePayoutLinesBackend(LineId[i]);
            }

            if (jackpot > 0)
            {
                for (int i = 0; i < Tempimages.Count; i++)
                {
                    for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
                    {
                        StartGameAnimation(Tempimages[i].slotImages[k].gameObject);
                    }
                }
            }
            else
            {
                for (int i = 0; i < points_AnimString.Count; i++)
                {
                    points_anim = points_AnimString[i]?.Split(',')?.Select(Int32.Parse)?.ToList();

                    for (int k = 0; k < points_anim.Count; k++)
                    {
                        if (points_anim[k] >= 10)
                        {
                            StartGameAnimation(Tempimages[(points_anim[k] / 10) % 10].slotImages[points_anim[k] % 10].gameObject);
                        }
                        else
                        {
                            StartGameAnimation(Tempimages[0].slotImages[points_anim[k]].gameObject);
                        }
                    }
                }
            }
            WinningsAnim(true);
        }
        else
        {

            if (audioController) audioController.StopWLAaudio();
        }
        CheckSpinAudio = false;

    }

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }



    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool isStop=false)
    {
        alltweens[index].Pause();
        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 0.5f).SetEase(Ease.OutElastic);

        if(!isStop){
            yield return new WaitForSeconds(0.2f);  
        }
        else{
            yield return null;
        }
    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

