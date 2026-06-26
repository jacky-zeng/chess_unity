using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.SceneManagement; //场景命名空间
using UnityEngine.UI;
using UnityEngine.EventSystems;
using WebSocketSharp;
using Newtonsoft.Json;
using zengyanqiHelper;
using zengyanqiCard;
using zengyanqiCommon;

public class MainOnlineManager : MonoBehaviour
{
    //todo  1.房主退出逻辑  2.断线重连逻辑 3.倒计时逻辑
    /**
      *  字典中的数据结构：
      *  
      *  dictSingle    字符串 i + "|" + Card.CardType  ==>  sprite 
      *  dictWhole     index    ==>  Card(Number,Type,tagType,userType) 
      *  currentUserMj index    ==>  gameObject 
    */

    #region 变量定义
    WebSocket ws;

    //单例模式
    public static MainOnlineManager _instance;
    //public static MainOnlineManager Instance { get { return _instance; } }

    [HideInInspector]
    public int currentActiveCardKey = -1; //当前活跃的牌（用户/机器人打出的牌）
    [HideInInspector]
    public bool isGetLast = false;        //是否抓了最后一张牌，开杠的时候用的，用于处理显示
    [HideInInspector]
    public int currentUserGrabkey = -1;   //当前用户抓到的牌
    [HideInInspector]
    public int currentRobotGrabkey = -1;  //当前机器人抓到的牌

    public bool openLog = false;          //是否打开日志

    public int globalFrameRate = 12;      //帧 (每秒钟更新多少个画面)

    public AudioClip audioClip;

    public Camera CameraMain;

    public Camera CameraNoTaa;

    private bool isOff = false;        //抗锯齿开关

    //牌
    public GameObject reverseParent;   //牌库

    public GameObject userParent;      //用户

    public GameObject userOutParent;   //当前用户打出的

    public GameObject robotParent;     //机器人的

    public GameObject robotOutParent;  //机器人打出的

    //按钮
    public GameObject btnStart;  //游戏开始
    public GameObject btnWin;    //胡
    public GameObject btnGang;   //公杠
    public GameObject btnAnGang; //暗杠
    public GameObject btnKan;    //碰
    public GameObject btnChi;    //吃
    public GameObject btnPass;   //过

    //UI
    public GameObject canvasPrepare;
    public GameObject canvasMain;
    public GameObject canvasChi;
    public GameObject CanvasGameStartSetting; //游戏开始前设置页面
    public GameObject canvasGameSetting;      //游戏设置页面
    public GameObject canvasAlert;

    public GameObject deskBg;       //麻将桌

    public GameObject deskSide1;    //麻将桌上东边用户
    public GameObject deskSide2;    //麻将桌上南边用户
    public GameObject deskSide3;    //麻将桌上西边用户
    public GameObject deskSide4;    //麻将桌上北边用户

    public GameObject gameEnd;    //结束结算页面

    public Sprite spMjBg;         //默认牌的背面

    public Sprite spWinMjBg;      //赢了的牌的背面

    public Sprite spHuMjBg;       //胡牌的背面

    public Sprite spGangMjBg;     //杠牌的背面

    public Sprite spAngangMjBg;   //暗杠牌的背面

    public Sprite[] sp;

    public GameObject mjBase;        //牌面上有值

    public GameObject mjHiddenBase;  //牌面上没值

    public GameObject mjNoScript;    //不带脚本的牌

    public GameObject txtInfo;

    //数据
    private Dictionary<string, Sprite> dictSingle = new Dictionary<string, Sprite>();         //所有牌型（单个）

    public Dictionary<int, Card> dictWhole = new Dictionary<int, Card>();                     //整副牌

    private Dictionary<int, GameObject> currentUserMj = new Dictionary<int, GameObject>();    //当前用户手上的牌

    private Dictionary<int, GameObject> currentUserOutMj = new Dictionary<int, GameObject>(); //当前用户打出的牌

    private Dictionary<int, Dictionary<int, GameObject>> robotList = new Dictionary<int, Dictionary<int, GameObject>>();    //机器人手上的牌

    private Dictionary<int, Dictionary<int, GameObject>> robotOutList = new Dictionary<int, Dictionary<int, GameObject>>(); //机器人打出的牌

    private Dictionary<int, Dictionary<int, bool>> dictShun = new Dictionary<int, Dictionary<int, bool>>();  //吃牌的顺子

    //private int randomDiceSide;                                                              //掷骰子后得到的骰子方位

    private int userDiceSide;                                                                //当前用户的方位（东）

    private int currentActivityDiceSide;                                                     //当前活跃用户的方位

    private int realUserDiceSide;                                                            //当前用户的真实方位(服务端分发的方位)

    private int gapDiceSide;                                                                 //真实方位 与 代码意义上的东边，相差几个方位

    private int[] isOnlineSides;                                                             //真实在线的方位

    private int[] isNotOnlineSides;                                                          //真实不在线的方位

    private bool userCanKnock = false;                                                       //当前用户是否可以出牌

    //延时
    private float invokeSeconds = 0.5f;

    //重试的次数
    private int retryTimes = 0;

    //设备号
    private string deviceUniqueId = "";
    //牌桌房间号
    private string groupNum = "";

    //是否是房间创建者
    private bool isHomeOwner = false;

    private int totalClient = 0;
    private int totalPrepare = 0;

    //倒计时
    public GameObject timer;

    private int isOnChi;
    private int isOnRobotHu;
    private int winRandomDiceSide;

    #endregion

    #region 屏幕变了定义
    /// <summary>
    /// 开发屏幕的宽
    /// </summary>
    public static float DevelopWidth = 1792f;

    /// <summary>
    /// 开发屏幕的长
    /// </summary>
    public static float DevelopHeigh = 828f;

    /// <summary>
    /// 开发高宽比
    /// </summary>
    public static float DevelopRate = DevelopHeigh / DevelopWidth;

    /// <summary>
    /// 设备自身的高
    /// </summary>
    public static int curScreenHeight = Screen.height;

    /// <summary>
    /// 设备自身的高
    /// </summary>
    public static int curScreenWidth = Screen.width;

    /// <summary>
    /// 当前屏幕高宽比
    /// </summary>
    public static float ScreenRate = (float)Screen.height / (float)Screen.width;

    /// <summary>
    /// 世界摄像机rect高的比例
    /// </summary>
    public static float cameraRectHeightRate = DevelopHeigh / ((DevelopWidth / Screen.width) * Screen.height);

    /// <summary>
    /// 世界摄像机rect宽的比例
    /// </summary>
    public static float cameraRectWidthRate = DevelopWidth / ((DevelopHeigh / Screen.height) * Screen.width);
    #endregion

