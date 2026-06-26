using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.SceneManagement; //场景命名空间
using UnityEngine.UI;
using UnityEngine.EventSystems;
using zengyanqiHelper;
using zengyanqiCard;
using zengyanqiCommon;

public class MainManager : MonoBehaviour
{
    /**
    *  字典中的数据结构：
    *  
    *  dictSingle  字符串 i + "|" + Card.CardType  ==>  sprite 
    *  dictWhole   index  ==>  Card(Number,Type,tagType,userType) 
    *  
    */

    #region 变量定义
    //单例模式
    public static MainManager _instance;
    //public static MainManager Instance { get { return _instance; } }

    [HideInInspector]
    public int currentActiveCardKey = -1; //当前活跃的牌（用户/机器人打出的牌）
    [HideInInspector]
    public bool isGetLast = false;        //是否抓了最后一张牌，开杠的时候用的，用于处理显示
    [HideInInspector]
    public int currentUserGrabkey = -1;   //当前用户抓到的牌
    [HideInInspector]
    public int currentRobotGrabkey = -1;  //当前机器人抓到的牌

    public bool openLog = false;          //是否打开日志

    public int globalFrameRate = 1;       //帧 (每秒钟更新多少个画面)

    public AudioClip audioClip;

    //牌
    public GameObject reverseParent;   //牌库

    public GameObject userParent;      //用户

    public GameObject userOutParent;   //当前用户打出的

    public GameObject robotParent;     //机器人的

    public GameObject robotOutParent;  //机器人打出的

    //按钮
    public GameObject btnWin;    //胡
    public GameObject btnGang;   //公杠
    public GameObject btnAnGang; //暗杠
    public GameObject btnKan;    //碰
    public GameObject btnChi;    //吃
    public GameObject btnPass;   //过

    //UI
    public GameObject canvasMain;
    public GameObject canvasChi;

    public GameObject deskBg;       //麻将桌

    public GameObject deskSide1;    //麻将桌上东边用户
    public GameObject deskSide2;    //麻将桌上南边用户
    public GameObject deskSide3;    //麻将桌上西边用户
    public GameObject deskSide4;    //麻将桌上北边用户

    public GameObject gameEnd;    //结束结算页面

    public Sprite spMjBg;         //默认牌的背面

    public Sprite spWinMjBg;      //赢了的牌的背面

    public Sprite spHuMjBg;       //胡牌的背面

    public Sprite spAngangMjBg;   //暗杠牌的背面

    public Sprite[] sp;

    public GameObject mjBase;        //牌面上有值

    public GameObject mjHiddenBase;  //牌面上没值

    public GameObject mjNoScript;    //不带脚本的牌

    public GameObject txtInfo;

    public GameObject txtInfoUserMj;

    //数据
    /**
    *  字典中的数据结构：
    *  
    *  dictSingle    字符串 i + "|" + Card.CardType  ==>  sprite 
    *  dictWhole     index    ==>  Card(Number,Type,tagType,userType) 
    *  currentUserMj index    ==>  gameObject 
    */

    private Dictionary<string, Sprite> dictSingle = new Dictionary<string, Sprite>();        //所有牌型（单个）

    public Dictionary<int, Card> dictWhole = new Dictionary<int, Card>();                    //整副牌

    private Dictionary<int, GameObject> currentUserMj = new Dictionary<int, GameObject>();   //当前用户手上的牌

    private Dictionary<int, GameObject> currentUserOutMj = new Dictionary<int, GameObject>();//当前用户打出的牌

    private Dictionary<int, Dictionary<int, GameObject>> robotList = new Dictionary<int, Dictionary<int, GameObject>>();  //机器人手上的牌

    private Dictionary<int, Dictionary<int, GameObject>> robotOutList = new Dictionary<int, Dictionary<int, GameObject>>();  //机器人打出的牌

    private Dictionary<int, Dictionary<int, bool>> dictShun = new Dictionary<int, Dictionary<int, bool>>();  //吃牌的顺子

    private int randomDiceSide;                                                              //掷骰子后得到的骰子方位

    private int userDiceSide;                                                                //当前用户的方位（东）

    private int currentActivityDiceSide;                                                     //当前活跃用户的方位

    private bool userCanKnock = false;                                                       //当前用户是否可以出牌

    //延时
    private float invokeSeconds = 0.5f;

    #endregion

    #region 对象池辅助方法
    //创建牌（从对象池获取，避免 Instantiate）
    private GameObject CreateTile(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        return ObjectPool.Instance.Get(prefab, position, rotation, parent);
    }
    //销毁牌（归还到对象池，避免 Destroy）
    private void DestroyTile(GameObject tile)
    {
        if (tile != null)
            ObjectPool.Instance.Push(tile);
    }
    #endregion

    #region unity相关方法
    private void Awake()
    {
        _instance = this;

        Application.targetFrameRate = globalFrameRate;

        /*
         总结：
            1.当开发应用在移动端时，“Canvas Scaler”的“UI Scale Mode”为“Scale With Screen Size”，以便自适应移动端屏幕

            2.最好事先知道应用到移动端屏幕的分辨率，或屏幕比例，以对应合适设置“Canvas Scaler”的“Reference Resolution”

            3.当应用是横屏游戏时，把“Canvas Scaler”的“Match”改为“0”，以“Width”为基准缩放UI适应屏幕；当应用是竖屏游戏时，
              把“Canvas Scaler”的“Match”改为“1”，以“Height”为基准缩放UI适应屏幕

            4.当然“Canvas Scaler”还有其他设置，但是不是常用，这里不做介绍了，以上内容针对UI屏幕自适应就够用了
         */
        //Screen.SetResolution(1750, 900, false);
        //Screen.orientation = ScreenOrientation.LandscapeRight;

        if (!openLog)
        {
            txtInfo.SetActive(false);
            txtInfoUserMj.SetActive(false);
        }
        else
        {
            txtInfo.SetActive(true);
            txtInfoUserMj.SetActive(true);
        }

        //切换新背景音效
        if (DateTime.Now.Second % 2 == 0)
        {
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.Play();
        }

        //播放洗牌音效
        //StartCoroutine(playAudio(99, "xipai.mp3", AudioType.MPEG));
    }