    #region unity相关方法
    private void Awake()
    {
        /*
         总结：
            1.当开发应用在移动端时，“Canvas Scaler”的“UI Scale Mode”为“Scale With Screen Size”，以便自适应移动端屏幕

            2.最好事先知道应用到移动端屏幕的分辨率，或屏幕比例，以对应合适设置“Canvas Scaler”的“Reference Resolution”

            3.当应用是横屏游戏时，把“Canvas Scaler”的“Match”改为“0”，以“Width”为基准缩放UI适应屏幕；当应用是竖屏游戏时，
              把“Canvas Scaler”的“Match”改为“1”，以“Height”为基准缩放UI适应屏幕

            4.当然“Canvas Scaler”还有其他设置，但是不是常用，这里不做介绍了，以上内容针对UI屏幕自适应就够用了
         */
        //Screen.SetResolution(1792, 828, true);
        //Screen.orientation = ScreenOrientation.LandscapeRight;

        Application.targetFrameRate = globalFrameRate;  //设置帧率

        FitCamera(CameraMain/*Camera.main*/);
        FitCamera(CameraNoTaa/*Camera.main*/);

        _instance = this;

        groupNum = PlayerPrefs.GetString(PlayerPrefsKey.GroupNum.ToString());
        deviceUniqueId = PlayerPrefs.GetString(PlayerPrefsKey.DeviceUniqueId.ToString());

        canvasAlert.SetActive(false);
        canvasPrepare.SetActive(true);
        canvasMain.transform.Find("self").gameObject.SetActive(false);
        canvasMain.transform.Find("next").gameObject.SetActive(false);
        canvasMain.transform.Find("oposite").gameObject.SetActive(false);
        canvasMain.transform.Find("prev").gameObject.SetActive(false);

        canvasMain.transform.Find("selfRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("nextRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("opositeRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("prevRobot").gameObject.SetActive(false);

        canvasGameSetting.SetActive(false);
        CanvasGameStartSetting.SetActive(false);

        timer.SetActive(false);

        btnStart.SetActive(false);

        if (!openLog)
        {
            txtInfo.SetActive(false);
        }
        else
        {
            txtInfo.SetActive(true);
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

    public void FitCamera(Camera camera)
    {
        ///适配屏幕。实际屏幕比例<=开发比例的 上下黑  反之左右黑
        if (DevelopRate <= ScreenRate)
        {
            camera.rect = new Rect(0, (1 - cameraRectHeightRate) / 2, 1, cameraRectHeightRate);
        }
        else
        {
            camera.rect = new Rect((1 - cameraRectWidthRate) / 2, 0, cameraRectWidthRate, 1);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //print("Start()");

        string groupNum = PlayerPrefs.GetString(PlayerPrefsKey.GroupNum.ToString());

        //print(groupNum);

        // 创建WebSocket对象并指定服务器地址和端口号
        int type = PlayerPrefs.GetInt(PlayerPrefsKey.Type.ToString()); //类型 1-创建房间  2-加入房间

        isHomeOwner = type == 1 ? true : false;

        //ws = new WebSocket("ws://local.chat.com:9600?device_unique_id=" + deviceUniqueId + (type == 1 ? "&group_num=" : "&join_group_num=") + groupNum);

        //string strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
        //PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + "|Start() begin");

        //不带证书的连接
        //ws = new WebSocket("ws://chat.zengyanqi.com:9601?device_unique_id=" + deviceUniqueId + (type == 1 ? "&group_num=" : "&join_group_num=") + groupNum);

        //*带证书的连接
        ws = new WebSocket("wss://www.zengyanqi.com:9600?device_unique_id=" + deviceUniqueId + (type == 1 ? "&group_num=" : "&join_group_num=") + groupNum);
        ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        //*/

        //strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
        //PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + "|Start() end");

        //// 配置SSL证书验证回调函数
        //ws.SslConfiguration.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
        //{
        //    // 在这里可以对服务器返回的SSL证书进行验证，例如检查证书的合法性、有效期等
        //    return true; // 返回true表示验证通过，连接继续进行；返回false表示验证不通过，连接终止。
        //};

        //strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
        //PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + "|after SslConfiguration");

        // 当连接成功建立时的回调函数
        ws.OnOpen += OnOpen;

        // 当接收到消息时的回调函数
        ws.OnMessage += OnMessage;

        // 当连接关闭时的回调函数
        ws.OnClose += OnClose;

        // 当发生错误时的回调函数
        ws.OnError += OnError;

        // 连接到服务器
        ws.Connect();

        // 修复切换场景导致光照变暗的问题：
        //（Environment Lighting的Source从SkyBox改成Color）：Window -> Rendering -> Lighting - Environment Lighting - Source = Color

        Screen.orientation = ScreenOrientation.AutoRotation;//设置方向为自动(根据需要自动旋转屏幕朝向任何启用的方向。)
        Screen.autorotateToLandscapeRight = true;           //允许自动旋转到右横屏
        Screen.autorotateToLandscapeLeft = true;            //允许自动旋转到左横屏
        Screen.autorotateToPortrait = false;                //不允许自动旋转到纵向
        Screen.autorotateToPortraitUpsideDown = false;      //不允许自动旋转到纵向上下
        Screen.sleepTimeout = SleepTimeout.NeverSleep;      //睡眠时间为从不睡眠
    }

    // Update is called once per frame
    void Update()
    {
        MainThreadDispatcher.Execute();
        //print(DateTime.Now);
    }
    #endregion

    #region 头像
    public void avatarStyle()
    {
        int side1 = (1 + gapDiceSide) > 4 ? (1 + gapDiceSide) % 4 : (1 + gapDiceSide);
        int side2 = (2 + gapDiceSide) > 4 ? (2 + gapDiceSide) % 4 : (2 + gapDiceSide);
        int side3 = (3 + gapDiceSide) > 4 ? (3 + gapDiceSide) % 4 : (3 + gapDiceSide);
        int side4 = (4 + gapDiceSide) > 4 ? (4 + gapDiceSide) % 4 : (4 + gapDiceSide);
        switch (currentActivityDiceSide)  //当前能看到牌的用户始终是1
        {
            case 1:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + side1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side4) as Sprite;
                break;
            case 2:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + side2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side4) as Sprite;
                break;
            case 3:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + side3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side4) as Sprite;
                break;
            case 4:
                (canvasMain.transform.Find("self").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side1) as Sprite;
                (canvasMain.transform.Find("next").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side2) as Sprite;
                (canvasMain.transform.Find("oposite").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side3) as Sprite;
                (canvasMain.transform.Find("prev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_active_" + side4) as Sprite;
                break;
        }
    }
    #endregion

    #region 初始化，结束及相关方法
    /**
     *  庄家方位  各个用户的13张手牌
     */
    private void beginByWs(int randomDiceSide, string dictUsers)
    {
        canvasPrepare.SetActive(false);
        canvasMain.transform.Find("self").gameObject.SetActive(true);
        canvasMain.transform.Find("next").gameObject.SetActive(true);
        canvasMain.transform.Find("oposite").gameObject.SetActive(true);
        canvasMain.transform.Find("prev").gameObject.SetActive(true);

        //玩家永远是代码意义上的东边 !!!   但是玩家头像和声音可以换，方位上加个偏移量 gapDiceSide
        userDiceSide = Convert.ToInt32(OnlineSide.East);

        gapDiceSide = realUserDiceSide - userDiceSide; //真实方位 与 代码意义上的东边，相差几个方位

        foreach (var notOnlineSide in isNotOnlineSides)
        {
            int realDiceSideRobot = notOnlineSide > gapDiceSide ? (notOnlineSide - gapDiceSide) : (4 + notOnlineSide - gapDiceSide);
            if (realDiceSideRobot == 1)
            {
                canvasMain.transform.Find("selfRobot").gameObject.SetActive(true);
            }
            else if (realDiceSideRobot == 2)
            {
                canvasMain.transform.Find("nextRobot").gameObject.SetActive(true);
            }
            else if (realDiceSideRobot == 3)
            {
                canvasMain.transform.Find("opositeRobot").gameObject.SetActive(true);
            }
            else if (realDiceSideRobot == 4)
            {
                canvasMain.transform.Find("prevRobot").gameObject.SetActive(true);
            }
        }

        //print("服务端掷骰子得到的值是：" + randomDiceSide);

        ////随机掷骰子  todo 骰子特效
        //randomDiceSide = (new System.Random()).Next(1, 5);

        //谁最先出牌 庄家
        currentActivityDiceSide = randomDiceSide > gapDiceSide ? (randomDiceSide - gapDiceSide) : (4 + randomDiceSide - gapDiceSide);
        avatarStyle();
        //print("随机掷骰子的出牌方位是（能看到牌的用户始终是1）：" + currentActivityDiceSide);

        //桌子背景图 （仅仅是换个桌布（显示东南西北），以及换个庄家）
        string namePath = "Images/deskBg/desk_" + realUserDiceSide; //注意： 不要文件后缀名
        (deskBg.GetComponent<SpriteRenderer>()).sprite = Resources.Load<Sprite>(namePath) as Sprite;

        string[] dictUsersArr = dictUsers.Split('#');

        //用户随机的x张牌
        displayUser(dictUsersArr[realUserDiceSide - 1].Split(',').Select(str => int.Parse(str)).ToArray());

        //print("用户手牌" + userDiceSide + "|index = " + (realUserDiceSide - 1) + "|" + dictUsersArr[realUserDiceSide - 1]);

        int index = (gapDiceSide + 1);
        //其他三个用户抓牌 暂时其他三个是机器人
        for (int i = 2; i <= 4; i++)  //机器人只有2 3 4
        {
            if (index > 3)
            {
                index = 0;
            }
            int robotDiceSide = i;

            displayRobot(dictUsersArr[index].Split(',').Select(str => int.Parse(str)).ToArray(), robotDiceSide);
            //print("机器人手牌" + robotDiceSide + "|index=" + index + "|" + dictUsersArr[index]);
            robotOutList.Add(robotDiceSide, new Dictionary<int, GameObject>());
            ++index;
        }

        //剩余牌的展示
        displayReverseSide();

        if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.East))
        {
            //当前用户抓牌
            userGrabCard();
        }
        else
        {
            //机器人抓牌 (不在线的用户,由房主代抓)
            int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

            if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
            {
                print("机器人抓牌，机器人在牌桌上看到的方位" + getDeskViewSide(deskViewDiceSide) + "真实机器人方位" + currentActivityDiceSide);
                robotGrabCard();
            }
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sideIn">方位，不同方位的音效放不同文件夹了</param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private void playResourceAudio(int sideIn, string fileName)
    {
        int side = (sideIn + gapDiceSide) > 4 ? (sideIn + gapDiceSide) % 4 : (sideIn + gapDiceSide);

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

    //生成
    private void Init()
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
    }

    //游戏结束
    private void end(List<int> winList = null, int dianPaoDeskViewDiceSide = 0, int key = -1)
    {
        print("发送:MessageType.End=====牌局结束=====");
        wsSend(Convert.ToInt32(MessageType.End), (winList != null ? string.Join(",", winList) : "") + "|" + dianPaoDeskViewDiceSide + "|" + key);
    }

    private void endByWs(List<int> deskWinList, int dianPaoDeskViewDiceSide, int key)
    {
        setBtnActiveFalse();

        hideChiPannel();

        int dianPaoSide = 0;
        if (dianPaoDeskViewDiceSide != 0)
        {
            dianPaoSide = dianPaoDeskViewDiceSide > gapDiceSide ? (dianPaoDeskViewDiceSide - gapDiceSide) : (4 + dianPaoDeskViewDiceSide - gapDiceSide);
        }

        for (int i = 4; i > 1; i--)
        {
            (canvasPrepare.transform.Find("PannelPrepare").Find("User" + i).GetComponent<Image>()).sprite
                      = Resources.Load<Sprite>("Images/deskBg/avatar1") as Sprite;
        }

        userCanKnock = false;
        List<int> winList = new List<int>(); //当前客户端真实胡牌的方位列表
        if (deskWinList != null)
        {
            foreach (var side in deskWinList)
            {
                int winSide = side > gapDiceSide ? (side - gapDiceSide) : (4 + side - gapDiceSide);
                winList.Add(winSide);

                //点炮的，记得把牌加到胡的那个人那里
                print("方位" + winSide + "|牌桌 " + getDeskViewSide(side) + "胡了, 点炮方位：" + dianPaoDeskViewDiceSide + ":" + getDeskViewSide(dianPaoDeskViewDiceSide));

                if (key != -1) //点炮
                {
                    //播放音效
                    playResourceAudio(winSide, "hu2");

                    if (userDiceSide != winSide)
                    {
                        //牌到了 机器人 手中
                        dictWhole[key].userType = Convert.ToInt32(UserType.User);
                        dictWhole[key].hu = Convert.ToInt32(Hu.Yes);
                        robotList[winSide].Add(key, new GameObject("init"));
                    }
                    else
                    {
                        //牌到了 用户 手中
                        dictWhole[key].userType = Convert.ToInt32(UserType.User);
                        dictWhole[key].hu = Convert.ToInt32(Hu.Yes);
                        currentUserMj.Add(key, new GameObject("init"));
                    }
                }
                else //自摸胡
                {
                    //音效随机一下
                    string randomStr = DateTime.Now.Second % 2 == 0 ? "" : "1";
                    //播放音效
                    playResourceAudio(winSide, "hu" + randomStr);
                }
            }

            winRandomDiceSide = deskWinList.First();
        }

        Dictionary<int, Dictionary<int, int>> sortRobotList = robotListSort();
        //todo 结算得分
        #region 结束面板中的牌等展示
        int sideOposite = 3;
        int sidePrev = 4;
        int sideNext = 2;

        var sideSelfTransform = gameEnd.transform.Find("sideSelf");
        var sideOpositeTransform = gameEnd.transform.Find("sideOposite");
        var sidePrevTransform = gameEnd.transform.Find("sidePrev");
        var sideNextTransform = gameEnd.transform.Find("sideNext");

        int side1 = (1 + gapDiceSide) > 4 ? (1 + gapDiceSide) % 4 : (1 + gapDiceSide);
        int side2 = (2 + gapDiceSide) > 4 ? (2 + gapDiceSide) % 4 : (2 + gapDiceSide);
        int side3 = (3 + gapDiceSide) > 4 ? (3 + gapDiceSide) % 4 : (3 + gapDiceSide);
        int side4 = (4 + gapDiceSide) > 4 ? (4 + gapDiceSide) % 4 : (4 + gapDiceSide);

        (gameEnd.transform.Find("avatarSelf").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side1) as Sprite;
        (gameEnd.transform.Find("avatarNext").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side2) as Sprite;
        (gameEnd.transform.Find("avatarOpositee").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side3) as Sprite;
        (gameEnd.transform.Find("avatarPrev").GetComponent<Image>()).sprite = Resources.Load<Sprite>("Images/avatar/avatar_" + side4) as Sprite;

        if (dianPaoSide == userDiceSide)
        {
            gameEnd.transform.Find("txtDianPaoSelf").gameObject.SetActive(true);
        }
        else if (dianPaoSide == sideOposite)
        {
            gameEnd.transform.Find("txtDianPaoOposite").gameObject.SetActive(true);
        }
        else if (dianPaoSide == sidePrev)
        {
            gameEnd.transform.Find("txtDianPaoPrev").gameObject.SetActive(true);
        }
        else if (dianPaoSide == sideNext)
        {
            gameEnd.transform.Find("txtDianPaoNext").gameObject.SetActive(true);
        }

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
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg;     //暗杠牌的背景 
            }
            else if (dictTemp.TagType == Convert.ToInt32(TagType.Gang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spGangMjBg;       //杠牌的背景 
            }

            imageTemp.gameObject.SetActive(true);
        }

        foreach (var robotItem in sortRobotList[sideOposite])
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
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg;     //暗杠牌的背景 
            }
            else if (dictTemp.TagType == Convert.ToInt32(TagType.Gang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spGangMjBg;       //杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        foreach (var robotItem in sortRobotList[sidePrev])
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
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg;     //暗杠牌的背景 
            }
            else if (dictTemp.TagType == Convert.ToInt32(TagType.Gang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spGangMjBg;       //杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        foreach (var robotItem in sortRobotList[sideNext])
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
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spAngangMjBg;     //暗杠牌的背景 
            }
            else if (dictTemp.TagType == Convert.ToInt32(TagType.Gang))
            {
                (imageTemp.Find("mjBg").GetComponent<Image>()).sprite = spGangMjBg;       //杠牌的背景 
            }
            imageTemp.gameObject.SetActive(true);
        }
        #endregion

        Time.timeScale = 0; //游戏暂停
        gameEnd.SetActive(true);
    }

    //整理机器人手牌
    private Dictionary<int, Dictionary<int, int>> robotListSort()
    {
        Dictionary<int, Dictionary<int, int>> sortRobotList = new Dictionary<int, Dictionary<int, int>>();
        for (int i = 2; i <= 4; i++)
        {
            Dictionary<int, int> tempPublic = new Dictionary<int, int>();
            Dictionary<int, int> temp = new Dictionary<int, int>();

            temp.Clear();
            foreach (var robotItem in robotList[i])
            {
                if ((TagType)dictWhole[robotItem.Key].TagType != TagType.Normal)
                {
                    // key是唯一的， 值使用 牌的大小 + 牌的形态*10 + 牌的类型*100，便于排序
                    tempPublic.Add(robotItem.Key, dictWhole[robotItem.Key].Number + dictWhole[robotItem.Key].TagType * 10 + dictWhole[robotItem.Key].CardType * 100);
                }
                if ((TagType)dictWhole[robotItem.Key].TagType == TagType.Normal && (CardType)dictWhole[robotItem.Key].CardType == CardType.Tong)
                {
                    temp.Add(robotItem.Key, dictWhole[robotItem.Key].Number);
                }
            }
            var tempPublicSort = tempPublic.OrderBy(t => t.Value); //Value就是牌的点数，根据这个进行排序
            var tempSort = temp.OrderBy(t => t.Value);             //Value就是牌的点数，根据这个进行排序

            sortRobotList[i] = tempPublicSort.Concat(tempSort).ToDictionary(x => x.Key, x => x.Value);
            temp.Clear();
            foreach (var robotItem in robotList[i])
            {
                if ((TagType)dictWhole[robotItem.Key].TagType == TagType.Normal && (CardType)dictWhole[robotItem.Key].CardType == CardType.Tiao)
                {
                    temp.Add(robotItem.Key, dictWhole[robotItem.Key].Number);
                }
            }
            tempSort = temp.OrderBy(t => t.Value);
            sortRobotList[i] = sortRobotList[i].Concat(tempSort).ToDictionary(x => x.Key, x => x.Value);
            temp.Clear();
            foreach (var robotItem in robotList[i])
            {
                if ((TagType)dictWhole[robotItem.Key].TagType == TagType.Normal && (CardType)dictWhole[robotItem.Key].CardType == CardType.Wan)
                {
                    temp.Add(robotItem.Key, dictWhole[robotItem.Key].Number);
                }
            }
            tempSort = temp.OrderBy(t => t.Value);
            sortRobotList[i] = sortRobotList[i].Concat(tempSort).ToDictionary(x => x.Key, x => x.Value);
            temp.Clear();
            foreach (var robotItem in robotList[i])
            {
                if ((TagType)dictWhole[robotItem.Key].TagType == TagType.Normal && (CardType)dictWhole[robotItem.Key].CardType == CardType.Feng)
                {
                    temp.Add(robotItem.Key, dictWhole[robotItem.Key].Number);
                }
            }
            tempSort = temp.OrderBy(t => t.Value);
            sortRobotList[i] = sortRobotList[i].Concat(tempSort).ToDictionary(x => x.Key, x => x.Value);
            temp.Clear();
            foreach (var robotItem in robotList[i])
            {
                if ((TagType)dictWhole[robotItem.Key].TagType == TagType.Normal && (CardType)dictWhole[robotItem.Key].CardType == CardType.Zi)
                {
                    temp.Add(robotItem.Key, dictWhole[robotItem.Key].Number);
                }
            }
            tempSort = temp.OrderBy(t => t.Value);
            sortRobotList[i] = sortRobotList[i].Concat(tempSort).ToDictionary(x => x.Key, x => x.Value);
        }

        return sortRobotList;
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
                Destroy(currentUserMjItem.Value); //删除用户牌的游戏物体 (因为下面会重新画)

                var tempCurrentUserMj = dictWhole[currentUserMjItem.Key];
                if (tempCurrentUserMj.TagType != Convert.ToInt32(TagType.Normal))
                {
                    // key是唯一的， 值使用 牌的大小 + 牌的形态*10 + 牌的类型*100，便于排序
                    userCardDictPublic.Add(currentUserMjItem.Key, tempCurrentUserMj.Number + tempCurrentUserMj.TagType * 10 + tempCurrentUserMj.CardType * 100);
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

        //var tempSort = temp.OrderBy(t => t.Value); //Value就是牌的点数，根据这个进行排序 举个例子如果公杠了5万和5条，可能顺序就不对了

        if (robotDiceSide == 0) //不是机器人，是自己
        {
            Card card = new Card(-1, -1);
            Vector3 origin = new Vector3(-86.4f, -31.8f, -61.7f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(34.2f, -35.2f, -58.1f) - origin).normalized; //单位法向量

            foreach (KeyValuePair<int, int> tempValue in tempSort)
            {
                position = origin + spaceIndex * pos * 6.15f; //沿向量移动
                GameObject gameObjectInit = Instantiate(mjNoScript, position, Quaternion.Euler(177.95f, -1.62f, 271.62f), userParent.transform);
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
            if (robotDiceSide == Convert.ToInt32(OnlineSide.Sorth))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(37.6f, 4.3f, -18.7f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(45.2f, -16.5f, -62.2f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(160, 240, 320);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 4.5f; //沿向量移动
                    GameObject gameObjectInit = Instantiate(mjNoScript, position, Quaternion.Euler(193.627f, 266.655f, 244.009f), userParent.transform);
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
            else if (robotDiceSide == Convert.ToInt32(OnlineSide.West))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(24.8f, 5.5f, 8.6f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(-45.8f, 4.8f, 8.9f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(260, 390, 520);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 7.5f; //沿向量移动
                    GameObject gameObjectInit = Instantiate(mjNoScript, position, Quaternion.Euler(-25.293f, 0, 90.663f), userParent.transform);
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
            else if (robotDiceSide == Convert.ToInt32(OnlineSide.North))
            {
                Card card = new Card(-1, -1);
                Vector3 origin = new Vector3(-110.618f, -0.143f, 0.339f);
                Vector3 position = origin;
                Vector3 pos = (new Vector3(-105.6f, -11.6f, -43.9f) - origin).normalized; //单位法向量
                Vector3 scale = new Vector3(160, 240, 320);

                foreach (KeyValuePair<int, int> tempValue in tempSort)
                {
                    position = origin + spaceIndex * pos * 4.5f; //沿向量移动
                    GameObject gameObjectInit = Instantiate(mjNoScript, position, Quaternion.Euler(191.86f, 80.352f, 284.829f), userParent.transform);
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
    private int display(int[] userCardInts, CardType cardType, int spaceIndex)
    {
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

        Vector3 origin = new Vector3(-30f, 28.98f, -63.5f);
        Vector3 position = origin;
        Vector3 pos = (new Vector3(39.6f, 29f, -63.5f) - origin).normalized; //单位法向量

        foreach (KeyValuePair<int, int> tempValue in tempSort)
        {
            position = origin + (spaceIndex - 2) * pos * 6.1f; //沿向量移动

            GameObject gameObjectInit = Instantiate(mjBase, position, Quaternion.Euler(98f, 0, 270f), userParent.transform);
            gameObjectInit.name = tempValue.Key.ToString();
            Card tempCard = dictWhole[tempValue.Key];
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
            currentUserMj.Add(tempValue.Key, gameObjectInit);
            ++spaceIndex;
        }

        return spaceIndex;
    }

    private int displayRobotEasy(int[] userCardInts, int spaceIndex, int robotDiceSide = 0, Dictionary<int, GameObject> tempRobot = null)
    {
        Dictionary<int, int> temp = new Dictionary<int, int>(); // dictWhole的key  ==> 牌的点数Number
                                                                // Dictionary<int, Sprite> dictTemp = new Dictionary<int, Sprite>();
        for (int i = 0; i < userCardInts.Length; i++)
        {
            int index = userCardInts[i];
            temp.Add(index, dictWhole[index].Number);
        }

        Vector3 origin, position, pos, scale;
        Quaternion qr;
        float distance;
        getPos(robotDiceSide, out origin, out pos, out qr, out scale, out distance);

        foreach (KeyValuePair<int, int> tempValue in temp)
        {
            position = origin + spaceIndex * pos * distance; //沿向量移动

            GameObject gameObjectInit = Instantiate(mjNoScript, position, qr, robotParent.transform);
            gameObjectInit.transform.localPosition = position;
            gameObjectInit.name = tempValue.Key.ToString();
            gameObjectInit.transform.localScale = scale;
            Card tempCard = dictWhole[tempValue.Key];
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
            tempRobot.Add(tempValue.Key, gameObjectInit);
            ++spaceIndex;
        }

        return spaceIndex;
    }

    private void getPos(int robotDiceSide, out Vector3 origin, out Vector3 pos, out Quaternion qr, out Vector3 scale, out float distance)
    {
        origin = new Vector3(0, 0, 0);
        pos = origin;
        qr = Quaternion.Euler(0, 0, 0);
        scale = new Vector3(200, 300, 400);
        distance = 0;

        if (robotDiceSide == Convert.ToInt32(OnlineSide.Sorth)) //南
        {
            origin = new Vector3(40.3781891f, 2.66314507f, -20.5665283f);
            pos = (new Vector3(45.09481f, -10.96289f, -49.04408f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(115.218f, 168.311f, 537.976f);
            scale = new Vector3(136, 204, 272);
            distance = 4.08f;
        }
        else if (robotDiceSide == Convert.ToInt32(OnlineSide.North)) //北
        {
            origin = new Vector3(-107f, -0.605f, -9.9f);
            pos = (new Vector3(-99.7f, -10.9f, -58.4f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(103.203f, 144.852f, 334.019f);
            scale = new Vector3(160, 240, 320);
            distance = 4.8f;
        }
        else if (robotDiceSide == Convert.ToInt32(OnlineSide.West)) //西
        {
            origin = new Vector3(19.2f, 2.095f, 6.7f);
            pos = (new Vector3(-43f, 2.1f, 6.7f) - origin).normalized; //单位法向量
            qr = Quaternion.Euler(78.166f, 0, 90);
            scale = new Vector3(260, 390, 520);
            distance = 7.8f;
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
                    position1.y = (isOdd ? (position1.y - 3.99f) : position1.y);
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
                    position3.y = (isOdd ? (position3.y - 5.186f) : position3.y);
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

                GameObject gameObjectInit = Instantiate(mjHiddenBase, positionCurrent, qr, reverseParent.transform);
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

    //弹层提示
    private void alert(string msg)
    {
        canvasAlert.transform.Find("Panel").Find("Text").GetComponent<Text>().text = msg;
        canvasAlert.SetActive(true);
        Invoke("hideAlert", 0.8f);
    }

    private void hideAlert()
    {
        canvasAlert.SetActive(false);
    }
    #endregion

    #region 用户相关

    #region 用户抓牌(1.轮次抓牌  2.开杠抓牌)
    private void userGrabCardInvoke()
    {
        userGrabCard();
    }

    private void userGrabCard(bool isAnGangOrGang = false)
    {
        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (isAnGangOrGang)
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

            print("===========userGrabCard暗杠or杠 isGetLast=" + isGetLast + " prevKeyGrab = " + prevKeyGrab + " (last)keyGrab = " + keyGrab);

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }
            else
            {
                isGetLast = false;
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
        print("------userGrabCard isAnGangOrGang=" + isAnGangOrGang + " keyGrab=" + keyGrab + "------" + dictWhole[keyGrab].ToString());

        if (!hasKey)  //没牌了，牌局结束
        {
            log("没牌了，牌局结束");
            end();
            return;
        }

        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

        print("发送:MessageType.UserGrab用户抓牌，当前方位号(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + (isAnGangOrGang ? "杠或暗杠" : "") + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[Convert.ToInt32(keyGrab)].ToString());
        wsSend(Convert.ToInt32(MessageType.UserGrab), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyGrab.ToString() + "|" + (isAnGangOrGang ? "杠或暗杠" : ""));
    }

    private void userGrabCardByWs(int keyGrab)
    {
        userCanKnock = true;
        currentActivityDiceSide = userDiceSide;
        avatarStyle();

        //删除牌库中的物体显示
        Destroy(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

        //显示到用户手中
        int gangCount = 0;
        foreach (var currentUserMjItem in currentUserMj)
        {
            if (dictWhole[currentUserMjItem.Key].TagType == Convert.ToInt32(TagType.Gang))
            {
                ++gangCount;
            }
        }
        GameObject gameObjectInit = Instantiate(mjBase, new Vector3(0, 0, 0), Quaternion.Euler(82f, 180, 90f), userParent.transform);
        gameObjectInit.transform.localPosition = new Vector3(14.5f + 5 * (gangCount / 4), -29.8f, -63.5f);
        gameObjectInit.name = keyGrab.ToString();
        Card tempCard = dictWhole[keyGrab];
        (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];

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
        setBtnActiveFalse();

        bool returnValue = false;
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

        if (currentActivityDiceSide == userDiceSide)  //刚好轮到当前用户 判断 胡/暗杠/杠 (无需通知其他用户进行优先级比较)
        {
            print("-----刚好轮到当前用户------");
            timerBegin(12.8f);
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

            return returnValue;
        }
        else    //其他用户出的牌，当前用户进行判断显示操作按钮
        {
            int currentActiveCardIndex = getCurrentActiveCard();

            //判断点炮胡
            Card[] cardsTestWin = (Card[])cards.Clone();
            print("-----判断点炮胡: " + i.ToString() + "|" + currentActiveCardIndex.ToString());
            cardsTestWin[i] = dictWhole[currentActiveCardIndex];
            bool isWin = Helper.isWin(cardsTestWin);
            if (isWin)
            {
                return true;
                //btnPass.SetActive(true);
                //btnWin.SetActive(true);
                //returnValue = true;
            }
            log("当前用户是否点炮胡了" + (isWin ? "是" : "否"));

            //是否有公杠（别人点的公杠） (将手牌，用来计算)
            if (Helper.canGang(cards, dictWhole[currentActiveCardIndex]))
            {
                return true;
            }
            else
            {
                //是否可以碰
                if (Helper.canKan(cards, dictWhole[currentActiveCardIndex]))
                {
                    return true;
                }
                //是否可以吃
                if (isOnChi == 1 && (userDiceSide - currentActivityDiceSide == 1 || userDiceSide - currentActivityDiceSide == -3))
                {
                    dictShun = Helper.canShun(cards, dictWhole[getCurrentActiveCard()]);
                    if (dictShun.Count() > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private void currentUserOperateByWs(int deskViewDiceSide)
    {
        setBtnActiveFalse();

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

        if (currentActivityDiceSide == userDiceSide)  //刚好轮到当前用户 error 不可能运行到这里 因为当前用户的直接自己的优先级最高，无需在服务端判定优先级
        {
            throw new Exception("error 不可能运行到这里");
        }

        int currentActiveCardIndex = getCurrentActiveCard();

        //判断点炮胡
        Card[] cardsTestWin = (Card[])cards.Clone();
        cardsTestWin[i] = dictWhole[currentActiveCardIndex];
        bool isWin = Helper.isWin(cardsTestWin);
        if (isWin)
        {
            btnPass.SetActive(true);
            btnWin.SetActive(true);
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
                    item.Value.GetComponent<MJOnline>().StandUp();
                }
            }
            btnPass.SetActive(true);
            btnGang.SetActive(true);
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
                        item.Value.GetComponent<MJOnline>().StandUp();
                    }
                }

                btnPass.SetActive(true);
                //显示用户碰牌按钮
                btnKan.SetActive(true);
            }
            //是否可以吃
            if (isOnChi == 1 && (userDiceSide - currentActivityDiceSide == 1 || userDiceSide - currentActivityDiceSide == -3))
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
                                        item.Value.GetComponent<MJOnline>().StandUp();
                                    }
                                }
                            }
                        }
                    }

                    btnPass.SetActive(true);
                    //显示用户吃牌按钮
                    btnChi.SetActive(true);
                }
            }
        }

        //开启倒计时
        timerBegin();
    }

    //开启倒计时
    private void timerBegin(float timeLeft = 7.8f)
    {
        timer.SetActive(true);
        timer.GetComponent<Timer>().begin(timeLeft);
    }

    //关闭倒计时
    private void timerEnd()
    {
        timer.GetComponent<Timer>().end();
        timer.SetActive(false);
    }

    //当前用户打出牌
    private void currentUserKnock(string keyKnock)
    {
        setBtnActiveFalse();
        userCanKnock = false;

        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

        print("发送:MessageType.UserKnock用户出牌，当前方位号(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[Convert.ToInt32(keyKnock)].ToString());
        wsSend(Convert.ToInt32(MessageType.UserKnock), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyKnock);
    }

    private void currentUserKnockByWs(int deskViewDiceSide, string keyKnock)
    {
        currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);

        print("当前平铺显示打出牌的用户是(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + " 牌" + dictWhole[Convert.ToInt32(keyKnock)].ToString());

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

        GameObject gameObjectInit = Instantiate(mjNoScript, new Vector3(0, 0, 0), qr, userOutParent.transform);
        gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
        gameObjectInit.transform.localScale = scale;
        gameObjectInit.name = keyKnock;
        Card tempCard = dictWhole[Convert.ToInt32(keyKnock)];
        (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        #endregion

        //播放音效
        playResourceAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType));
        //StartCoroutine(playAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType) + ".wav"));

        dictWhole[Convert.ToInt32(keyKnock)].UserType = Convert.ToInt32(UserType.Out); //标记为已打出

        currentUserOutMj.Add(Convert.ToInt32(keyKnock), gameObjectInit); //加入到当前已打出牌的字典中

        Destroy(currentUserMj[Convert.ToInt32(keyKnock)]); //删除用户牌的游戏物体
        currentUserMj.Remove(Convert.ToInt32(keyKnock));   //从用户牌中移除当前打出的牌

        setCurrentActiveCard(Convert.ToInt32(keyKnock));

        //整理当前用户的排序
        displayUser(null);

        //otherOperate();
        //当前用户出牌后 轮到其他人操作
        Invoke("otherOperate", 0.9f);
    }

    //当前用户出牌后 轮到其他人操作
    private void otherOperate()
    {
        print("===otherOperate===");

        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

        print("发送:MessageType.Operate告知服务器，活跃牌｜用户直接过 isHomeOwner=" + isHomeOwner);
        wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard() + "|0");

        if (isHomeOwner /*&& isNotOnlineSides.Contains(deskViewDiceSide)*/) //是房主 /*且 当前用户是机器人*/ （由房主代为控制）
        {
            //机器人 其他用户 碰，杠，吃，胡（点炮）
            otherRobotHandle();
        }
    }

    //用户“碰，吃，公杠” (真实用户才会执行这个函数)  
    private void userAddCard(int type, /*Dictionary<int, bool>*/ string strDictShunItem = null)
    {
        int deskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);

        if (type == Convert.ToInt32(MessageType.AnGang))
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

            //是否有暗杠
            List<Card> canAnGangList = Helper.canAnGangList(cards);
            if (canAnGangList.Count() >= 1)   //一个暗杠或多个暗杠 随便暗杠一个即可  todo 有时间需要改成用户选择杠哪个
            {
                print("发送:MessageType.AnGang用户暗杠，方位号(注意：能看到牌的用户始终是1)" + userDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + canAnGangList.First().ToString());
                wsSend(Convert.ToInt32(MessageType.AnGang), deskViewDiceSide + "|" + realUserDiceSide);
            }
        }
        else if (type == Convert.ToInt32(MessageType.Gang)) //公杠
        {
            int keyAdd = getCurrentActiveCard();
            if (userDiceSide == currentActivityDiceSide) //自己摸到牌后将已经碰了的牌公杠
            {
                keyAdd = currentUserGrabkey;
            }

            print("发送:MessageType.Gang用户公杠，方位号(注意：能看到牌的用户始终是1)" + userDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[keyAdd].ToString());
            wsSend(Convert.ToInt32(MessageType.Gang), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyAdd);
        }
        else if (type == Convert.ToInt32(MessageType.Peng))
        {
            int keyAdd = getCurrentActiveCard();

            print("发送:MessageType.Peng用户碰牌，方位号(注意：能看到牌的用户始终是1)" + userDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[keyAdd].ToString());
            wsSend(Convert.ToInt32(MessageType.Peng), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyAdd);
        }
        else if (type == Convert.ToInt32(MessageType.Chi))
        {
            int keyAdd = getCurrentActiveCard();

            print("发送:MessageType.Chi用户吃牌，方位号(注意：能看到牌的用户始终是1)" + userDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[keyAdd].ToString() + "|" + strDictShunItem);
            wsSend(Convert.ToInt32(MessageType.Chi), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyAdd + "|" + strDictShunItem);
        }
    }

    //当前用户暗杠/碰/吃
    private void userAddCardByWs(int msgType, int keyAdd = 0, /*Dictionary<int, bool>*/string strDictShunItem = null)
    {
        timerBegin(12.8f);
        if (msgType == Convert.ToInt32(MessageType.AnGang))
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

            //是否有暗杠
            List<Card> canAnGangList = Helper.canAnGangList(cards);
            if (canAnGangList.Count() >= 1)   //一个暗杠或多个暗杠 随便暗杠一个即可  todo 有时间需要改成用户选择杠哪个
            {
                //播放音效
                playResourceAudio(userDiceSide, "angang");
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
        else if (msgType == Convert.ToInt32(MessageType.Gang))
        {
            //播放音效
            playResourceAudio(userDiceSide, "gang");
            //注意 此时 currentActivityDiceSide 是打出牌用户的方位
            int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

            if (userDiceSide == currentActivityDiceSide) //情况1:自己摸到牌后将已经碰了的牌公杠
            {
                print("用户杠了牌桌上" + getDeskViewSide(deskViewDiceSide) + "方(自己摸牌公杠)的牌：" + dictWhole[keyAdd].ToString());
            }
            else // 情况2:手牌有3个，别人出牌点杠
            {
                print("用户杠了牌桌上" + getDeskViewSide(deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());
                var tempOutCard = robotOutParent.transform.Find(keyAdd.ToString());
                if (tempOutCard != null)
                {
                    //删除打出用户的物体显示
                    Destroy(tempOutCard.gameObject);
                    //删除 robotOutList 中的
                    robotOutList[currentActivityDiceSide].Remove(keyAdd);
                    //牌到了用户手中
                    currentUserMj.Add(keyAdd, new GameObject("init"));
                    dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);
                }
            }
            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Gang); //修改牌的类型为“公杠”
            foreach (var item in currentUserMj)
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Gang); //修改用户手牌牌的类型为“公杠”
                }
            }

            //重新排列用户牌
            displayUser(null);
            currentActivityDiceSide = userDiceSide;
            avatarStyle();

            //摸最后一张牌
            userGrabCard(true);
        }
        else if (msgType == Convert.ToInt32(MessageType.Peng))
        {
            //播放音效
            playResourceAudio(userDiceSide, "peng");
            int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

            //注意 此时 currentActivityDiceSide 是打出牌用户的方位
            print("用户碰了牌桌上" + getDeskViewSide(deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());
            //删除打出用户的物体显示
            Destroy(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
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
        else if (msgType == Convert.ToInt32(MessageType.Chi))
        {
            //播放音效
            playResourceAudio(userDiceSide, "chi");
            int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

            //注意 此时 currentActivityDiceSide 是打出牌用户的方位
            print("用户吃了牌桌上" + getDeskViewSide(deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());

            //删除打出用户的物体显示
            Destroy(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);
            //删除 robotOutList 中的
            robotOutList[currentActivityDiceSide].Remove(keyAdd);
            //牌到了用户手中
            currentUserMj.Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Chi); //修改牌的类型为“吃”

            string[] strDictShunItems = strDictShunItem.Split('%');

            int prevNumber = 0;
            foreach (var item in currentUserMj)
            {
                foreach (var itemShun in strDictShunItems)
                {
                    var items = itemShun.Split('#');
                    if (
                        items[1] == "1"
                        && prevNumber != Convert.ToInt32(items[0]) //防止相同大小的牌变成2次吃
                        && dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType
                        && dictWhole[item.Key].Number == Convert.ToInt32(items[0])
                        && dictWhole[item.Key].TagType == Convert.ToInt32(TagType.Normal) //防止已经被吃的牌被重复利用
                        )
                    {
                        prevNumber = Convert.ToInt32(items[0]);
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

    //模拟机器人暗杠/碰/吃...
    private void currentRobotAddCardByWs(int deskViewDiceSide, int msgType, int keyAdd = 0, /*Dictionary<int, bool>*/string strDictShunItem = null)
    {
        if (msgType == Convert.ToInt32(MessageType.AnGang))
        {
            //初始化
            Card[] cards = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
            int i = 0;
            //robotList
            foreach (var currentRobotItem in robotList[currentActivityDiceSide])
            {
                if (dictWhole[currentRobotItem.Key].TagType == Convert.ToInt32(TagType.Normal))
                {
                    cards[i] = dictWhole[currentRobotItem.Key];
                    ++i;
                }
            }

            //是否有暗杠
            List<Card> canAnGangList = Helper.canAnGangList(cards);
            if (canAnGangList.Count() >= 1)   //一个暗杠或多个暗杠 机器人随便暗杠一个即可
            {
                //播放音效
                playResourceAudio(currentActivityDiceSide, "angang");

                foreach (var currentRobotItem in robotList[currentActivityDiceSide])
                {
                    var cardTemp = dictWhole[currentRobotItem.Key];
                    if (cardTemp.Number == canAnGangList.First().Number && cardTemp.CardType == canAnGangList.First().CardType)
                    {
                        dictWhole[currentRobotItem.Key].TagType = Convert.ToInt32(TagType.AnGang); //修改牌的类型为暗杠
                    }
                }

                //重新排列机器人牌
                displayRobot(null, currentActivityDiceSide);

                if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
                {
                    //摸最后一张牌
                    Invoke("robotGrabCardTrueInvoke", invokeSeconds * 2);
                }
            }
        }
        else if (msgType == Convert.ToInt32(MessageType.Gang))
        {
            //暂存当前出牌方方位
            int side = currentActivityDiceSide;
            //出牌方
            int _deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
            print("机器人杠了牌桌上" + getDeskViewSide(_deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());

            userCanKnock = false;
            //机器人所在方
            currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
            avatarStyle();
            //播放音效
            playResourceAudio(currentActivityDiceSide, "gang");

            int currentDeskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);

            //todo 是否可以 抢杠胡

            if (_deskViewDiceSide == currentDeskViewDiceSide) //杠了当前用户的牌
            {
                var tempOutCard = userOutParent.transform.Find(keyAdd.ToString());
                if (tempOutCard != null)
                {
                    //删除打出用户的物体显示
                    Destroy(tempOutCard.gameObject);
                    //删除 currentUserOutMj 中的
                    print("(currentRobotAddCardByWs) 删除 currentUserOutMj 中的");
                    currentUserOutMj.Remove(keyAdd);

                    //牌（别人打出的）到了机器人手中
                    robotList[currentActivityDiceSide].Add(keyAdd, new GameObject("init"));
                    dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);
                }
            }
            else
            {
                var tempOutCard = robotOutParent.transform.Find(keyAdd.ToString());
                if (tempOutCard != null)
                {
                    //删除打出机器人的物体显示
                    Destroy(tempOutCard.gameObject);
                    //删除 robotOutList 中的
                    print("(currentRobotAddCardByWs) 删除 robotOutList 中的");
                    robotOutList[side].Remove(keyAdd);

                    //牌（别人打出的）到了机器人手中
                    robotList[currentActivityDiceSide].Add(keyAdd, new GameObject("init"));
                    dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);
                }
            }

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Gang); //修改牌的类型为“公杠”
            foreach (var item in robotList[currentActivityDiceSide])   //手牌中的三个牌（如果是自己摸到的，就是四张牌）也得修改为“公杠”
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Gang); //修改手牌牌的类型为“公杠”
                }
            }

            //重新排列机器人牌
            displayRobot(null, currentActivityDiceSide);

            if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
            {
                //摸最后一张牌
                Invoke("robotGrabCardTrueInvoke", invokeSeconds * 2);
            }
        }
        else if (msgType == Convert.ToInt32(MessageType.Peng))
        {
            //暂存当前出牌方方位
            int side = currentActivityDiceSide;

            int _deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
            print("机器人碰了牌桌上" + getDeskViewSide(_deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());

            userCanKnock = false;
            currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
            avatarStyle();
            //播放音效
            playResourceAudio(currentActivityDiceSide, "peng");

            int currentDeskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);

            if (_deskViewDiceSide == currentDeskViewDiceSide)
            {
                //删除打出 用户 的物体显示
                Destroy(userOutParent.transform.Find(keyAdd.ToString()).gameObject);

                print("删除 currentUserOutMj 中的");
                currentUserOutMj.Remove(keyAdd);
            }
            else
            {
                //删除打出 机器人 的物体显示
                Destroy(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);

                robotOutList[side].Remove(keyAdd);
            }

            //牌到了机器人手中
            robotList[currentActivityDiceSide].Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Peng); //修改牌的类型为“碰”
            foreach (var item in robotList[currentActivityDiceSide])
            {
                if (dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType && dictWhole[item.Key].Number == dictWhole[keyAdd].Number)
                {
                    dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Peng); //修改手牌中碰了的牌的类型为“碰”
                }
            }

            displayRobot(null, currentActivityDiceSide);

            if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
            {
                //机器人直接出牌
                Invoke("currentRobotKnock", 1f);
            }
        }
        else if (msgType == Convert.ToInt32(MessageType.Chi))
        {
            //暂存当前出牌方方位
            int side = currentActivityDiceSide;

            int _deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
            print("机器人吃了牌桌上" + getDeskViewSide(_deskViewDiceSide) + "方的牌：" + dictWhole[keyAdd].ToString());

            userCanKnock = false;
            currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
            avatarStyle();
            //播放音效
            playResourceAudio(currentActivityDiceSide, "chi");

            int currentDeskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);

            if (_deskViewDiceSide == currentDeskViewDiceSide)
            {
                //删除打出 用户 的物体显示
                Destroy(userOutParent.transform.Find(keyAdd.ToString()).gameObject);

                print("删除 currentUserOutMj 中的");
                currentUserOutMj.Remove(keyAdd);
            }
            else
            {
                //删除打出 机器人 的物体显示
                Destroy(robotOutParent.transform.Find(keyAdd.ToString()).gameObject);

                robotOutList[side].Remove(keyAdd);
            }

            //牌到了机器人手中
            robotList[currentActivityDiceSide].Add(keyAdd, new GameObject("init"));
            dictWhole[keyAdd].userType = Convert.ToInt32(UserType.User);

            dictWhole[keyAdd].TagType = Convert.ToInt32(TagType.Chi); //修改牌的类型为“吃”

            string[] strDictShunItems = strDictShunItem.Split('%');

            int prevNumber = 0;
            foreach (var item in robotList[currentActivityDiceSide])
            {
                foreach (var itemShun in strDictShunItems)
                {
                    var items = itemShun.Split('#');
                    if (
                        items[1] == "1"
                        && prevNumber != Convert.ToInt32(items[0]) //防止相同大小的牌变成2次吃
                        && dictWhole[item.Key].CardType == dictWhole[keyAdd].CardType
                        && dictWhole[item.Key].Number == Convert.ToInt32(items[0])
                        && dictWhole[item.Key].TagType == Convert.ToInt32(TagType.Normal) //防止已经被吃的牌被重复利用
                        )
                    {
                        prevNumber = Convert.ToInt32(items[0]);
                        dictWhole[item.Key].TagType = Convert.ToInt32(TagType.Chi); //修改手牌中碰了的牌的类型为“吃”
                    }
                }
            }

            displayRobot(null, currentActivityDiceSide);

            if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
            {
                //机器人直接出牌
                Invoke("currentRobotKnock", 1f);
            }
        }
    }
    #endregion

    #region 机器人相关
    #region 其他机器人处理 碰，杠，胡（点炮）
    private void otherRobotHandle()
    {
        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

        bool returnValue = false;
        List<int> winList = new List<int>();
        foreach (KeyValuePair<int, Dictionary<int, GameObject>> robotUserMj in robotList)
        {
            returnValue = false;
            int robotDeskViewDiceSide = (robotUserMj.Key + gapDiceSide) > 4 ? (robotUserMj.Key + gapDiceSide) % 4 : (robotUserMj.Key + gapDiceSide);

            if (isNotOnlineSides.Contains(robotDeskViewDiceSide)) //房主才会执行 otherRobotHandle ，然后机器人用户才由房主代为操作
            {
                if (deskViewDiceSide == robotDeskViewDiceSide)
                {
                    print("发送:MessageType.Operate告知服务器，当前机器人直接过,当前机器人出的牌");
                    wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + robotDeskViewDiceSide + "|" + getCurrentActiveCard() + "|0");
                    continue;
                }
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

                if (isOnRobotHu == 1 && Helper.isWin(cardsTestWin))
                {
                    returnValue = true;
                    print("发送:MessageType.Operate告知服务器，活跃牌｜机器人可操作");
                    wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + robotDeskViewDiceSide + "|" + getCurrentActiveCard() + "|1");
                    continue;
                }

                if (winList.Count() == 0)
                {
                    if (Helper.canGang(cards, dictWhole[getCurrentActiveCard()]))
                    {
                        returnValue = true;
                        print("发送:MessageType.Operate告知服务器，活跃牌｜机器人可操作");
                        wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + robotDeskViewDiceSide + "|" + getCurrentActiveCard() + "|1");
                        continue;
                    }
                    else if (Helper.canKan(cards, dictWhole[getCurrentActiveCard()]))
                    {
                        returnValue = true;
                        print("发送:MessageType.Operate告知服务器，活跃牌｜机器人可操作");
                        wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + robotDeskViewDiceSide + "|" + getCurrentActiveCard() + "|1");
                        continue;
                    }
                }

                if (!returnValue)
                {
                    print("发送:MessageType.Operate告知服务器，活跃牌｜机器人直接过");
                    wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + robotDeskViewDiceSide + "|" + getCurrentActiveCard() + "|0");
                }
            }
        }
    }

    private void otherRobotHandleByWs(int deskViewDiceSide)
    {
        int robotDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);

        List<int> winList = new List<int>();
        foreach (KeyValuePair<int, Dictionary<int, GameObject>> robotUserMj in robotList)
        {
            if (robotUserMj.Key == userDiceSide)
            {
                print("error");
                throw new Exception("robotUserMj.Key == currentActivityDiceSide error");
            }
            if (robotUserMj.Key == robotDiceSide)
            {
                if (isNotOnlineSides.Contains(deskViewDiceSide))
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

                    if (isOnRobotHu == 1 && Helper.isWin(cardsTestWin))
                    {
                        int winDeskViewDiceSide = (robotUserMj.Key + gapDiceSide) > 4 ? (robotUserMj.Key + gapDiceSide) % 4 : (robotUserMj.Key + gapDiceSide);
                        winList.Add(winDeskViewDiceSide);

                        //一炮多响
                        winList = getAllWinList(winList, robotUserMj);
                        //点炮用户
                        int dianPaoDeskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
                        end(winList, dianPaoDeskViewDiceSide, getCurrentActiveCard());
                        break;
                    }

                    //胡不了
                    if (winList.Count() == 0)
                    {
                        if (Helper.canGang(cards, dictWhole[getCurrentActiveCard()]))
                        {
                            print("发送:MessageType.Gang机器人杠牌，方位号(注意：能看到牌的用户始终是1)" + robotUserMj.Key + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[getCurrentActiveCard()].ToString());
                            wsSend(Convert.ToInt32(MessageType.Gang), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard());
                        }
                        else if (Helper.canKan(cards, dictWhole[getCurrentActiveCard()]))
                        {
                            print("发送:MessageType.Peng机器人碰牌，方位号(注意：能看到牌的用户始终是1)" + robotUserMj.Key + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[getCurrentActiveCard()].ToString());
                            wsSend(Convert.ToInt32(MessageType.Peng), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard());
                        }
                    }
                }
            }
        }
    }

    private List<int> getAllWinList(List<int> winList, KeyValuePair<int, Dictionary<int, GameObject>> robotUserMjIn)
    {
        foreach (KeyValuePair<int, Dictionary<int, GameObject>> robotUserMj in robotList)
        {
            if (robotUserMjIn.Key != robotUserMj.Key) //除去已经判断过是否能胡的方位
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
                    int winDeskViewDiceSide = (robotUserMj.Key + gapDiceSide) > 4 ? (robotUserMj.Key + gapDiceSide) % 4 : (robotUserMj.Key + gapDiceSide);
                    winList.Add(winDeskViewDiceSide);
                }
            }
        }

        if (currentActivityDiceSide != userDiceSide) //说明不是当前用户出的牌
        {
            //判断当前用户是否可以胡
            Card[] cardsUser = { new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1), new Card(-1, -1) };
            int j = 0;

            foreach (var currentUserItem in currentUserMj)
            {
                if (dictWhole[currentUserItem.Key].TagType == Convert.ToInt32(TagType.Normal))
                {
                    cardsUser[j] = dictWhole[currentUserItem.Key];
                    ++j;
                }
            }

            cardsUser[j] = dictWhole[getCurrentActiveCard()];

            if (Helper.isWin(cardsUser))
            {
                int winDeskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);
                winList.Add(winDeskViewDiceSide);
            }
        }

        return winList;
    }
    #endregion

    #region 机器人抓牌
    private void robotGrabCardInvoke()
    {
        //print("robotGrabCardInvoke");
        //轮到下一个机器人抓牌
        currentActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位
        avatarStyle();

        robotGrabCard();
    }

    //暗杠后自己抓牌，不需要改变currentActivityDiceSide
    private void robotGrabCardTrueInvoke()
    {
        print("robotGrabCardTrueInvoke");

        robotGrabCard(true);
    }

    private void robotGrabCard(bool isAnGangOrGang = false)
    {
        int keyGrab = 0;
        int prevKeyGrab = 0;
        bool hasKey = false;

        if (isAnGangOrGang)
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

            print("===========robotGrabCard暗杠or杠 isGetLast=" + isGetLast + " prevKeyGrab = " + prevKeyGrab + " (last)keyGrab = " + keyGrab);

            if (!isGetLast)   //杠完抓倒数第二个牌，防止牌悬空
            {
                isGetLast = true;
                keyGrab = prevKeyGrab;
            }
            else
            {
                isGetLast = false;
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
        print("------robotGrabCard isAnGangOrGang=" + isAnGangOrGang + " keyGrab=" + keyGrab + "------");

        if (!hasKey)
        {
            //没牌了，牌局结束
            end();
            return;
        }

        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
        print("发送:MessageType.UserGrab机器人抓牌，方位号(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + (isAnGangOrGang ? "杠或暗杠" : "") + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[Convert.ToInt32(keyGrab)].ToString());
        wsSend(Convert.ToInt32(MessageType.UserGrab), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyGrab.ToString() + "|" + (isAnGangOrGang ? 1 : 0));
    }

    private void robotGrabCardByWs(int deskViewDiceSide, int keyGrab)
    {
        currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
        avatarStyle();

        currentRobotGrabkey = keyGrab;

        //删除牌库中的物体显示
        Destroy(reverseParent.transform.Find(keyGrab.ToString()).gameObject);

        GameObject gameObjectInit = null;
        #region 显示到机器人手中
        if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.Sorth))     //南边
        {
            gameObjectInit = Instantiate(mjNoScript, new Vector3(48.1f, -20.7f, -69.7f), Quaternion.Euler(115.218f, 168.311f, 536.354f), robotParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(48.1f, -20.7f, -69.7f);
            gameObjectInit.transform.localScale = new Vector3(136, 204, 272);
            gameObjectInit.name = keyGrab.ToString();
            //Card tempCard = dictWhole[keyGrab];
            // 不给显示什么牌
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.West)) //西边
        {
            gameObjectInit = Instantiate(mjNoScript, new Vector3(80.5f, 2.1f, 6.7f), Quaternion.Euler(78.166f, 360f, 450f), robotParent.transform);
            gameObjectInit.transform.localPosition = new Vector3(-80.5f, 2.1f, 6.7f);
            gameObjectInit.transform.localScale = new Vector3(260, 390, 520);
            gameObjectInit.name = keyGrab.ToString();
            //Card tempCard = dictWhole[keyGrab];
            // 不给显示什么牌 
            //(gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.North)) //北边
        {
            gameObjectInit = Instantiate(mjNoScript, new Vector3(-98.1f, -13.1f, -69.1f), Quaternion.Euler(103.203f, 144.852f, 334.019f), robotParent.transform);
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

        print("机器人(注意：能看到牌的用户始终是1)" + currentActivityDiceSide);

        robotList[currentActivityDiceSide].Add(keyGrab, gameObjectInit);

        print("isHomeOwner = " + isHomeOwner + "当前机器人(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + "抓到了" + dictWhole[keyGrab].ToString());

        //int realActiveSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
        //print(deskViewDiceSide + "==两个机器人的真实side到底是否相同？=|===" + realActiveSide);
        if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
        {
            //机器人 杠，胡 （不满足条件，会直接出牌）
            currentRobotOperate();
        }
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

        if (isOnRobotHu == 1 && Helper.isWin(cards))
        {
            int winDeskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
            end(new List<int>() { winDeskViewDiceSide });
            return;
        }

        //是否有暗杠 
        List<Card> canAnGangList = Helper.canAnGangList(cards);
        if (canAnGangList.Count() > 0)
        {

            int anGangKey = -1;
            foreach (var currentRobotItem in robotList[currentActivityDiceSide])
            {
                var cardTemp = dictWhole[currentRobotItem.Key];
                if (cardTemp.Number == canAnGangList.First().Number && cardTemp.CardType == canAnGangList.First().CardType)
                {
                    anGangKey = currentRobotItem.Key;
                }
            }

            if (anGangKey != -1)
            {
                int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
                wsSend(Convert.ToInt32(MessageType.AnGang), deskViewDiceSide + "|" + realUserDiceSide);
            }
        }
        //else if(是否有公杠) //机器人的暂时不做
        //{

        //}
        else
        {
            //机器人打出牌
            if (hasAdd)  //说明 碰或者杠了牌，因为要播放语音，延迟出牌
            {
                Invoke("currentRobotKnock", 1.5f);
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
                Destroy(currentRobotMjItem.Value); //删除机器人牌的游戏物体 (因为下面会重新画)

                if (dictWhole[currentRobotMjItem.Key].TagType != Convert.ToInt32(TagType.Normal))
                {
                    robotCardIntPublic.Add(currentRobotMjItem.Key);
                }
            }

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

        int[] robotCardIntsCopy = robotCardIntsList.ToArray<int>();

        int spaceIndex = 0;

        Dictionary<int, GameObject> tempRobot = new Dictionary<int, GameObject>();

        //平铺展示用户/机器人抓到的牌
        spaceIndex = displayPublic(robotCardIntPublic, spaceIndex, side, tempRobot);

        //print("展示用户公开的牌 spaceIndex=" + spaceIndex + " 不公开的牌的张数：" + robotCardIntsCopy.Length);
        displayRobotEasy(robotCardIntsCopy, spaceIndex, side, tempRobot);

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
        string keyKnock = "";

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
                keyKnock = key.ToString();
            }
        }
        //log("keyKnock=" + keyKnock + "======" + "side = " + currentActivityDiceSide);
        //log("keyKnock=" + keyKnock + "======" + dictWhole[Convert.ToInt32(keyKnock)].ToString() + "side = " + currentActivityDiceSide);
        #endregion

        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
        if (isHomeOwner && isNotOnlineSides.Contains(deskViewDiceSide))
        {
            print("发送:MessageType.UserKnock机器人出牌，当前方位号(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + "在牌桌上" + getDeskViewSide(deskViewDiceSide) + "牌" + dictWhole[Convert.ToInt32(keyKnock)].ToString());
            wsSend(Convert.ToInt32(MessageType.UserKnock), deskViewDiceSide + "|" + realUserDiceSide + "|" + keyKnock);
        }
    }

    private void currentRobotKnockByWs(int deskViewDiceSide, string keyKnock)
    {
        currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
        avatarStyle();

        print("当前平铺显示打出牌的用户是(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + " 牌" + dictWhole[Convert.ToInt32(keyKnock)].ToString());

        #region 平铺显示机器人已打出的牌
        Card tempCard = dictWhole[Convert.ToInt32(keyKnock)];
        GameObject gameObjectInit = null;
        if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.Sorth))
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

            gameObjectInit = Instantiate(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = keyKnock;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.West))
        {
            Vector3 origin = new Vector3(8f, -11.1f, -7.9f);
            Vector3 position = origin;
            Vector3 pos = (new Vector3(-71.8f, -11.1f, -7.9f) - origin).normalized; //单位法向量
            Quaternion qr = Quaternion.Euler(-28.072f, 0, 89.965f);
            Vector3 scale = new Vector3(180, 270, 360);

            Vector3 posR = (new Vector3(8f, -13.9f, -13.2f) - origin).normalized; //单位法向量

            int count = robotOutList[currentActivityDiceSide].Count();
            int CountR = Convert.ToInt32(Math.Floor(Convert.ToDouble(count / 14)));
            int CountO = count % 14;

            gameObjectInit = Instantiate(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = keyKnock;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        else if (currentActivityDiceSide == Convert.ToInt32(OnlineSide.North))
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

            gameObjectInit = Instantiate(mjNoScript, new Vector3(0, 0, 0), qr, robotOutParent.transform);
            gameObjectInit.transform.localPosition = position + CountO * 5.4f * pos + CountR * posR * 7.2f;
            gameObjectInit.transform.localScale = scale;
            gameObjectInit.name = keyKnock;
            (gameObjectInit.GetComponentInChildren<SpriteRenderer>()).sprite = dictSingle[tempCard.Number.ToString() + "|" + Convert.ToInt32(tempCard.CardType)];
        }
        #endregion

        //播放音效
        playResourceAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType));
        //StartCoroutine(playAudio(currentActivityDiceSide, tempCard.Number + Enum.GetName(typeof(CardType), tempCard.CardType) + ".wav"));

        dictWhole[Convert.ToInt32(keyKnock)].UserType = Convert.ToInt32(UserType.Out); //标记为已打出

        print("机器人出牌robotOutList[currentActivityDiceSide].Add(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + "打出" + dictWhole[Convert.ToInt32(keyKnock)].ToString());
        robotOutList[currentActivityDiceSide].Add(Convert.ToInt32(keyKnock), gameObjectInit); //加入到当前机器人已打出牌的字典中

        Destroy(robotList[currentActivityDiceSide][Convert.ToInt32(keyKnock)]); //删除 用户/机器人 牌的游戏物体
        robotList[currentActivityDiceSide].Remove(Convert.ToInt32(keyKnock));   //从用户牌中移除当前打出的牌

        setCurrentActiveCard(Convert.ToInt32(keyKnock));

        //重新排列机器人的牌
        displayRobot(null, currentActivityDiceSide);

        print("机器人(注意：能看到牌的用户始终是1)" + currentActivityDiceSide + "打出" + dictWhole[Convert.ToInt32(keyKnock)].ToString());

        //轮到其他机器人或用户操作
        Invoke("otherRobotOrUserOperate", 0.9f);
    }
    #endregion

    //轮到其他机器人或用户操作
    private void otherRobotOrUserOperate()
    {
        /*
         1. 其他三个方位，均无法操作，自动过
         2. 存在1个或一个以上的用户可以操作 （注意：优先级 胡>杠>碰>吃），则服务端启动一个延时队列，按优先级决定具体操作
         */

        //必须其他可操作 用户/机器人 都无法吃/碰/杠/胡 或者 选择 “过”， 才会轮到下一个用户 （注意：优先级 胡>杠>碰>吃）
        //服务端记录到redis，三个用户都发完，由服务端决定是否next。。。
        int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);
        if (currentUserOperate()) //此时 currentActivityDiceSide != userDiceSide
        {
            print("发送:MessageType.Operate告知服务器，活跃牌｜用户可操作 isHomeOwner=" + isHomeOwner);
            wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard() + "|1");
        }
        else
        {
            print("发送:MessageType.Operate告知服务器，活跃牌｜用户直接过 isHomeOwner=" + isHomeOwner);
            wsSend(Convert.ToInt32(MessageType.Operate), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard() + "|0");
        }

        if (isHomeOwner) //是房主 （由房主代为控制）
        {
            //机器人 其他用户 碰，杠，吃，胡（点炮）
            otherRobotHandle();
        }

    }

    //轮到下个用户
    private void nextByWs(int deskViewDiceSide)
    {
        currentActivityDiceSide = deskViewDiceSide > gapDiceSide ? (deskViewDiceSide - gapDiceSide) : (4 + deskViewDiceSide - gapDiceSide);
        avatarStyle();

        int nextActivityDiceSide = (currentActivityDiceSide % 4 + 1) > 4 ? (currentActivityDiceSide % 4 + 1) % 4 : (currentActivityDiceSide % 4 + 1); //加1刚好是逆时针轮一位

        print("轮到下个用户(注意：能看到牌的用户始终是1)" + nextActivityDiceSide);

        if (userDiceSide == nextActivityDiceSide) //轮到自己
        {
            currentActivityDiceSide = nextActivityDiceSide;
            avatarStyle();

            print("轮到自己");
            Invoke("userGrabCardInvoke", invokeSeconds);
        }
        else //轮到其他
        {
            if (isHomeOwner) //是房主 这个判断其实可以不要，因为只有房主才能收到
            {
                print("==轮到机器人==是房主 且 当前用户是机器人 （由房主代打）" + nextActivityDiceSide);
                //log("轮到机器人 invokeSeconds=" + invokeSeconds, false);
                userCanKnock = false;
                Invoke("robotGrabCardInvoke", invokeSeconds);
            }
            print("==轮到机器人==============" + nextActivityDiceSide + "|isNotOnlineSides=" + string.Join(", ", isNotOnlineSides));
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
                    currentGameObject.GetComponent<MJOnline>().Select();
                    return;
                }
            }
            //else
            //{
            //    var mj = dictItem.Value.GetComponent<MJOnline>();
            //    if (mj)
            //    {
            //        mj.ResetSelect();
            //    }
            //}
        }
    }
    #endregion

    #region 按钮相关

    //结合swoole

    public void btnStartClick()
    {
        if (isHomeOwner)
        {
            print("我是房主，点击了开始按钮");
            if (totalClient != 0 && totalClient == totalPrepare)
            {
                btnStart.SetActive(false);

                isOnChi = PlayerPrefs.GetInt(PlayerPrefsKey.isOnChi.ToString());
                isOnRobotHu = PlayerPrefs.GetInt(PlayerPrefsKey.isOnRobotHu.ToString());

                //牌局开始
                print("发送:MessageType.Start=====牌局开始===== isOnChi = " + isOnChi + " isOnRobotHu = " + isOnRobotHu + " winRandomDiceSide = " + winRandomDiceSide);


                //gapDiceSide //真实方位 与 代码意义上的东边，相差几个方位

                //int xx = winRandomDiceSide > gapDiceSide ? (winRandomDiceSide - gapDiceSide) : (4 + winRandomDiceSide - gapDiceSide);
                //int deskViewDiceSideyy = (winRandomDiceSide + gapDiceSide) > 4 ? (winRandomDiceSide + gapDiceSide) % 4 : (winRandomDiceSide + gapDiceSide);
                //print("=====牌局开始===== xx = " + xx + " deskViewDiceSideyy = " + deskViewDiceSideyy + " gapDiceSide = " + gapDiceSide);

                wsSend(Convert.ToInt32(MessageType.Start), isOnChi + "$" + isOnRobotHu + "$" + winRandomDiceSide);
            }
            else
            {
                alert("请等待其他用户准备");
            }
        }
        else
        {
            print("我是不是房主，我是用户，点击了准备按钮");
            btnStart.SetActive(false);

            //牌局开始
            print("发送:MessageType.Start=====用户准备=====");
            wsSend(Convert.ToInt32(MessageType.Prepare), "prepare");
        }
    }

    //点击“暗杠”
    public void btnAnGangClick()
    {
        //播放音效
        //playResourceAudio(userDiceSide, "angang");
        //StartCoroutine(playAudio(userDiceSide, "angang.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        userAddCard(Convert.ToInt32(MessageType.AnGang));
    }

    //点击“公杠” (两种情况 1:自己摸到牌后将已经碰了的牌公杠  2:手牌有3个，别人出牌点杠)
    public void btnGangClick()
    {
        //播放音效
        //playResourceAudio(userDiceSide, "gang");
        //StartCoroutine(playAudio(userDiceSide, "gang.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        userAddCard(Convert.ToInt32(MessageType.Gang));
    }

    //点击“碰”
    public void btnKanClick()
    {
        //播放音效
        //playResourceAudio(userDiceSide, "peng");
        //StartCoroutine(playAudio(userDiceSide, "peng.wav"));

        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        userAddCard(Convert.ToInt32(MessageType.Peng));
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
            //点击完，隐藏按钮
            setBtnActiveFalse();

            string strDictShunItem = "";
            foreach (var item in dictShun[dictShun.First().Key])
            {
                strDictShunItem += item.Key + "#" + (item.Value ? "1" : "0") + "%";
            }

            userAddCard(Convert.ToInt32(MessageType.Chi), strDictShunItem.TrimEnd('%'));
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
                rectTf.anchoredPosition = new Vector2((index - 1) * 100 + 61, rectTf.anchoredPosition.y);
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
        //点击完，隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        string panelChiName = EventSystem.current.currentSelectedGameObject.name;

        string index = canvasChi.transform.Find(panelChiName).Find("Text").GetComponent<Text>().text;

        string strDictShunItem = "";
        foreach (var item in dictShun[Convert.ToInt32(index)])
        {
            strDictShunItem += item.Key + "#" + (item.Value ? "1" : "0") + "%";
        }

        userAddCard(Convert.ToInt32(MessageType.Chi), strDictShunItem.TrimEnd('%'));
    }

    //点击“过” 
    public void btnPassClick()
    {
        hideChiPannel();

        if (currentActivityDiceSide != userDiceSide)  //不是轮到当前用户
        {
            setBtnActiveFalse();
            int deskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);
            print("点击“过”，发送:MessageType.Pass 牌桌上" + deskViewDiceSide);
            wsSend(Convert.ToInt32(MessageType.Pass), deskViewDiceSide + "|" + realUserDiceSide + "|" + getCurrentActiveCard());
        }
        else   //刚好轮到当前用户，点击了“过”之后，保留“胡”这个按钮
        {
            setBtnActiveFalse(true);
        }
    }

    //点击“胡”
    public void btnWinClick()
    {
        setBtnActiveFalse();

        int winDeskViewDiceSide = (userDiceSide + gapDiceSide) > 4 ? (userDiceSide + gapDiceSide) % 4 : (userDiceSide + gapDiceSide);

        if (currentActivityDiceSide != userDiceSide) //点炮
        {
            int dianPaoDeskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

            List<int> winList = new List<int> { };

            winList.Add(winDeskViewDiceSide);
            //一炮多响 ====begin===== 
            foreach (KeyValuePair<int, Dictionary<int, GameObject>> robotUserMj in robotList)
            {
                if (currentActivityDiceSide != robotUserMj.Key) //除去点炮的方位
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
                        int winDeskViewDiceSideTemp = (robotUserMj.Key + gapDiceSide) > 4 ? (robotUserMj.Key + gapDiceSide) % 4 : (robotUserMj.Key + gapDiceSide);
                        winList.Add(winDeskViewDiceSideTemp);
                    }
                }
            }
            //一炮多响 ====end=====

            end(winList, dianPaoDeskViewDiceSide, getCurrentActiveCard());
        }
        else
        {
            end(new List<int> { winDeskViewDiceSide });
        }
    }

    //隐藏按钮
    private void setBtnActiveFalse(bool isPass = false)
    {
        //关闭倒计时
        timerEnd();

        if (!isPass)  //当前用户如果可以胡的话，即使点了“过”， “胡”这个按钮还是保持可以点的状态
        {
            btnWin.SetActive(false);
        }
        btnGang.SetActive(false);
        btnAnGang.SetActive(false);
        btnKan.SetActive(false);
        btnChi.SetActive(false);
        btnPass.SetActive(false);
    }

    //点击“继续” 重新开始
    public void btnContinue()
    {
        //清空数据
        dictWhole.Clear();// = new Dictionary<int, Card>();                           //整副牌

        currentUserMj.Clear();// = new Dictionary<int, GameObject>();                 //当前用户手上的牌

        currentUserOutMj.Clear();// = new Dictionary<int, GameObject>();              //当前用户打出的牌

        robotList.Clear();// = new Dictionary<int, Dictionary<int, GameObject>>();    //机器人手上的牌

        robotOutList.Clear();// = new Dictionary<int, Dictionary<int, GameObject>>(); //机器人打出的牌

        dictShun.Clear();// = new Dictionary<int, Dictionary<int, bool>>();           //吃牌的顺子

        //隐藏按钮
        setBtnActiveFalse();

        //隐藏选择吃的页面
        hideChiPannel();

        //清空桌面上的牌
        Transform transform;
        for (int i = 0; i < reverseParent.transform.childCount; i++)
        {
            transform = reverseParent.transform.GetChild(i);
            Destroy(transform.gameObject);
        }
        for (int i = 0; i < userParent.transform.childCount; i++)
        {
            transform = userParent.transform.GetChild(i);
            Destroy(transform.gameObject);
        }
        for (int i = 0; i < robotParent.transform.childCount; i++)
        {
            transform = robotParent.transform.GetChild(i);
            Destroy(transform.gameObject);
        }
        for (int i = 0; i < userOutParent.transform.childCount; i++)
        {
            transform = userOutParent.transform.GetChild(i);
            Destroy(transform.gameObject);
        }
        for (int i = 0; i < robotOutParent.transform.childCount; i++)
        {
            transform = robotOutParent.transform.GetChild(i);
            Destroy(transform.gameObject);
        }

        //清空结束页面
        gameEnd.transform.Find("winSelf").gameObject.SetActive(false);
        gameEnd.transform.Find("winOposite").gameObject.SetActive(false);
        gameEnd.transform.Find("winPrev").gameObject.SetActive(false);
        gameEnd.transform.Find("winNext").gameObject.SetActive(false);

        gameEnd.transform.Find("txtDianPaoSelf").gameObject.SetActive(false);
        gameEnd.transform.Find("txtDianPaoOposite").gameObject.SetActive(false);
        gameEnd.transform.Find("txtDianPaoPrev").gameObject.SetActive(false);
        gameEnd.transform.Find("txtDianPaoNext").gameObject.SetActive(false);

        var sideSelfTransform = gameEnd.transform.Find("sideSelf");
        var sideOpositeTransform = gameEnd.transform.Find("sideOposite");
        var sidePrevTransform = gameEnd.transform.Find("sidePrev");
        var sideNextTransform = gameEnd.transform.Find("sideNext");

        for (int i = 0; i < sideSelfTransform.transform.childCount; i++)
        {
            if (sideSelfTransform.transform.GetChild(i).gameObject.name != "template")
            {
                transform = sideSelfTransform.transform.GetChild(i);
                Destroy(transform.gameObject);
            }
        }
        for (int i = 0; i < sideOpositeTransform.transform.childCount; i++)
        {
            if (sideOpositeTransform.transform.GetChild(i).gameObject.name != "template")
            {
                transform = sideOpositeTransform.transform.GetChild(i);
                Destroy(transform.gameObject);
            }
        }
        for (int i = 0; i < sidePrevTransform.transform.childCount; i++)
        {
            if (sidePrevTransform.transform.GetChild(i).gameObject.name != "template")
            {
                transform = sidePrevTransform.transform.GetChild(i);
                Destroy(transform.gameObject);
            }
        }
        for (int i = 0; i < sideNextTransform.transform.childCount; i++)
        {
            if (sideNextTransform.transform.GetChild(i).gameObject.name != "template")
            {
                transform = sideNextTransform.transform.GetChild(i);
                Destroy(transform.gameObject);
            }
        }

        gameEnd.SetActive(false);

        canvasAlert.SetActive(false);
        canvasPrepare.SetActive(true);

        //桌面头像重置
        canvasMain.transform.Find("self").gameObject.SetActive(false);
        canvasMain.transform.Find("next").gameObject.SetActive(false);
        canvasMain.transform.Find("oposite").gameObject.SetActive(false);
        canvasMain.transform.Find("prev").gameObject.SetActive(false);

        canvasMain.transform.Find("selfRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("nextRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("opositeRobot").gameObject.SetActive(false);
        canvasMain.transform.Find("prevRobot").gameObject.SetActive(false);

        //开始/准备 页面重置
        if (isHomeOwner)
        {
            btnStart.SetActive(true);
            btnStart.transform.Find("Text").GetComponent<Text>().text = "开始";
        }
        else
        {
            btnStart.SetActive(true);
            btnStart.transform.Find("Text").GetComponent<Text>().text = "准备";
        }

        wsSend(Convert.ToInt32(MessageType.Prepare));

        Time.timeScale = 1; //游戏开始
    }

    //点击“设置”
    public void btnSetting()
    {
        canvasGameSetting.SetActive(true);
    }

    //点击“关闭设置”
    public void btnCloseSetting()
    {
        canvasGameSetting.SetActive(false);
    }

    //开启/关闭 抗锯齿
    public void checkBoxTAAChange(bool isOnxx)
    {
        print(isOff + "------------" + isOnxx);
        if (isOff)
        {
            CameraMain.gameObject.SetActive(true);
            CameraNoTaa.gameObject.SetActive(false);

            isOff = false;
        }
        else
        {
            CameraMain.gameObject.SetActive(false);
            CameraNoTaa.gameObject.SetActive(true);

            isOff = true;
        }
    }

    //点击“退出房间” 回到房间号输入页面
    public void btnExitGroup()
    {
        // 关闭连接
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }

        Time.timeScale = 1; //游戏重新开始
        SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景(回到选择页去填写房间号)
    }

    //开启/关闭 允许 “吃” 牌
    public void checkBoxChi(bool isOn)
    {
        int isOnChiSave = PlayerPrefs.GetInt(PlayerPrefsKey.isOnChi.ToString());


        if (isOnChiSave == 1)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey.isOnChi.ToString(), 0);
        }
        else
        {
            PlayerPrefs.SetInt(PlayerPrefsKey.isOnChi.ToString(), 1);
        }

        print(isOnChiSave + "isOnChi======" + PlayerPrefs.GetInt(PlayerPrefsKey.isOnChi.ToString()) + "|" + isOn);
    }

    //开启/关闭 允许机器人 “胡”
    public void checkBoxRobotHu(bool isOn)
    {
        int isOnRobotHuSave = PlayerPrefs.GetInt(PlayerPrefsKey.isOnRobotHu.ToString());


        if (isOnRobotHuSave == 1)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey.isOnRobotHu.ToString(), 0);
        }
        else
        {
            PlayerPrefs.SetInt(PlayerPrefsKey.isOnRobotHu.ToString(), 1);
        }

        print(isOnRobotHuSave + "isOnRobotHu======" + PlayerPrefs.GetInt(PlayerPrefsKey.isOnRobotHu.ToString()) + "|" + isOn);
    }

    //点击“确定” （设置完成）
    public void btnOk()
    {
        CanvasGameStartSetting.SetActive(false);
    }

    ////点击“回到首页” 
    //public void btnHome()
    //{
    //    // 关闭连接
    //    if (ws != null && ws.IsAlive)
    //    {
    //        ws.Close();
    //    }

    //    Time.timeScale = 1; //游戏重新开始
    //    SceneManager.LoadScene(ScenesSelect.Start.ToString(), LoadSceneMode.Single); //加载场景(回到首页)
    //}
    #endregion

    #region websocket
    void OnDestroy()
    {
         print("OnDestroy!!!");
        // 关闭连接
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }

    void OnOpen(object sender, System.EventArgs e)
    {
        print("连接已建立");
        //将要在主线程执行的处理代码
        Action action = () =>
        {
            Init();
        };

        //将任务添加到主线程任务队列中等待执行
        MainThreadDispatcher.Enqueue(action);
    }

    void OnMessage(object sender, MessageEventArgs e)
    {
        //print("OnMessage接收到消息：" + e.Data);

        ReceiveMessage msgs = JsonConvert.DeserializeObject<ReceiveMessage>(e.Data);

        if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Fail))
        {
            string strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
            PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + " OnMessage()" + msgs.message);
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Connect))
        {
            string[] strs = msgs.message.Split('|');
            totalClient = Convert.ToInt32(strs[0]);
            totalPrepare = totalPrepare == 0 ? 1 : totalPrepare;
            MainThreadDispatcher.Enqueue(() =>
            {
                if (isHomeOwner)
                {
                    btnStart.SetActive(true);
                    CanvasGameStartSetting.SetActive(true);
                    btnStart.transform.Find("Text").GetComponent<Text>().text = "开始";
                }
                else
                {
                    btnStart.SetActive(true);
                    CanvasGameStartSetting.SetActive(false);
                    btnStart.transform.Find("Text").GetComponent<Text>().text = "准备"; 
                }

                 (canvasPrepare.transform.Find("PannelPrepare").Find("User1").GetComponent<Image>()).sprite
                       = Resources.Load<Sprite>("Images/deskBg/avatarPrepare") as Sprite;

                for (int i = 1; i <= totalClient; i++)
                {
                    (canvasPrepare.transform.Find("PannelPrepare").Find("Status" + i).GetComponent<Image>()).sprite
                    = Resources.Load<Sprite>("Images/deskBg/online") as Sprite;
                }
                for (int j = 4; j > totalClient; j--)
                {
                    (canvasPrepare.transform.Find("PannelPrepare").Find("Status" + j).GetComponent<Image>()).sprite
                    = Resources.Load<Sprite>("Images/deskBg/offline") as Sprite;
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Prepare))
        {
            string[] strs = msgs.message.Split('|');
            totalPrepare = Convert.ToInt32(strs[0]);
            MainThreadDispatcher.Enqueue(() =>
            {
                for (int i = 1; i <= totalPrepare; i++)
                {
                    (canvasPrepare.transform.Find("PannelPrepare").Find("User" + i).GetComponent<Image>()).sprite
                    = Resources.Load<Sprite>("Images/deskBg/avatarPrepare") as Sprite;
                }
                for (int j = 4; j > totalPrepare; j--)
                {
                    (canvasPrepare.transform.Find("PannelPrepare").Find("User" + j).GetComponent<Image>()).sprite
                    = Resources.Load<Sprite>("Images/deskBg/avatar1") as Sprite;
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.End))
        {
            string[] strs = msgs.message.Split('|'); // strs[0] winList ｜ strs[1] dianPaoDeskViewDiceSide ｜ str[2] key
            List<int> winList = strs[0].Length == 0 ? null : strs[0].Split(',').Select(str => int.Parse(str)).ToList();

            print("winList = " + strs[0]);   //胡牌的方位 为空则表示没牌了

            //将要在主线程执行的处理代码
            Action action = () =>
            {
                endByWs(winList, Convert.ToInt32(strs[1]), Convert.ToInt32(strs[2]));
            };

            //将任务添加到主线程任务队列中等待执行
            MainThreadDispatcher.Enqueue(action);
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Start)) //牌局开始
        {
            //strs[0] realUserDiceSide ｜ strs[1] currentActivityDiceSide ｜ strs[2] dictUsers ｜ strs[3] dictWhole ｜ strs[4] isOnlineSide | strs[5] isOnChi$isOnRobotHu

            string[] strs = msgs.message.Split('|');
            realUserDiceSide = Convert.ToInt32(strs[0]);
            print("当前用户在牌桌上的位置：" + getDeskViewSide(realUserDiceSide));
            print("随机掷骰子(谁最先出牌 庄家),方位：" + Convert.ToInt32(strs[1]));

            print("realUserDiceSide = " + strs[0]);           //当前用户真实的方位
            //print("currentActivityDiceSide = " + strs[1]);  //当前庄家的方位 / 随机掷骰子(谁最先出牌 庄家)
            //print("dictUsers = " + strs[2]);                //各个用户抓到的13张牌
            //print("dictWhole = " + strs[3]);                //整副牌
            //print("isOnlineSide = " + strs[4]);             //真实在线的方位

            // try
            // {
                string[] settings = strs[5].Split('$');
                isOnChi = int.Parse(settings[0]);
                isOnRobotHu = int.Parse(settings[1]);
                winRandomDiceSide = int.Parse(settings[2]);
            // } catch (Exception ex)
            // {
            //     print("msgs.message = " + msgs.message);

            //     print("牌局开始，解析数据异常：" + ex.Message);
            // }

            

            print(msgs.message + "!!!!!!");

            log("真实在线的方位" + strs[4]);
            isOnlineSides = strs[4].Split('#').Select(str => int.Parse(str)).ToArray();

            string notOnlineSides = "";
            if (!isOnlineSides.Contains(1))
            {
                notOnlineSides += "1,";
            }
            if (!isOnlineSides.Contains(2))
            {
                notOnlineSides += "2,";
            }
            if (!isOnlineSides.Contains(3))
            {
                notOnlineSides += "3,";
            }
            if (!isOnlineSides.Contains(4))
            {
                notOnlineSides += "4,";
            }
            if(notOnlineSides != "")
            {
                isNotOnlineSides = notOnlineSides.TrimEnd(',').Split(',').Select(str => int.Parse(str)).ToArray();
            } else
            {
                isNotOnlineSides = new int[] { };
            }
            
            //print("===");

            var dictWholeServer = JsonConvert.DeserializeObject<List<Card>>(strs[3]);
            int index = 0;
            foreach (var item in dictWholeServer)
            {
                dictWhole.Add(index, item);
                ++index;
            }

            print("ok 牌局开始======================");
         
            MainThreadDispatcher.Enqueue(() =>
            {
                beginByWs(Convert.ToInt32(strs[1]), strs[2]);
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.UserGrab)) //用户/机器人 抓牌
        {
            string[] strs = msgs.message.Split('|'); //strs[0] deskViewDiceSide ｜ strs[1] realUserDiceSide | strs[2] keyGrab |  strs[3] isAngangOrGang
            print("接收到：用户/机器人 抓牌，抓牌方牌桌方位" + getDeskViewSide(Convert.ToInt32(strs[0])) + " isAngangOrGang=" + strs[3] + "发消息方真实方位" + strs[1] + "牌" + dictWhole[Convert.ToInt32(strs[2])].ToString());

            MainThreadDispatcher.Enqueue(() =>
            {
                if (realUserDiceSide == Convert.ToInt32(strs[0])) //当前用户抓牌
                {
                    currentActivityDiceSide = Convert.ToInt32(Side.East);
                    avatarStyle();
                    userGrabCardByWs(Convert.ToInt32(strs[2]));
                }
                else  //模拟机器人抓牌
                {
                    robotGrabCardByWs(Convert.ToInt32(strs[0]), Convert.ToInt32(strs[2]));
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.UserKnock)) //用户 出牌
        {
            string[] strs = msgs.message.Split('|');   //strs[0] deskViewDiceSide ｜ strs[1] realUserDiceSide | strs[2] keyKnock
            print("接收到：用户出牌，在牌桌上" + getDeskViewSide(Convert.ToInt32(strs[0])) + "发消息用户真实方位" + strs[1] + "牌" + dictWhole[Convert.ToInt32(strs[2])].ToString());

            MainThreadDispatcher.Enqueue(() =>
            {
                if (realUserDiceSide == Convert.ToInt32(strs[0])) //当前用户出牌
                {
                    currentUserKnockByWs(Convert.ToInt32(strs[0]), strs[2]);
                }
                else  //模拟机器人抓牌
                {
                    currentRobotKnockByWs(Convert.ToInt32(strs[0]), strs[2]);
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Operate)) //用户 操作
        {
            string[] strs = msgs.message.Split('|');   //strs[0] deskViewDiceSide ｜ strs[1] realUserDiceSide(当前一直是0)
            print("接收到：用户操作，在牌桌上" + getDeskViewSide(Convert.ToInt32(strs[0])) + "发消息用户真实方位" + strs[1]);

            MainThreadDispatcher.Enqueue(() =>
            {
                if (realUserDiceSide == Convert.ToInt32(strs[0])) //当前用户操作
                {
                    currentUserOperateByWs(Convert.ToInt32(strs[0]));
                }
                else  //模拟机器人操作 (房主才会收到)
                {
                    print("模拟机器人操作 (房主才会收到)");
                    otherRobotHandleByWs(Convert.ToInt32(strs[0]));
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.AnGang) //用户 暗杠
            || Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Gang)     //用户 杠
            || Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Peng)     //用户 碰
            || Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Chi)      //用户 吃
            )
        {
            string strType = "";
            if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.AnGang))
            {
                strType = "暗杠";
            }
            else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Gang))
            {
                strType = "杠";
            }
            else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Peng))
            {
                strType = "碰";
            }
            else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Chi))
            {
                strType = "吃";
            }
            string[] strs = msgs.message.Split('|');   //strs[0] deskViewDiceSide ｜ strs[1] realUserDiceSide | strs[2] keyKnock

            MainThreadDispatcher.Enqueue(() =>
            {
                if (realUserDiceSide == Convert.ToInt32(strs[0])) //当前用户暗杠/杠/碰
                {
                    print("接收到： 当前用户" + strType + "，在牌桌上" + getDeskViewSide(Convert.ToInt32(strs[0])) + "发消息用户真实方位" + strs[1] + "牌" + dictWhole[Convert.ToInt32(strs[2])].ToString());

                    userAddCardByWs(Convert.ToInt32(msgs.type), Convert.ToInt32(strs[2]), strs[3]);
                }
                else  //模拟机器人暗杠/杠/碰
                {
                    print("接收到：模拟机器人" + strType + "，在牌桌上" + getDeskViewSide(Convert.ToInt32(strs[0])) + "发消息用户真实方位" + strs[1] + "牌" + dictWhole[Convert.ToInt32(strs[2])].ToString());

                    currentRobotAddCardByWs(Convert.ToInt32(strs[0]), Convert.ToInt32(msgs.type), Convert.ToInt32(strs[2]), strs[3]);
                }
            });
        }
        else if (Convert.ToInt32(msgs.type) == Convert.ToInt32(MessageType.Next)) //轮到下一个 （只有房主才会收到）
        {
            print("接收到：轮到下一个 （只有房主才会收到）");

            MainThreadDispatcher.Enqueue(() =>
            {
                int deskViewDiceSide = (currentActivityDiceSide + gapDiceSide) > 4 ? (currentActivityDiceSide + gapDiceSide) % 4 : (currentActivityDiceSide + gapDiceSide);

                nextByWs(deskViewDiceSide);
            });
        }
    }

    void OnClose(object sender, CloseEventArgs e)
    {
        print("连接已关闭，状态码：" + e.Code + "，原因：" + e.Reason);

        MainThreadDispatcher.Enqueue(() =>
        {
            //string strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
            PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), e.Reason);
            //print("OnClose 。。。");

            Time.timeScale = 1; //游戏重新开始
            SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景(回到选择页去填写房间号)
        });
        print("OnClose end");
    }

    void OnError(object sender, ErrorEventArgs e)
    {
        print("连接发生错误：" + e.Message);

        MainThreadDispatcher.Enqueue(() =>
        {
            //string strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
            //PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + " OnError()" + e.Message);
            //print("OnError 。。。");

            Time.timeScale = 1; //游戏重新开始
            SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景(回到选择页去填写房间号)
        });
        print("OnError end");

    }

    private void wsSend(int msgType, string msg = "")
    {
        if (ws != null && ws.IsAlive)
        {
            retryTimes = 0;
            canvasMain.transform.Find("loading").gameObject.SetActive(false);
            ws.Send(JsonConvert.SerializeObject(new SendMessage(msgType.ToString(), groupNum, deviceUniqueId, msg)));
            //print("已发送" + JsonConvert.SerializeObject(new SendMessage(msgType.ToString(), groupNum, deviceUniqueId, msg)));
        }
        else
        {
            print("连接丢失了 retryTimes= " + retryTimes);
            if (retryTimes >= 20)
            {
                string strTip = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString(), "");
                PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), strTip + " wsSend() Error 连接丢失！！！");

                Time.timeScale = 1; //游戏重新开始
                SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景(回到选择页去填写房间号)
            }
            else
            {
                ++retryTimes;
                canvasMain.transform.Find("loading").gameObject.SetActive(true);
                wsSend(msgType, msg);
            }
        }
    }
    #endregion

    //在牌桌上看到的“东南西北”方位
    private string getDeskViewSide(int side)
    {
        return " " + (side == 1 ? "东" : (side == 2 ? "南" : (side == 3 ? "西" : "北"))) + " ";
    }

}