    // Start is called before the first frame update
    void Start()
    {
        print("Start()");

        // 修复切换场景导致光照变暗的问题：
        //（Environment Lighting的Source从SkyBox改成Color）：Window -> Rendering -> Lighting - Environment Lighting - Source = Color

        Screen.orientation = ScreenOrientation.AutoRotation;//设置方向为自动(根据需要自动旋转屏幕朝向任何启用的方向。)
        Screen.autorotateToLandscapeRight = true;           //允许自动旋转到右横屏
        Screen.autorotateToLandscapeLeft = true;            //允许自动旋转到左横屏
        Screen.autorotateToPortrait = false;                //不允许自动旋转到纵向
        Screen.autorotateToPortraitUpsideDown = false;      //不允许自动旋转到纵向上下
        Screen.sleepTimeout = SleepTimeout.NeverSleep;      //睡眠时间为从不睡眠

        Init();
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion

    public void avatarStyle()
    {
        switch (currentActivityDiceSide)
        {
            case 1:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + 1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 4) as Sprite;
                break;
            case 2:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + 2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 4) as Sprite;
                break;
            case 3:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + 3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 4) as Sprite;
                break;
            case 4:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + 3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + 4) as Sprite;
                break;
        }

    }

    #region 初始化，结束及相关方法
    public void Init()
    {
        print("Init()");

        //清空对象池，防止旧场景残留对象
        ObjectPool.Instance.init();

        //初始化
        initMj();

        //玩家永远是代码意义上的东边 !!!
        userDiceSide = Convert.ToInt32(Side.East);

        //随机掷骰子  todo 骰子特效
        randomDiceSide = (new System.Random()).Next(1, 5);

        //谁最先出牌 庄家
        currentActivityDiceSide = randomDiceSide;
        avatarStyle();

        //仅仅是换个桌布（显示东南西北），以及换个庄家

        //桌子背景图
        string namePath = "Images/deskBg/desk_" + randomDiceSide; //注意： 不要文件后缀名
        (deskBg.GetComponent<SpriteRenderer>()).sprite = Resources.Load<Sprite>(namePath) as Sprite;

        int[] randomWhole = getRandom(13 * 4);

        //用户随机的x张牌
        displayUser(randomWhole.Skip(0).Take(13).ToArray());

        //其他三个用户抓牌 暂时其他三个是机器人
        for (int i = 1; i <= 3; i++)
        {
            int robotDiceSide = userDiceSide + i;
            displayRobot(randomWhole.Skip(13 * i).Take(13).ToArray(), robotDiceSide);

            robotOutList.Add(robotDiceSide, new Dictionary<int, GameObject>());
        }

        //剩余牌的展示
        displayReverseSide();

        if (randomDiceSide == Convert.ToInt32(Side.East))
        {
            //当前用户抓牌
            userGrabCard();
        }
        else
        {
            //机器人抓牌
            robotGrabCard();
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="side">方位，不同方位的音效放不同文件夹了</param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private void playResourceAudio(int side, string fileName)
    {
        string namePath = "Audios/style" + side + "/" + fileName;
        AudioClip clip = Resources.Load<AudioClip>(namePath);

        if (side == 1)
        {
            AudioSource audioSource = deskSide1.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        }
        else if (side == 2)
        {
            AudioSource audioSource = deskSide2.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        }
        else if (side == 3)
        {
            AudioSource audioSource = deskSide3.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        }
        else if (side == 4)
        {
            AudioSource audioSource = deskSide4.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    ///// <summary>
    ///// 播放音效
    ///// </summary>
    ///// <param name="side">方位，不同方位的音效放不同文件夹了</param>
    ///// <param name="fileName"></param>
    ///// <returns></returns>
    //IEnumerator playAudio(int side, string fileName, AudioType audioType = AudioType.WAV)
    //{
    //    //print(System.Environment.CurrentDirectory);
    //    //print(Application.dataPath);

    //    //制作音效的地址： https://ttsmaker.com/zh-cn 
    //    using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + System.Environment.CurrentDirectory + @"\Assets\Audios\" + "style" + side + @"\" + fileName, audioType))
    //    {
    //        yield return uwr.SendWebRequest();

    //        if (uwr.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError(uwr.error);
    //            yield break;
    //        }

    //        AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
    //        AudioSource audioSource = desk.GetComponent<AudioSource>();
    //        audioSource.clip = clip;
    //        audioSource.Play();
    //    }
    //}

    //生成
    private void initMj()
    {
        //生成一份麻将 每种麻将有4个  (9 * 3 + 4 + 3 ) * 4 = 136
        for (int i = 1; i <= 9; i++)
        {
            dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Tong), sp[i - 1]); ;          //筒   {(1,CardType.Tong), sprite}
        }
        for (int i = 1; i <= 9; i++)
        {
            dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Tiao), sp[i + 8]);            //条  
        }
        for (int i = 1; i <= 9; i++)
        {
            dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Wan), sp[i + 1 + 8 * 2]);     //万   
        }

        for (int i = 1; i <= 4; i++)
        {
            dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Feng), sp[i + 2 + 8 * 3]);    //东南西北
        }

        for (int i = 1; i <= 3; i++)
        {
            dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Zi), sp[i + 3 + 8 * 3 + 3]);  //中发白 
        }

        //随机打乱这136个麻将
        int[] randomArr = Enumerable.Range(0, 136).ToArray(); //从0开始，共136个数字

        System.Random rand = new System.Random();

        for (int i = randomArr.Length - 1; i >= 0; i--)
        {
            int j = rand.Next(0, 136);
            int temp = randomArr[i];
            randomArr[i] = randomArr[j];
            randomArr[j] = temp;
        }

        int index = 0;
        for (int times = 0; times < 4; times++)
        {
            foreach (KeyValuePair<string, Sprite> dict in dictSingle)
            {
                print("dictSingle 的 key=" + dict.Key);
                string[] tempStr = dict.Key.Split('|');
                log(randomArr[index] + "|", false);
                dictWhole.Add(randomArr[index], new Card(Convert.ToInt32(tempStr[0]), Convert.ToInt32(tempStr[1])));

                ++index;
            }
        }

        //前面key打乱了顺序，现在重新排序，牌库就变乱序了
        dictWhole = dictWhole.OrderBy(t => t.Key).ToArray().ToDictionary(t => t.Key, u => u.Value);

        log("dictWhole总长度：" + dictWhole.Count());

        foreach (KeyValuePair<int, Card> dictItem in dictWhole)
        {
            //log(dictItem.Value.ToString(), false);
            if (dictItem.Value.UserType != Convert.ToInt32(UserType.Normal)) //在牌库中
            {
                log("出现一个奇怪的东西：" + dictItem.Key + "|" + dictItem.Value.UserType);
            }
        }
    }

    //private void initMj()
    //{
    //    //生成一份麻将 每种麻将有4个  (9 * 3 + 4 + 3 ) * 4 = 136
    //    for (int i = 1; i <= 9; i++)
    //    {
    //        dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Tong), sp[i - 1]); ;          //筒   {(1,CardType.Tong), sprite}
    //    }
    //    for (int i = 1; i <= 9; i++)
    //    {
    //        dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Tiao), sp[i + 8]);            //条  
    //    }
    //    for (int i = 1; i <= 9; i++)
    //    {
    //        dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Wan), sp[i + 1 + 8 * 2]);     //万   
    //    }

    //    for (int i = 1; i <= 4; i++)
    //    {
    //        dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Feng), sp[i + 2 + 8 * 3]);    //东南西北
    //    }

    //    for (int i = 1; i <= 3; i++)
    //    {
    //        dictSingle.Add(i.ToString() + "|" + Convert.ToInt32(CardType.Zi), sp[i + 3 + 8 * 3 + 3]);  //中发白 
    //    }

    //    int index = 0;


    //    foreach (KeyValuePair<string, Sprite> dict in dictSingle)
    //    {
    //        for (int times = 0; times < 4; times++)
    //        {
    //            print("dictSingle 的 key=" + dict.Key);
    //            string[] tempStr = dict.Key.Split('|');
    //            dictWhole.Add(index, new Card(Convert.ToInt32(tempStr[0]), Convert.ToInt32(tempStr[1])));

    //            KeyValuePair<int, Card> item = new KeyValuePair<int, Card>(index, new Card(Convert.ToInt32(tempStr[0]), Convert.ToInt32(tempStr[1])));
    //            ++index;
    //        }
    //    }

    //    foreach (KeyValuePair<int, Card> dictItem in dictWhole)
    //    {
    //        if (dictItem.Value.UserType != Convert.ToInt32(UserType.Normal)) //在牌库中
    //        {
    //            log("出现一个奇怪的东西：" + dictItem.Key + "|" + dictItem.Value.UserType);
    //        }
    //    }
    //}

    //获取随机牌(默认13张)
    private int[] getRandom(int len = 13)
    {

        int[] randomArray = new int[len];
        System.Random random = new System.Random();
        for (int i = 0; i < randomArray.Length; i++)
        {
            int randomNumber = random.Next(0, 136);                //共136张牌 (返回随机下标 0～135)
            while (Array.IndexOf(randomArray, randomNumber) != -1) // check if the number is already in the array
            {
                randomNumber = random.Next(0, 136);                // if it is, generate a new random number
            }
            randomArray[i] = randomNumber;
        }

        return randomArray;
    }

    //游戏结束
    private void end(List<int> winList = null)
    {
        //todo 结算得分
        #region 结束面板中的牌等展示
        int sideOposite = (userDiceSide % 4 + 2) > 4 ? (userDiceSide % 4 + 2) % 4 : (userDiceSide % 4 + 2);
        int sidePrev = (userDiceSide % 4 + 1) > 4 ? (userDiceSide % 4 + 1) % 4 : (userDiceSide % 4 + 1);
        int sideNext = (userDiceSide % 4 + 3) > 4 ? (userDiceSide % 4 + 3) % 4 : (userDiceSide % 4 + 3);

        var sideSelfTransform = gameEnd.transform.Find("sideSelf");
        var sideOpositeTransform = gameEnd.transform.Find("sideOposite");
        var sidePrevTransform = gameEnd.transform.Find("sidePrev");
        var sideNextTransform = gameEnd.transform.Find("sideNext");
        foreach (var currentUserMjItem in currentUserMj)
        {
            var dictTemp = dictWhole[currentUserMjItem.Key];
            var imageTemp = Instantiate(sideSelfTransform.Find("template"), sideSelfTransform);
            (imageTemp.Find("mj").GetComponent<Image>()).sprite = dictSingle[dictTemp.Number.ToString() + "|" + Convert.ToInt32(dictTemp.CardType)];
            if (winList != null && winList.Contains(userDiceSide))
            {
                if (dictTemp.hu == Convert.ToInt32(Hu.Yes))
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spHuMjBg;     //胡牌的背景 
                }
                else
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spWinMjBg;    //赢的人的牌，背景
                }
                gameEnd.transform.Find("winSelf").gameObject.SetActive(true);             //显示“赢”字
            }
            if (dictTemp.TagType == Convert.ToInt32(TagType.AnGang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg; //暗杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }

        foreach (var robotItem in robotList[sideOposite])
        {
            var dictTemp = dictWhole[robotItem.Key];
            var imageTemp = Instantiate(sideOpositeTransform.Find("template"), sideOpositeTransform);
            (imageTemp.Find("mj").GetComponent<Image>()).sprite = dictSingle[dictTemp.Number.ToString() + "|" + Convert.ToInt32(dictTemp.CardType)];
            if (winList != null && winList.Contains(sideOposite))
            {
                if (dictTemp.hu == Convert.ToInt32(Hu.Yes))
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spHuMjBg;     //胡牌的背景 
                }
                else
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spWinMjBg;    //赢的人的牌，背景
                }
                gameEnd.transform.Find("winOposite").gameObject.SetActive(true);
            }
            if (dictTemp.TagType == Convert.ToInt32(TagType.AnGang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg; //暗杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        foreach (var robotItem in robotList[sidePrev])
        {
            var dictTemp = dictWhole[robotItem.Key];
            var imageTemp = Instantiate(sidePrevTransform.Find("template"), sidePrevTransform);
            (imageTemp.Find("mj").GetComponent<Image>()).sprite = dictSingle[dictTemp.Number.ToString() + "|" + Convert.ToInt32(dictTemp.CardType)];
            if (winList != null && winList.Contains(sidePrev))
            {
                if (dictTemp.hu == Convert.ToInt32(Hu.Yes))
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spHuMjBg;     //胡牌的背景 
                }
                else
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spWinMjBg;    //赢的人的牌，背景
                }
                gameEnd.transform.Find("winPrev").gameObject.SetActive(true);
            }
            if (dictTemp.TagType == Convert.ToInt32(TagType.AnGang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg; //暗杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        foreach (var robotItem in robotList[sideNext])
        {
            var dictTemp = dictWhole[robotItem.Key];
            var imageTemp = Instantiate(sideNextTransform.Find("template"), sideNextTransform);
            (imageTemp.Find("mj").GetComponent<Image>()).sprite = dictSingle[dictTemp.Number.ToString() + "|" + Convert.ToInt32(dictTemp.CardType)];
            if (winList != null && winList.Contains(sideNext))
            {
                if (dictTemp.hu == Convert.ToInt32(Hu.Yes))
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spHuMjBg;     //胡牌的背景 
                }
                else
                {
                    (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spWinMjBg;    //赢的人的牌，背景
                }
                gameEnd.transform.Find("winNext").gameObject.SetActive(true);
            }
            if (dictTemp.TagType == Convert.ToInt32(TagType.AnGang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg; //暗杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        #endregion

        Time.timeScale = 0; //游戏暂停
        gameEnd.SetActive(true);
    }

    private void log(string str, bool isEndLine = true, bool isClean = false)
    {
        if (openLog)
        {
            if (isClean || txtInfo.GetComponent<Text>().text.Length >= 100)
            {
                txtInfo.GetComponent<Text>().text = "";
            }
            txtInfo.GetComponent<Text>().text += str + (isEndLine ? "\n" : "");
        }
    }
    #endregion

    #region 当前活跃的牌 （1.用户抓到的手牌 或者 2.用户打出的牌）
    private void setCurrentActiveCard(int key)
    {
        currentActiveCardKey = key;
    }

    private int getCurrentActiveCard()
    {
        return currentActiveCardKey;
    }
    #endregion

    #region 展示相关
    //展示用户牌
    private void displayUser(int[] userCardInts)
    {
        Dictionary<int, int> userCardDictPublic = new Dictionary<int, int>();
        if (userCardInts == null)
        {
            int[] currentUserMjKeys = currentUserMj.Keys.ToArray<int>();

            foreach (var currentUserMjItem in currentUserMj)
            {
                DestroyTile(currentUserMjItem.Value); //删除用户牌的游戏物体 (因为下面会重新画)

                var tempCurrentUserMj = dictWhole[currentUserMjItem.Key];
                if (tempCurrentUserMj.TagType != Convert.ToInt32(TagType.Normal))
                {
                    // key是唯一的， 值使用 牌的大小 + 牌的形态 + 牌的类型*100，便于排序
                    userCardDictPublic.Add(currentUserMjItem.Key, tempCurrentUserMj.Number + tempCurrentUserMj.TagType + tempCurrentUserMj.CardType * 100);
                }
            }

            currentUserMj.Clear(); //清空
            userCardInts = currentUserMjKeys;

            //log("", true, true);
            //log("用户的手牌为：" + string.Join(",", currentUserMjKeys));
        }

        var userCardDictPublicSort = userCardDictPublic.OrderBy(t => t.Value);

        List<int> rsUserCardIntPublic = new List<int>();

        //string str = "";

        foreach (var userCardDictPublicSortItem in userCardDictPublicSort)
        {
            //str += dictWhole[userCardDictPublicSortItem.Key].ToString() + "," + userCardDictPublicSortItem.Value + "|";
            rsUserCardIntPublic.Add(userCardDictPublicSortItem.Key);
        }
        //print(str);

        //获得没公开的用户手牌 （没 碰，吃，杠）
        List<int> userCardIntsList = new List<int>();
        for (int i = 0; i < userCardInts.Length; i++)
        {
            bool has = false;
            foreach (var userCardIntPublicItem in rsUserCardIntPublic)
            {
                if (userCardIntPublicItem == userCardInts[i])
                {
                    has = true;
                }
            }

            if (!has)
            {
                userCardIntsList.Add(userCardInts[i]);
            }
        }

        //if (rsUserCardIntPublic.Count() > 0)
        //{
        //log("用户公开的手牌是：" + string.Join(",", userCardIntPublic.ToArray<int>()));
        //}

        int[] userCardIntsCopy = userCardIntsList.ToArray<int>();

        int spaceIndex = 0;

        //平铺展示用户抓到的牌
        spaceIndex = displayPublic(rsUserCardIntPublic, spaceIndex);
        //log("展示用户公开的牌 spaceIndex=" + spaceIndex + " 不公开的牌的张数：" + userCardIntsCopy.Length);
        spaceIndex = display(userCardIntsCopy, CardType.Tong, spaceIndex);
        spaceIndex = display(userCardIntsCopy, CardType.Tiao, spaceIndex);
        spaceIndex = display(userCardIntsCopy, CardType.Wan, spaceIndex);
        spaceIndex = display(userCardIntsCopy, CardType.Feng, spaceIndex);
        spaceIndex = display(userCardIntsCopy, CardType.Zi, spaceIndex);

        for (int i = 0; i < userCardInts.Length; i++)
        {
            dictWhole[userCardInts[i]].UserType = Convert.ToInt32(UserType.User); //在整副牌中标记这个牌是用户的
        }

        //log("用户的手牌是：" + string.Join(",", userCardInts) + "长度：" + userCardInts.Length + "spaceIndex=" + spaceIndex + " count=" + currentUserMj.Count());
    }

    //展示 用户/机器人 公开的牌
    private int displayPublic(List<int> cardIntPublic, int spaceIndex, int robotDiceSide = 0, Dictionary<int, GameObject> tempRobot = null)
    {
        Dictionary<int, int> tempSort = new Dictionary<int, int>(); // dictWhole的key  ==> 牌的点数Number
                                                                    // Dictionary<int, Sprite> dictTemp = new Dictionary<int, Sprite>();
        foreach (var key in cardIntPublic)
        {
            tempSort.Add(key, dictWhole[key].Number);
        }

        //var tempSort = temp.OrderBy(t => t.Value); //Value就是牌的点数，根据这个进行排序 todo 举个例子如果公杠了5万和5条，可能顺序就不对了

        if (robotDiceSide == 0) //不是机器人，是自己
        {
            Card card = new Card(-1, -1);
            Vector3 origin = new Vector3(-86.4f, -31.8f, -61.7f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(34.2f, -35.2f, -58.1f) - origin).normalized; //单位法向量

            foreach (KeyValuePair<int, int> tempValue in tempSort)
            {
                position = origin + spaceIndex * pos * 6.15f; //沿向量移动
                GameObject gameObjectInit = CreateTile(mjNoScript, position, Quaternion.Euler(177.95f, -1.62f, 271.62f), userParent.transform);
                gameObjectInit.transform.localPosition = position;  //必须使用localPosition
                gameObjectInit.name = tempValue.Key.ToString();
                Card tempCard = dictWhole[tempValue.Key];
                (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                if (tempCard.TagType == Convert.ToInt32(TagType.AnGang))
                {
                    if (card.Number != tempCard.Number || card.CardType != tempCard.CardType)  //暗杠的牌发生了变化，往上漂浮
                    {
                        card = new Card(tempCard.Number, tempCard.CardType);
                        gameObjectInit.transform.localPosition = new Vector3(position.x + 6.3f, position.y + 3.9f, position.z + 0.3f);
                        gameObjectInit.transform.localRotation = Quaternion.Euler(1.654f, 178.398f, 631.203f);
                        --spaceIndex;
                    }
                }
                currentUserMj.Add(tempValue.Key, gameObjectInit);
                ++spaceIndex;
            }

            return --spaceIndex;
        }
        else  //机器人
        {
            if (robotDiceSide == Convert.ToInt32(Side.Sorth))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(36.7f, 8.4f, -20.8f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(44.9f, -14.2f, -68f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(160, 240, 320);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 4.5f; //沿向量移动
                    GameObject gameObjectInit = CreateTile(mjNoScript, position, Quaternion.Euler(193.627f, 266.655f, 244.009f), userParent.transform);
                    gameObjectInit.transform.localPosition = position;  //必须使用localPosition
                    gameObjectInit.transform.localScale = scale;
                    gameObjectInit.name = tempValue.Key.ToString();
                    Card tempCard = dictWhole[tempValue.Key];
                    (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                    if (tempCard.TagType == Convert.ToInt32(TagType.AnGang))
                    {
                        if (card.Number != tempCard.Number || card.CardType != tempCard.CardType)  //暗杠的牌发生了变化，往上漂浮
                        {
                            card = new Card(tempCard.Number, tempCard.CardType);
                            gameObjectInit.transform.localPosition = new Vector3(position.x + 0.1f, position.y + 1.1f, position.z - 5f);
                            gameObjectInit.transform.localRotation = Quaternion.Euler(-12.484f, 86.062f, 245.002f);
                            --spaceIndex;
                        }
                    }
                    tempRobot.Add(tempValue.Key, gameObjectInit);
                    ++spaceIndex;
                }

                return spaceIndex;
            }
            else if (robotDiceSide == Convert.ToInt32(Side.West))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(24.8f, 5.5f, 8.6f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(-45.8f, 4.8f, 8.9f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(260, 390, 520);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 7.5f; //沿向量移动
                    GameObject gameObjectInit = CreateTile(mjNoScript, position, Quaternion.Euler(-25.293f, 0, 90.663f), userParent.transform);
                    gameObjectInit.transform.localPosition = position;  //必须使用localPosition
                    gameObjectInit.transform.localScale = scale;
                    gameObjectInit.name = tempValue.Key.ToString();
                    Card tempCard = dictWhole[tempValue.Key];
                    (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                    if (tempCard.TagType == Convert.ToInt32(TagType.AnGang))
                    {
                        if (card.Number != tempCard.Number || card.CardType != tempCard.CardType)  //暗杠的牌发生了变化，往上漂浮
                        {
                            card = new Card(tempCard.Number, tempCard.CardType);
                            gameObjectInit.transform.localPosition = new Vector3(position.x - 8f, position.y + 3.9f, position.z - 2.1f);
                            gameObjectInit.transform.localRotation = Quaternion.Euler(-25.293f, 0, 270.586f);
                            --spaceIndex;
                        }
                    }
                    tempRobot.Add(tempValue.Key, gameObjectInit);
                    ++spaceIndex;
                }

                return spaceIndex;
            }
            else if (robotDiceSide == Convert.ToInt32(Side.North))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(-110.618f, -0.143f, 0.339f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(-105.6f, -11.6f, -43.9f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(160, 240, 320);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 4.5f; //沿向量移动
                    GameObject gameObjectInit = CreateTile(mjNoScript, position, Quaternion.Euler(191.86f, 80.352f, 284.829f), userParent.transform);
                    gameObjectInit.transform.localPosition = position;  //必须使用localPosition
                    gameObjectInit.transform.localScale = scale;
                    gameObjectInit.name = tempValue.Key.ToString();
                    Card tempCard = dictWhole[tempValue.Key];
                    (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                    if (tempCard.TagType == Convert.ToInt32(TagType.AnGang))
                    {
                        if (card.Number != tempCard.Number || card.CardType != tempCard.CardType)  //暗杠的牌发生了变化，往上漂浮
                        {
                            card = new Card(tempCard.Number, tempCard.CardType);
                            gameObjectInit.transform.localPosition = new Vector3(position.x + 1.2f, position.y + 2.2f, position.z - 4.1f);
                            gameObjectInit.transform.localRotation = Quaternion.Euler(-11.86f, 260.352f, 284.753f);
                            --spaceIndex;
                        }
                    }
                    tempRobot.Add(tempValue.Key, gameObjectInit);
                    ++spaceIndex;
                }

                return spaceIndex;
            }
            else
            {
                //不会执行到这的
                return spaceIndex;
            }
        }
    }

    //顺序输出每一种牌型（用户未公开的牌）
    private int display(int[] userCardInts, CardType cardType, int spaceIndex, int robotDiceSide = 0, Dictionary<int, GameObject> tempRobot = null)
    {
        //log("展示牌型： " + cardType, false);
        Dictionary<int, int> temp = new Dictionary<int, int>(); // dictWhole的key  ==> 牌的点数Number
                                                                // Dictionary<int, Sprite> dictTemp = new Dictionary<int, Sprite>();
        for (int i = 0; i < userCardInts.Length; i++)
        {
            int index = userCardInts[i];
            if ((CardType)dictWhole[index].CardType == cardType)
            {
                temp.Add(index, dictWhole[index].Number);
            }
        }

        var tempSort = temp.OrderBy(t => t.Value); //Value就是牌的点数，根据这个进行排序

        if (robotDiceSide == 0) //不是机器人，是自己
        {
            Vector3 origin = new Vector3(-30f, 28.98f, -63.5f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(39.6f, 29f, -63.5f) - origin).normalized; //单位法向量

            foreach (KeyValuePair<int, int> tempValue in tempSort)
            {
                position = origin + (spaceIndex - 2) * pos * 6.1f; //沿向量移动

                GameObject gameObjectInit = CreateTile(mjBase, position, Quaternion.Euler(98f, 0, 270f), userParent.transform);
                gameObjectInit.name = tempValue.Key.ToString();
                Card tempCard = dictWhole[tempValue.Key];
                (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
                currentUserMj.Add(tempValue.Key, gameObjectInit);
                ++spaceIndex;
            }

            return spaceIndex;
        }
        else  //机器人
        {
            Vector3 origin, position, pos, scale;
            Quaternion qr;
            getPos(robotDiceSide, out origin, out position, out pos, out qr, out scale);

            foreach (KeyValuePair<int, int> tempValue in tempSort)
            {
                position = origin + spaceIndex * pos * (robotDiceSide % 2 == 0 ? 4.5f : 7.5f); //沿向量移动

                GameObject gameObjectInit = CreateTile(mjNoScript, position, qr, robotParent.transform);
                gameObjectInit.transform.localPosition = position;
                gameObjectInit.name = tempValue.Key.ToString();
                gameObjectInit.transform.localScale = scale;
                Card tempCard = dictWhole[tempValue.Key];
                (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                tempRobot.Add(tempValue.Key, gameObjectInit);
                ++spaceIndex;
            }

            return spaceIndex;
        }
    }

    private void getPos(int robotDiceSide, out Vector3 origin, out Vector3 position, out Vector3 pos, out Quaternion qr, out Vector3 scale)
    {
        origin = new Vector3(0, 0, 0);
        position = origin;
        pos = origin;
        qr = Quaternion.Euler(0, 0, 0);
        scale = new Vector3(200, 300, 400);

        if (robotDiceSide == Convert.ToInt32(Side.Sorth)) //南
        {
            origin = new Vector3(39.77531f, 4.404819f, -16.92654f);
            position = origin;
            pos = (new Vector3(45.09481f, -10.96289f, -49.04408f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(115.218f, 168.311f, 537.976f);
            scale = new Vector3(160, 240, 320);
        }
        else if (robotDiceSide == Convert.ToInt32(Side.North)) //北
        {
            origin = new Vector3(-107f, -0.605f, -9.9f);
            position = origin;
            pos = (new Vector3(-99.7f, -10.9f, -58.4f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(103.203f, 144.852f, 334.019f);
            scale = new Vector3(160, 240, 320);
        }
        else if (robotDiceSide == Convert.ToInt32(Side.West)) //西
        {
            origin = new Vector3(19.2f, 2.095f, 6.7f);
            position = origin;
            pos = (new Vector3(-43f, 2.1f, 6.7f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(78.166f, 0, 90);
            scale = new Vector3(260, 390, 520);
        }
    }

    //剩余牌的展示
    private void displayReverseSide()
    {
        int spaceIndex = 0;
        bool isOdd; //是否奇数

        Vector3 origin1 = new Vector3(34.7f, 35.9f, -54.2f);
        Vector3 position1 = origin1;
        Vector3 pos1 = (new Vector3(-37.2f, 35.9f, -54.2f) - origin1).normalized; //单位法向量

        Vector3 origin2 = new Vector3(-67.8f, 41.1f, -66.6f);
        Vector3 position2 = origin2;
        Vector3 pos2 = (new Vector3(-70.11f, 61.29f, -0.60f) - origin2).normalized;

        Vector3 origin3 = new Vector3(-56f, 59, 0);
        Vector3 position3 = origin3;
        Vector3 pos3 = (new Vector3(18f, 59, 0) - origin3).normalized;

        Vector3 origin4 = new Vector3(56.9f, 63.4f, -25.7f);
        Vector3 position4 = origin4;
        Vector3 pos4 = (new Vector3(58.1f, 47.6f, -65.9f) - origin4).normalized;

        foreach (KeyValuePair<int, Card> dictItem in dictWhole)
        {
            if (dictItem.Value.UserType == Convert.ToInt32(UserType.Normal)) //在牌库中
            {
                isOdd = spaceIndex % 2 == 0 ? false : true;

                Vector3 positionCurrent = new Vector3(0, 0, 0);

                Quaternion qr = Quaternion.Euler(0, 180, 270);
                Vector3 scale = new Vector3(200, 300, 400);

                if (spaceIndex <= 27)  // (27 - 0 + 1) / 2 = 14  也就是每排14对
                {
                    position1 = isOdd ? position1 : (origin1 + spaceIndex * pos1 * 3f);          //沿向量移动
                    position1.y = (isOdd ? (position1.y - 3.2f) : position1.y);
                    positionCurrent = position1;
                    qr = Quaternion.Euler(0, 180, 270);
                }
                else if (spaceIndex <= 55)
                {
                    position2 = isOdd ? position2 : (origin2 + (spaceIndex - 27) * pos2 * 2.5f); //沿向量移
                    position2.y = (isOdd ? (position2.y - 2.6f) : position2.y);
                    position2.z = (isOdd ? (position2.z + 0.5f) : position2.z);
                    positionCurrent = position2;
                    qr = Quaternion.Euler(4.87f, 85.56f, 253f);
                    scale = new Vector3(160, 240, 320);
                }
                else if (spaceIndex <= 83)
                {
                    position3 = isOdd ? position3 : (origin3 + (spaceIndex - 55) * pos3 * 3.9f); //沿向量移动
                    position3.y = (isOdd ? (position3.y - 4.1f) : position3.y);
                    positionCurrent = position3;
                    qr = Quaternion.Euler(0, 180, 270);
                    scale = new Vector3(260, 390, 520);
                }
                else
                {
                    position4 = isOdd ? position4 : (origin4 + (spaceIndex - 83) * pos4 * 2.5f); //沿向量移
                    position4.y = (isOdd ? (position4.y - 2.6f) : position4.y);
                    position4.z = (isOdd ? (position4.z + 0.5f) : position4.z);
                    positionCurrent = position4;
                    qr = Quaternion.Euler(0.251f, 88.184f, 248.504f);
                    scale = new Vector3(160, 240, 320);
                }

                GameObject gameObjectInit = CreateTile(mjHiddenBase, positionCurrent, qr, reverseParent.transform);
                gameObjectInit.name = dictItem.Key.ToString();
                gameObjectInit.transform.localScale = scale;
                Card tempCard = dictItem.Value;
                //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

                ++spaceIndex;
            }
        }

        //log("处理牌库后,dictWhole的长度：" + dictWhole.Count());
        //log("处理牌库后,牌库的spaceIndex=：" + spaceIndex);
    }
    #endregion

    #region 用户相关

    #region 用户抓牌(1.轮次抓牌  2.开杠抓牌)
    private void userGrabCardInvoke()
    {
        userGrabCard();
    }

    private void userGrabCard(bool isAnGang = false)
    {
        userCanKnock = true;
        currentActivityDiceSide = userDiceSide;
        avatarStyle();

        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (isAnGang)
        {
            //获取牌库中的最后一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    prevKeyGrab = keyGrab;
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                }
            }

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }
        }
        else
        {
            //获取牌库中的第一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                    break;
                }
            }
        }

        if (!hasKey)  //没牌了，牌局结束
        {
            log("没牌了，牌局结束");
            end();
            return;
        }

        //删除牌库中的物体显示
        DestroyTile(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

        //显示到用户手中
        int gangCount = 0;
        foreach (var currentUserMjItem in currentUserMj)
        {
            if (dictWhole[currentUserMjItem.Key].TagType == Convert.ToInt32(TagType.Gang))
            {
                ++gangCount;
            }
        }
        GameObject gameObjectInit = CreateTile(mjBase, new Vector3(0, 0, 0), Quaternion.Euler(82f, 180, 90f), userParent.transform);
        gameObjectInit.transform.localPosition = new Vector3(14.5f + 5 * (gangCount / 4), -29.8f, -63.5f);
        gameObjectInit.name = keyGrab.ToString();
        Card tempCard = dictWhole[keyGrab];
        (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        //gameObjectInit.GetComponent<MJ>().Select(); //默认选中
        //牌到了用户手中
        dictWhole[keyGrab].userType = Convert.ToInt32(UserType.User);
        currentUserMj.Add(keyGrab, gameObjectInit);

        log("当前用户抓到了" + dictWhole[keyGrab].ToString());
        currentUserGrabkey = keyGrab;

        //显示  过，吃，碰，公杠，暗杠，胡
        currentUserOperate();
    }
    #endregion

    //显示  过，吃，碰，公杠，暗杠，胡
    private bool currentUserOperate()
    {
        bool returnValue = false;
        Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        int i = 0;

        txtInfoUserMj.GetComponent<Text>().text = "";
        foreach (var currentUserItem in currentUserMj)
        {
            if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
            {
                cards[i] = dictWhole[currentUserItem.Key];
                ++i;
                txtInfoUserMj.GetComponent<Text>().text += dictWhole[currentUserItem.Key].ToString() + (" ");
            }
        }

        setBtnActiveFalse();

        if (currentActivityDiceSide == userDiceSide)  //刚好轮到当前用户
        {
            //是否有暗杠 
            List<Card> canAnGangList = Helper.canAnGangList(cards);
            if (canAnGangList.Count() > 0)
            {
                btnPass.SetActive(true);
                btnAnGang.SetActive(true);
                returnValue = true;
            }
            log("当前用户是否暗杠" + (canAnGangList.Count() > 0 ? "是" : "否"));

            //是否可以胡
            bool isWin = Helper.isWin(cards);
            if (isWin)
            {
                btnPass.SetActive(true);
                btnWin.SetActive(true);
                returnValue = true;
            }
            log("当前用户是否胡了" + (isWin ? "是" : "否"));

            //是否有公杠 (自摸的公杠) (将已经碰了的牌，用来计算)
            Card[] cardPengs = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
            int j = 0;

            foreach (var currentUserItem in currentUserMj)
            {
                if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Peng))
                {
                    cardPengs[j] = dictWhole[currentUserItem.Key];
                    ++j;

                }
            }

            if (Helper.canGang(cardPengs, dictWhole[currentUserGrabkey]))
            {
                btnPass.SetActive(true);
                btnGang.SetActive(true);
                returnValue = true;
            }
        }
        else
        {
            int currentActiveCardIndex = getCurrentActiveCard();

            //判断点炮胡
            Card[] cardsTestWin = (Card[])cards.Clone();
            cardsTestWin[i] = dictWhole[currentActiveCardIndex];
            bool isWin = Helper.isWin(cardsTestWin);
            if (isWin)
            {
                btnPass.SetActive(true);
                btnWin.SetActive(true);
                returnValue = true;
            }
            log("当前用户是否点炮胡了" + (isWin ? "是" : "否"));

            //是否有公杠（别人点的公杠） (将手牌，用来计算)
            if (Helper.canGang(cards, dictWhole[currentActiveCardIndex]))
            {
                //手牌弹起显示
                foreach (var item in currentUserMj)
                {
                    if (dictWhole[item.Key].CardType == dictWhole[currentActiveCardIndex].CardType && dictWhole[item.Key].Number == dictWhole[currentActiveCardIndex].Number)
                    {
                        item.Value.GetComponent<MJ>().Select();
                    }
                }
                btnPass.SetActive(true);
                btnGang.SetActive(true);
                returnValue = true;
            }
            else
            {
                //是否可以碰
                if (Helper.canKan(cards, dictWhole[currentActiveCardIndex]))
                {
                    //手牌弹起显示
                    foreach (var item in currentUserMj)
                    {
                        if (dictWhole[item.Key].CardType == dictWhole[currentActiveCardIndex].CardType && dictWhole[item.Key].Number == dictWhole[currentActiveCardIndex].Number)
                        {
                            item.Value.GetComponent<MJ>().Select();
                        }
                    }

                    btnPass.SetActive(true);
                    //显示用户碰牌按钮
                    btnKan.SetActive(true);
                    returnValue = true;
                }
                //是否可以吃
                if (userDiceSide - currentActivityDiceSide == 1 || userDiceSide - currentActivityDiceSide == -3)
                {
                    dictShun = Helper.canShun(cards, dictWhole[getCurrentActiveCard()]);
                    if (dictShun.Count() > 0)
                    {
                        if (dictShun.Count() == 1)
                        {
                            int prevNumber = 0;
                            //刚好只有1组顺子时，手牌弹起显示
                            foreach (var item in currentUserMj)
                            {
                                foreach (var itemShun in dictShun[dictShun.First().Key])
                                {
                                    if (
                                        itemShun.Value == true
                                        && dictWhole[item.Key].TagType == Convert.ToInt32(TagType.Normal)
                                        && dictWhole[item.Key].CardType == dictWhole[currentActiveCardIndex].CardType
                                        && dictWhole[item.Key].Number == itemShun.Key
                                        )
                                    {
                                        if (prevNumber != itemShun.Key) //防止相同大小的牌弹起2次
                                        {
                                            prevNumber = itemShun.Key;
                                            item.Value.GetComponent<MJ>().Select();
                                        }
                                    }
                                }
                            }
                        }

                        btnPass.SetActive(true);
                        //显示用户吃牌按钮
                        btnChi.SetActive(true);
                        returnValue = true;
                    }

                }
            }
        }

        return returnValue;
    }

    //当前用户打出牌
    private void currentUserKnock(string index)
    {
        setBtnActiveFalse();
        userCanKnock = false;

        #region 平铺显示用户已打出的牌
        Vector3 origin = new Vector3(-62.5f, -24.707f, -40.919f);
        Vector3 position = origin;
        Vector3 pos = (new Vector3(19.3f, -25.5f, -39.5f) - origin).normalized; //单位法向量
        Quaternion qr = Quaternion.Euler(169.763f, -0.93f, 270.6f);
        Vector3 scale = new Vector3(180, 270, 360);

        Vector3 posR = (new Vector3(-62.6f, -23.3f, -33f) - origin).normalized; //单位法向量

        int count = currentUserOutMj.Count();
        int CountR = Convert.ToInt32(Math.Floor(Convert.ToDouble(count / 15)));
        int CountO = count % 15;

        GameObject gameObjectInit = CreateTile(mjNoScript, new Vector3(0, 0, 0), qr, userOutParent.transform);
        gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
        gameObjectInit.transform.localScale = scale;
        gameObjectInit.name = index;
        Card tempCard = dictWhole[Convert.ToInt32(index)];
        (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        #endregion

        //播放音效
        playResourceAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType));
        //StartCoroutine(playAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType) + ".wav"));

        dictWhole[Convert.ToInt32(index)].UserType = Convert.ToInt32(UserType.Out); //标记为已打出

        currentUserOutMj.Add(Convert.ToInt32(index), gameObjectInit); //加入到当前已打出牌的字典中

        DestroyTile(currentUserMj[Convert.ToInt32(index)]); //删除用户牌的游戏物体
        currentUserMj.Remove(Convert.ToInt32(index));   //从用户牌中移除当前打出的牌

        setCurrentActiveCard(Convert.ToInt32(index));

        //整理当前用户的排序
        displayUser(null);

        log("用户" + currentActivityDiceSide + "打出" + dictWhole[Convert.ToInt32(index)].ToString());

        //轮到其他人操作
        Invoke("otherOperate", 0.9f);
    }

    //轮到其他人操作
    private void otherOperate()
    {
        //机器人 其他用户 碰，杠，吃，胡（点炮）
        if (otherRobotHandle(true))
        {

        }
        else
        {
            //轮到下个用户
            //int nextActivityDiceSide = (currentActivityDiceSide % 4 + 3) > 4 ? (currentActivityDiceSide % 4 + 3) % 4 : (currentActivityDiceSide % 4 + 3); //加3刚好是顺时针轮一位
            int nextActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位

            log("轮到" + nextActivityDiceSide, false);

            if (userDiceSide == nextActivityDiceSide) //轮到自己
            {
                log("轮到自己");
                userGrabCard();
            }
            else //轮到机器人
            {
                log("轮到机器人 invokeSeconds=" + invokeSeconds, false);
                Invoke("robotGrabCardInvoke", invokeSeconds);
                log("currentUserKnock");
            }
        }
    }

    //轮到下个用户
    private void next()
    {
        //int nextActivityDiceSide = (currentActivityDiceSide % 4 + 3) > 4 ? (currentActivityDiceSide % 4 + 3) % 4 : (currentActivityDiceSide % 4 + 3); //加3刚好是顺时针轮一位
        int nextActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位

        log("轮到下个用户" + nextActivityDiceSide, false);

        if (userDiceSide == nextActivityDiceSide) //轮到自己
        {
            log("轮到自己");
            Invoke("userGrabCardInvoke", invokeSeconds);
        }
        else //轮到机器人
        {
            log("轮到机器人 invokeSeconds=" + invokeSeconds, false);
            userCanKnock = false;
            Invoke("robotGrabCardInvoke", invokeSeconds);
            log("next");
        }
    }

    //用户“碰，吃，公杠”
    private void userAddCard(int type, Dictionary<int, bool> dictShunItem = null)
    {
        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (type == Convert.ToInt32(TagType.Gang)) //公杠
        {
            int keyAdd;
            if (userDiceSide == currentActivityDiceSide) //情况1:自己摸到牌后将已经碰了的牌公杠
            {
                keyAdd = currentUserGrabkey;
            }
            else // 情况2:手牌有3个，别人出牌点杠
            {
                keyAdd = getCurrentActiveCard();

                //删除打出用户的物体显示
                DestroyTile(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
                //删除 robotOutList 中的
                robotOutList[currentActivityDiceSide].Remove(keyAdd);
                //牌到了用户手中
                currentUserMj.Add(keyAdd, new GameObject("init"));
                dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);
            }
            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Gang); //修改牌的类型为“公杠”
            foreach (var item in currentUserMj)
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Gang); //修改用户手牌牌的类型为“公杠”
                }
            }

            //获取牌库中的最后一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    prevKeyGrab = keyGrab;
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                }
            }

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }

            if (!hasKey)
            {
                //没牌了，牌局结束
                end();
                return;
            }

            displayUser(null);

            //删除牌库中的物体显示
            DestroyTile(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

            //显示到用户手中
            int gangCount = 0;
            foreach (var currentUserMjItem in currentUserMj)
            {
                if (dictWhole[currentUserMjItem.Key].TagType == Convert.ToInt32(TagType.Gang))
                {
                    ++gangCount;
                }
            }
            GameObject gameObjectInit = CreateTile(mjBase, new Vector3(0, 0, 0), Quaternion.Euler(82f, 180, 90f), userParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(14.5f + 5 * (gangCount / 4), -29.8f, -63.5f);
            gameObjectInit.name = keyGrab.ToString();
            Card tempCard = dictWhole[keyGrab];
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
            //gameObjectInit.GetComponent<MJ>().Select(); //默认选中
            //牌到了用户手中
            dictWhole[keyGrab].userType = Convert.ToInt32(UserType.User);
            currentUserMj.Add(keyGrab, gameObjectInit);

            log("当前用户抓到了" + dictWhole[keyGrab].ToString());
            currentUserGrabkey = keyGrab;
            userCanKnock = true;
            currentActivityDiceSide = userDiceSide;
            avatarStyle();

            //显示  过，吃，碰，公杠，暗杠，胡
            currentUserOperate();
        }
        else if (type == Convert.ToInt32(TagType.Peng))
        {
            int keyAdd = getCurrentActiveCard();

            //删除打出用户的物体显示
            DestroyTile(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
            //删除 robotOutList 中的
            robotOutList[currentActivityDiceSide].Remove(keyAdd);
            //牌到了用户手中
            currentUserMj.Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Peng); //修改牌的类型为“碰”
            foreach (var item in currentUserMj)
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Peng); //修改手牌中碰了的牌的类型为“碰”
                }
            }

            displayUser(null);
            userCanKnock = true;
            currentActivityDiceSide = userDiceSide;
            avatarStyle();
        }
        else if (type == Convert.ToInt32(TagType.Chi))
        {
            int keyAdd = getCurrentActiveCard();

            //删除打出用户的物体显示
            DestroyTile(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
            //删除 robotOutList 中的
            robotOutList[currentActivityDiceSide].Remove(keyAdd);
            //牌到了用户手中
            currentUserMj.Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Chi); //修改牌的类型为“吃”

            int prevNumber = 0;
            foreach (var item in currentUserMj)
            {
                foreach (var itemShun in dictShunItem)
                {
                    if (
                        itemShun.Value == true
                        && prevNumber != itemShun.Key //防止相同大小的牌变成2次吃
                        && dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType
                        && dictWhole[item.Key].Number == itemShun.Key
                        && dictWhole[item.Key].TagType == Convert.ToInt32(TagType.Normal) //防止已经被吃的牌被重复利用
                        )
                    {
                        prevNumber = itemShun.Key;
                        dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Chi); //修改手牌中碰了的牌的类型为“吃”

                    }
                }
            }

            displayUser(null);
            userCanKnock = true;
            currentActivityDiceSide = userDiceSide;
            avatarStyle();
        }
    }
    #endregion

    #region 机器人相关
    #region 其他机器人处理 碰，杠，吃（todo），胡（点炮）
    private bool otherRobotHandle(bool hasNoUser = true)
    {
        List<int> winList = new List<int>();
        foreach (KeyValuePair<int, Dictionary<int, GameObject>> robotUserMj in robotList)
        {
            if (robotUserMj.Key != currentActivityDiceSide)
            {
                Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
                int i = 0;
                foreach (KeyValuePair<int, GameObject> robotUserMjItem in robotUserMj.Value)
                {
                    if (dictWhole[robotUserMjItem.Key].TagType == Convert.ToInt32(TagType.Normal))
                    {
                        cards[i] = dictWhole[robotUserMjItem.Key];
                        ++i;
                    }
                }

                Card[] cardsTestWin = (Card[])cards.Clone();
                cardsTestWin[i] = dictWhole[getCurrentActiveCard()];//把当前活跃的牌放里面

                if (Helper.isWin(cardsTestWin))
                {
                    winList.Add(robotUserMj.Key);
                }
                if (winList.Count() == 0)
                {
                    if (Helper.canGang(cards, dictWhole[getCurrentActiveCard()]))
                    {
                        //播放音效
                        playResourceAudio(robotUserMj.Key, "gang");
                        //StartCoroutine(playAudio(robotUserMj.Key, "gang.wav"));

                        robotAddCard(robotUserMj.Key, true);
                        return true;
                    }
                    else if (Helper.canKan(cards, dictWhole[getCurrentActiveCard()]))
                    {
                        //播放音效
                        playResourceAudio(robotUserMj.Key, "peng");
                        //StartCoroutine(playAudio(robotUserMj.Key, "peng.wav"));

                        robotAddCard(robotUserMj.Key);
                        return true;
                    }
                }
            }
        }

        foreach (int side in winList)
        {
            //播放音效
            playResourceAudio(side, "hu2");
            //StartCoroutine(playAudio(robotUserMj.Key, "hu2.wav"));

            //牌到了 用户/机器人 手中
            dictWhole[getCurrentActiveCard()].userType = Convert.ToInt32(UserType.User);
            dictWhole[getCurrentActiveCard()].hu = Convert.ToInt32(Hu.Yes);
            robotList[side].Add(getCurrentActiveCard(), new GameObject("init"));
        }

        if (winList.Count() > 0)
        {
            if (!hasNoUser)
            {
                //判断当前用户是否可以胡
                Card[] cardsUser = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
                int i = 0;

                foreach (var currentUserItem in currentUserMj)
                {
                    if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
                    {
                        cardsUser[i] = dictWhole[currentUserItem.Key];
                        ++i;
                    }
                }

                cardsUser[i] = dictWhole[getCurrentActiveCard()];

                if (Helper.isWin(cardsUser))
                {
                    //播放音效
                    playResourceAudio(userDiceSide, "hu2");
                    winList.Add(userDiceSide);
                }
            }
            //游戏结束 需要判断所有其他机器人以及自己是否能胡（一炮多响），为了最终的结算
            end(winList);
            return true;
        }

        return false;
    }
    #endregion

    #region 机器人抓牌
    private void robotGrabCardInvoke()
    {
        log("robotGrabCardInvoke");
        //currentActivityDiceSide = (currentActivityDiceSide % 4 + 3) > 4 ? (currentActivityDiceSide % 4 + 3) % 4 : (currentActivityDiceSide % 4 + 3); //加3刚好是顺时针轮一位
        currentActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位
        avatarStyle();

        robotGrabCard();
    }

    //暗杠后自己抓牌，不需要改变currentActivityDiceSide
    private void robotGrabCardTrueInvoke()
    {
        log("robotGrabCardTrueInvoke");
        //currentActivityDiceSide = (currentActivityDiceSide % 4 + 3) > 4 ? (currentActivityDiceSide % 4 + 3) % 4 : (currentActivityDiceSide % 4 + 3); //加3刚好是顺时针轮一位
        //currentActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位
        //avatarStyle();

        robotGrabCard(true);
    }

    private void robotGrabCard(bool isAnGan = false)
    {
        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (isAnGan)
        {
            //获取牌库中的最后一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    prevKeyGrab = keyGrab;
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                }
            }

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }
        }
        else
        {
            //获取牌库中的第一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                    break;
                }
            }
        }

        if (!hasKey)
        {
            //没牌了，牌局结束
            end();
            return;
        }

        currentRobotGrabkey = keyGrab;

        //删除牌库中的物体显示
        DestroyTile(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

        GameObject gameObjectInit = null;
        #region 显示到机器人手中
        if (currentActivityDiceSide == Convert.ToInt32(Side.Sorth))     //南边
        {
            gameObjectInit = CreateTile(mjNoScript, new Vector3(48.1f, -20.7f, -69.7f), Quaternion.Euler(115.218f, 168.311f, 536.354f), robotParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(48.1f, -20.7f, -69.7f);
            gameObjectInit.transform.localScale = new Vector3(160, 240, 320);
            gameObjectInit.name = keyGrab.ToString();
            //Card tempCard = dictWhole[keyGrab];
            // 不给显示什么牌
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(Side.West)) //西边
        {
            gameObjectInit = CreateTile(mjNoScript, new Vector3(80.5f, 2.1f, 6.7f), Quaternion.Euler(78.166f, 360f, 450f), robotParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(-80.5f, 2.1f, 6.7f);
            gameObjectInit.transform.localScale = new Vector3(260, 390, 520);
            gameObjectInit.name = keyGrab.ToString();
            //Card tempCard = dictWhole[keyGrab];
            // 不给显示什么牌 
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(Side.North)) //北边
        {
            gameObjectInit = CreateTile(mjNoScript, new Vector3(-98.1f, -13.1f, -69.1f), Quaternion.Euler(103.203f, 144.852f, 334.019f), robotParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(-98.1f, -13.1f, -69.1f);
            gameObjectInit.transform.localScale = new Vector3(160, 240, 320);
            gameObjectInit.name = keyGrab.ToString();
            //Card tempCard = dictWhole[keyGrab];
            // 不给显示什么牌
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else
        {
            throw new Exception("robotGrabCard ERROR!");
            //当前用户默认东边
        }
        #endregion

        log("===========");
        //牌到了 用户/机器人 手中
        dictWhole[keyGrab].userType = Convert.ToInt32(UserType.User);

        log("机器人" + currentActivityDiceSide);

        robotList[currentActivityDiceSide].Add(keyGrab, gameObjectInit);

        log("当前机器人" + currentActivityDiceSide + "抓到了" + dictWhole[keyGrab].ToString());

        //机器人 杠，胡 （不满足条件，会直接出牌）
        currentRobotOperate();
    }
    #endregion

    #region 机器人 杠，胡 （不满足条件，会直接出牌）
    private void currentRobotOperate(bool hasAdd = false)
    {
        Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        int i = 0;

        foreach (var currentRobotItem in robotList[currentActivityDiceSide])
        {
            if (dictWhole[currentRobotItem.Key].TagType == Convert.ToInt32(TagType.Normal))
            {
                cards[i] = dictWhole[currentRobotItem.Key];
                ++i;
            }
        }

        if (Helper.isWin(cards))
        {
            //音效随机一下
            string randomStr = DateTime.Now.Second % 2 == 0 ? "" : "1";
            //播放音效
            playResourceAudio(currentActivityDiceSide, "hu" + randomStr);
            //StartCoroutine(playAudio(currentActivityDiceSide, "hu" + randomStr + ".wav"));

            print("机器人" + currentActivityDiceSide + "胡了");
            end(new List<int>() { currentActivityDiceSide });
            return;
        }

        //是否有暗杠 
        List<Card> canAnGangList = Helper.canAnGangList(cards);
        if (canAnGangList.Count() > 0)
        {
            foreach (var currentRobotItem in robotList[currentActivityDiceSide])
            {
                var cardTemp = dictWhole[currentRobotItem.Key];
                if (cardTemp.Number == canAnGangList.First().Number && cardTemp.CardType == canAnGangList.First().CardType)
                {
                    //播放音效
                    playResourceAudio(currentActivityDiceSide, "angang");
                    dictWhole[currentRobotItem.Key].TagType = Convert.ToInt32(TagType.AnGang); //修改牌的类型为暗杠
                }
            }

            //重新排列机器人的牌
            displayRobot(null, currentActivityDiceSide);

            //摸最后一张牌
            Invoke("robotGrabCardTrueInvoke", invokeSeconds);
        }
        else
        {
            //机器人打出牌
            if (hasAdd)  //说明 碰或者杠了牌，因为要播放语音，延迟出牌
            {
                Invoke("currentRobotKnock", 1f);
            }
            else
            {
                currentRobotKnock();
            }
        }
    }
    #endregion

    #region 展示机器人的牌
    private void displayRobot(int[] robotCardInts, int side)
    {
        List<int> robotCardIntPublic = new List<int>();
        if (robotCardInts == null)
        {
            int[] currentRobotMjKeys = robotList[side].Keys.ToArray<int>();

            foreach (var currentRobotMjItem in robotList[side])
            {
                DestroyTile(currentRobotMjItem.Value); //删除机器人牌的游戏物体 (因为下面会重新画)

                if (dictWhole[currentRobotMjItem.Key].TagType != Convert.ToInt32(TagType.Normal))
                {
                    robotCardIntPublic.Add(currentRobotMjItem.Key);
                }
            }

            robotList[side].Clear(); //清空
            robotCardInts = currentRobotMjKeys;

            //log("", true, true);
            //log("机器人" + side + "的手牌为：" + string.Join(",", currentRobotMjKeys));
        }

        //获得没公开的 用户/机器人 手牌 （没 碰，吃，杠）
        List<int> robotCardIntsList = new List<int>();
        for (int i = 0; i < robotCardInts.Length; i++)
        {
            bool has = false;
            foreach (var robotCardIntPublicItem in robotCardIntPublic)
            {
                if (robotCardIntPublicItem == robotCardInts[i])
                {
                    has = true;
                }
            }

            if (!has)
            {
                robotCardIntsList.Add(robotCardInts[i]);
            }
        }

        if (robotCardIntPublic.Count() > 0)
        {
            //log("机器人" + side + "公开的手牌是：" + string.Join(",", robotCardIntPublic.ToArray<int>()));
        }

        int[] robotCardIntsCopy = robotCardIntsList.ToArray<int>();

        int spaceIndex = 0;

        //平铺展示用户/机器人抓到的牌

        Dictionary<int, GameObject> tempRobot = new Dictionary<int, GameObject>();

        spaceIndex = displayPublic(robotCardIntPublic, spaceIndex, side, tempRobot);
        //log("展示用户公开的牌 spaceIndex=" + spaceIndex + " 不公开的牌的张数：" + robotCardIntsCopy.Length);
        spaceIndex = display(robotCardIntsCopy, CardType.Tong, spaceIndex, side, tempRobot);
        spaceIndex = display(robotCardIntsCopy, CardType.Tiao, spaceIndex, side, tempRobot);
        spaceIndex = display(robotCardIntsCopy, CardType.Wan, spaceIndex, side, tempRobot);
        spaceIndex = display(robotCardIntsCopy, CardType.Feng, spaceIndex, side, tempRobot);
        spaceIndex = display(robotCardIntsCopy, CardType.Zi, spaceIndex, side, tempRobot);

        robotList[side] = tempRobot;

        for (int i = 0; i < robotCardInts.Length; i++)
        {
            dictWhole[robotCardInts[i]].UserType = Convert.ToInt32(UserType.User); //在整副牌中标记这个牌是 用户/机器人 的
        }

        //log("机器人的手牌是：" + string.Join(",", robotCardInts) + "长度：" + robotCardInts.Length + "spaceIndex=" + spaceIndex + " count=" + robotList[currentActivityDiceSide].Count());
    }
    #endregion

    #region 机器人打出牌
    private void currentRobotKnock()
    {
        #region 随机尽量出一张落单的牌
        string index = "";

        Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        int i = 0;
        foreach (int key in robotList[currentActivityDiceSide].Keys)
        {
            if (dictWhole[key].TagType == Convert.ToInt32(TagType.Normal))
            {
                cards[i] = dictWhole[key];
                ++i;
            }
        }

        Card knockCard = Helper.getSingleCard(cards);

        foreach (int key in robotList[currentActivityDiceSide].Keys)
        {
            if (dictWhole[key].TagType == Convert.ToInt32(TagType.Normal) && dictWhole[key].CardType == knockCard.CardType && dictWhole[key].Number == knockCard.Number)
            {
                index = key.ToString();
            }
        }
        log("index=" + index + "======" + "side = " + currentActivityDiceSide);
        log("index=" + index + "======" + dictWhole[Convert.ToInt32(index)].ToString() + "side = " + currentActivityDiceSide);
        #endregion

        #region 平铺显示机器人已打出的牌
        Card tempCard = dictWhole[Convert.ToInt32(index)];
        GameObject gameObjectInit = null;
        if (currentActivityDiceSide == Convert.ToInt32(Side.Sorth))
        {
            Vector3 origin = new Vector3(48.1f, -30.8f, -48f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(38.5f, -9f, -7.3f) - origin).normalized; //单位法向量
            Quaternion qr = Quaternion.Euler(-9.063f, 81.459f, 62.136f);
            Vector3 scale = new Vector3(180, 270, 360);

            Vector3 posR = (new Vector3(40.7f, -32f, -49.1f) - origin).normalized; //单位法向量

            int count = robotOutList[currentActivityDiceSide].Count();
            int CountR = Convert.ToInt32(Math.Floor(Convert.ToDouble(count / 10)));
            int CountO = count % 10;

            gameObjectInit = CreateTile(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = index;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(Side.West))
        {
            Vector3 origin = new Vector3(8f, -10.378f, -6.6f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(-65.3f, -10.3f, -6.6f) - origin).normalized; //单位法向量
            Quaternion qr = Quaternion.Euler(-28.072f, 0, 89.965f);
            Vector3 scale = new Vector3(180, 270, 360);

            Vector3 posR = (new Vector3(8f, -13.9f, -13.2f) - origin).normalized; //单位法向量

            int count = robotOutList[currentActivityDiceSide].Count();
            int CountR = Convert.ToInt32(Math.Floor(Convert.ToDouble(count / 14)));
            int CountO = count % 14;

            gameObjectInit = CreateTile(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = index;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(Side.North))
        {
            Vector3 origin = new Vector3(-91.089f, -7.931f, -5.086f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(-98f, -27.6f, -41.5f) - origin).normalized; //单位法向量
            Quaternion qr = Quaternion.Euler(-1.716f, 279.784f, 117.992f);
            Vector3 scale = new Vector3(180, 270, 360);

            int count = robotOutList[currentActivityDiceSide].Count();
            int CountR = Convert.ToInt32(Math.Floor(Convert.ToDouble(count / 10)));
            int CountO = count % 10;

            Vector3 posR = (new Vector3(-83.7f, -8.2f, -6.4f) - origin).normalized; //单位法向量

            gameObjectInit = CreateTile(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = index;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        #endregion

        //播放音效
        playResourceAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType));
        //StartCoroutine(playAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType) + ".wav"));

        dictWhole[Convert.ToInt32(index)].UserType = Convert.ToInt32(UserType.Out); //标记为已打出

        robotOutList[currentActivityDiceSide].Add(Convert.ToInt32(index), gameObjectInit); //加入到当前机器人已打出牌的字典中

        DestroyTile(robotList[currentActivityDiceSide][Convert.ToInt32(index)]); //删除 用户/机器人 牌的游戏物体
        robotList[currentActivityDiceSide].Remove(Convert.ToInt32(index));   //从用户牌中移除当前打出的牌

        setCurrentActiveCard(Convert.ToInt32(index));

        //重新排列机器人的牌
        displayRobot(null, currentActivityDiceSide);

        log("机器人" + currentActivityDiceSide + "打出" + dictWhole[Convert.ToInt32(index)].ToString());

        //轮到其他机器人或用户操作
        Invoke("otherRobotOrUserOperate", 0.9f);

    }
    #endregion

    //轮到其他机器人或用户操作
    private void otherRobotOrUserOperate()
    {
        if (currentUserOperate())
        {

        }
        else
        {
            //机器人 其他用户 碰，杠，吃，胡（点炮）
            if (otherRobotHandle(false))
            {

            }
            else
            {
                //轮到下个用户
                next();
            }
        }
    }

    //机器人“碰，吃（todo 吃），公杠”
    private void robotAddCard(int side, bool isGang = false)
    {
        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (isGang) //公杠
        {
            int keyAdd;
            if (side == currentActivityDiceSide) //情况1:机器人自己摸到牌后将已经碰了的牌公杠
            {
                keyAdd = currentRobotGrabkey;
            }
            else // 情况2:手牌有3个，别人出牌点杠
            {
                keyAdd = getCurrentActiveCard();

                //todo 是否可以 抢杠胡

                //删除打出机器人的物体显示
                if (robotOutParent.transform.Find(keyAdd.ToString()) != null)
                {
                    DestroyTile(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
                    //删除 robotOutList 中的
                    robotOutList[currentActivityDiceSide].Remove(keyAdd);
                }
                //删除打出用户的物体显示
                if (userOutParent.transform.Find(keyAdd.ToString()) != null)
                {
                    DestroyTile(userOutParent.transform.Find(keyAdd.ToString()).gameObject);
                    //删除 currentUserOutMj 中的
                    currentUserOutMj.Remove(keyAdd);
                }

                //牌（别人打出的）到了机器人手中
                robotList[side].Add(keyAdd, new GameObject("init"));
                dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);
            }
            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Gang); //修改牌的类型为“公杠”
            foreach (var item in robotList[side])
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Gang); //修改手牌牌的类型为“公杠”
                }
            }

            //获取牌库中的最后一张牌
            foreach (var dictWholeItem in dictWhole)
            {
                if (dictWholeItem.Value.userType == Convert.ToInt32(UserType.Normal))
                {
                    prevKeyGrab = keyGrab;
                    keyGrab = dictWholeItem.Key;
                    hasKey = true;
                }
            }

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }

            if (!hasKey)
            {
                //没牌了，牌局结束
                end();
                return;
            }

            //删除牌库中的物体显示
            DestroyTile(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

            //牌到了机器人手中
            dictWhole[keyGrab].userType = Convert.ToInt32(UserType.User);
            robotList[side].Add(keyGrab, /*gameObjectInit*/new GameObject("init"));

            log("机器人抓到了" + dictWhole[keyGrab].ToString());
            currentRobotGrabkey = keyGrab;

            currentActivityDiceSide = side;
            avatarStyle();

            //机器人手牌展示
            displayRobot(null, side);

            //机器人 杠，胡 （不满足条件，会直接出牌）
            currentRobotOperate(true);
        }
        else
        {
            int keyAdd = getCurrentActiveCard();

            //删除打出机器人的物体显示
            if (robotOutParent.transform.Find(keyAdd.ToString()) != null)
            {
                DestroyTile(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
                //删除 robotOutList 中的
                robotOutList[currentActivityDiceSide].Remove(keyAdd);
            }
            //删除打出用户的物体显示
            if (userOutParent.transform.Find(keyAdd.ToString()) != null)
            {
                DestroyTile(userOutParent.transform.Find(keyAdd.ToString()).gameObject);
                //删除 currentUserOutMj 中的
                currentUserOutMj.Remove(keyAdd);
            }

            //牌（别人打出的）到了用户手中
            robotList[side].Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Peng); //修改牌的类型为“碰”
            foreach (var item in robotList[side])
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Peng); //修改手牌中碰了的牌的类型为“碰”
                }
            }

            displayRobot(null, side);
            //userCanKnock = true;
            currentActivityDiceSide = side;
            avatarStyle();
            //机器人直接出牌
            Invoke("currentRobotKnock", 1f);
        }
    }
    #endregion

    #region 弹起用户当前选中的牌，并将用户的其他麻将牌置为未选中
    public void Click(GameObject currentGameObject, bool isSelect)
    {
        foreach (KeyValuePair<int, GameObject> dictItem in currentUserMj)
        {
            //print(dictItem.Value.name + "---");
            if (currentGameObject.name == dictItem.Value.name)
            {
                if (dictWhole[dictItem.Key].TagType != Convert.ToInt32(TagType.Normal)) //点击用户未公开的牌，才会弹起
                {
                    return;
                }

                if (isSelect && userCanKnock && currentActivityDiceSide == userDiceSide) //牌弹起后， 再次点击，则认为是打出牌
                {
                    print("打出");
                    log("", true, true);
                    currentUserKnock(currentGameObject.name);
                    return;
                }
                else
                {
                    currentGameObject.GetComponent<MJ>().Select();
                }
            }
            else
            {
                var mj = dictItem.Value.GetComponent<MJ>();
                if (mj)
                {
                    mj.ResetSelect();
                }
            }
        }
    }
    #endregion

    #region 按钮相关
    //点击“暗杠”
    public void btnAnGangClick()
    {
        //播放音效
        playResourceAudio(userDiceSide, "angang");
        //StartCoroutine(playAudio(userDiceSide, "angang.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //初始化
        Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        int i = 0;
        foreach (var currentUserItem in currentUserMj)
        {
            if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
            {
                cards[i] = dictWhole[currentUserItem.Key];
                ++i;
            }
        }

        //是否有暗杠
        List<Card> canAnGangList = Helper.canAnGangList(cards);
        if (canAnGangList.Count() >= 1)   //一个暗杠或多个暗杠 随便暗杠一个即可
        {
            foreach (var currentUserItem in currentUserMj)
            {
                var cardTemp = dictWhole[currentUserItem.Key];
                if (cardTemp.Number == canAnGangList.First().Number && cardTemp.CardType == canAnGangList.First().CardType)
                {
                    dictWhole[currentUserItem.Key].TagType = Convert.ToInt32(TagType.AnGang); //修改牌的类型为暗杠
                }
            }

            //重新排列用户牌
            displayUser(null);

            //摸最后一张牌
            userGrabCard(true);
        }
    }

    //点击“公杠” (两种情况 1:自己摸到牌后将已经碰了的牌公杠  2:手牌有3个，别人出牌点杠)
    public void btnGangClick()
    {
        //播放音效
        playResourceAudio(userDiceSide, "gang");
        //StartCoroutine(playAudio(userDiceSide, "gang.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        if (userDiceSide == currentActivityDiceSide)
        {
            //是否有公杠 (自摸的公杠) (将已经碰了的牌，用来计算)
            Card[] cardPengs = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
            int j = 0;

            foreach (var currentUserItem in currentUserMj)
            {
                if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Peng))
                {
                    cardPengs[j] = dictWhole[currentUserItem.Key];
                    ++j;

                }
            }

            ////是否可以公杠 (重复判断了。。。 是否有必要？)
            //if (Helper.canGang(cardPengs, dictWhole[currentUserGrabkey]))
            //{
            userAddCard(Convert.ToInt32(TagType.Gang));
            //}
        }
        else
        {
            //是否有公杠（别人点的公杠） (将手牌，用来计算)
            Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
            int i = 0;

            foreach (var currentUserItem in currentUserMj)
            {
                if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
                {
                    cards[i] = dictWhole[currentUserItem.Key];
                    ++i;
                }
            }

            int currentActiveCardIndex = getCurrentActiveCard();

            ////是否可以公杠 (重复判断了。。。 是否有必要？)
            //if (Helper.canGang(cards, dictWhole[currentActiveCardIndex]))
            //{
            //手牌弹起显示
            foreach (var item in currentUserMj)
            {
                if (dictWhole[item.Key].CardType == dictWhole[currentActiveCardIndex].CardType && dictWhole[item.Key].Number == dictWhole[currentActiveCardIndex].Number)
                {
                    item.Value.GetComponent<MJ>().Select();
                }
            }

            userAddCard(Convert.ToInt32(TagType.Gang));
            //}
        }
    }

    //点击“碰”
    public void btnKanClick()
    {
        //播放音效
        playResourceAudio(userDiceSide, "peng");
        //StartCoroutine(playAudio(userDiceSide, "peng.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        //初始化
        //Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        //int i = 0;
        //foreach (var currentUserItem in currentUserMj)
        //{
        //    if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
        //    {
        //        cards[i] = dictWhole[currentUserItem.Key];
        //        ++i;
        //    }
        //}

        ////是否可以碰 ( 重复判断了。。。 是否有必要？)
        //if (Helper.canKan(cards, dictWhole[getCurrentActiveCard()]))
        //{
        userAddCard(Convert.ToInt32(TagType.Peng));
        //}

    }

    //吃
    public void btnChiClick()
    {
        //初始化
        Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
        int i = 0;
        foreach (var currentUserItem in currentUserMj)
        {
            if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
            {
                cards[i] = dictWhole[currentUserItem.Key];
                ++i;
            }
        }

        //是否可以吃
        if (dictShun.Count() == 1) //直接吃
        {
            //播放音效
            playResourceAudio(userDiceSide, "chi");

            //点击完，隐藏按钮
            setBtnActiveFalse();

            userAddCard(Convert.ToInt32(TagType.Chi), dictShun[dictShun.First().Key]);
        }
        else
        {
            //弹出选择层，让用户选择吃哪个
            hideChiPannel();
            int index = 1;
            foreach (var dictShunItem in dictShun)
            {
                Transform panelChi = canvasChi.transform.Find("panelChi" + index);
                panelChi.Find("Text").GetComponent<Text>().text = dictShunItem.Key.ToString();

                int indexInner = 1;
                foreach (var shunItem in dictShunItem.Value)
                {
                    (panelChi.Find("template" + indexInner).Find("mj").GetComponent<Image>()).sprite = dictSingle[shunItem.Key.ToString() + "|" + Convert.ToInt32(dictWhole[getCurrentActiveCard()].CardType)];
                    if (shunItem.Value)
                    {
                        (panelChi.Find("template" + indexInner).Find("mjBg").GetComponent<Image>()).sprite = spMjBg;
                    }
                    else
                    {
                        (panelChi.Find("template" + indexInner).Find("mjBg").GetComponent<Image>()).sprite = spHuMjBg;     //胡牌的背景 （为了高亮）
                    }
                    ++indexInner;
                }
                ++index;

                var rectTf = canvasChi.transform.Find("panelBg").GetComponent<RectTransform>();
                rectTf.sizeDelta = new Vector2((index - 1) * 200, 100);
                rectTf.anchoredPosition = new Vector2((index - 1) * 100, rectTf.anchoredPosition.y);
                canvasChi.transform.Find("panelBg").gameObject.SetActive(true);
                panelChi.gameObject.SetActive(true);
            }
        }
    }

    //隐藏选择吃的页面
    private void hideChiPannel()
    {
        canvasChi.transform.Find("panelChi1").gameObject.SetActive(false);
        canvasChi.transform.Find("panelChi2").gameObject.SetActive(false);
        canvasChi.transform.Find("panelChi3").gameObject.SetActive(false);
        canvasChi.transform.Find("panelBg").gameObject.SetActive(false);
    }

    public void btnChiSelectClick()
    {
        //播放音效
        playResourceAudio(userDiceSide, "chi");

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        string panelChiName = EventSystem.current.currentSelectedGameObject.name;

        string index = canvasChi.transform.Find(panelChiName).Find("Text").GetComponent<Text>().text;

        userAddCard(Convert.ToInt32(TagType.Chi), dictShun[Convert.ToInt32(index)]);
    }

    //点击“过” 
    public void btnPassClick()
    {
        setBtnActiveFalse();

        hideChiPannel();

        //刚好不是自己的回合时，才能next
        if (userDiceSide != currentActivityDiceSide)
        {
            next();
        }
    }

    //点击“胡”
    public void btnWinClick()
    {
        //音效随机一下
        string randomStr = DateTime.Now.Second % 2 == 0 ? "" : "1";
        string wavName = currentActivityDiceSide == userDiceSide ? "hu" + randomStr : "hu2";
        //播放音效
        playResourceAudio(userDiceSide, wavName);
        //StartCoroutine(playAudio(userDiceSide, wavName));

        if (currentActivityDiceSide != userDiceSide) //点炮
        {
            //牌到了 用户 手中
            dictWhole[getCurrentActiveCard()].userType = Convert.ToInt32(UserType.User);
            dictWhole[getCurrentActiveCard()].hu = Convert.ToInt32(Hu.Yes);
            currentUserMj.Add(getCurrentActiveCard(), new GameObject("init"));
            //因为先判断了其他机器人点炮胡，会直接带动当前用户自动胡，所以这里无需判断其他机器人是否点炮胡
        }

        setBtnActiveFalse();
        end(new List<int> { userDiceSide });
    }

    //隐藏按钮
    private void setBtnActiveFalse()
    {
        btnWin.SetActive(false);
        btnGang.SetActive(false);
        btnAnGang.SetActive(false);
        btnKan.SetActive(false);
        btnChi.SetActive(false);
        btnPass.SetActive(false);
    }

    //点击“继续” 重新开始
    public void btnContinue()
    {
        Time.timeScale = 1; //游戏重新开始
        SceneManager.LoadScene(ScenesSelect.Main.ToString(), LoadSceneMode.Single); //重新加载场景
    }

    //点击“关闭” 回到主页
    public void btnClose()
    {
        Time.timeScale = 1; //游戏重新开始
        SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景(回到选择页去填写房间号)
    }
    #endregion
}

public enum Side
{
    East  = 1,  //东
    Sorth = 2,  //南
    West  = 3,  //西
    North = 4,  //北
}