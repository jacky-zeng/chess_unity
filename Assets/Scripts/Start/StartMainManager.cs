using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using zengyanqiCommon;

public class StartMainManager : MonoBehaviour
{
    //public int globalFrameRate = 12;       //帧 (每秒钟更新多少个画面)

    public GameObject canvas1;
    public GameObject canvas2;
    public GameObject canvas3;

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

    private void Awake()
    {
        //Application.targetFrameRate = globalFrameRate;  //设置帧率

        FitCamera(Camera.main);
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
        Screen.orientation = ScreenOrientation.AutoRotation;//设置方向为自动(根据需要自动旋转屏幕朝向任何启用的方向。)
        Screen.autorotateToLandscapeRight = true;           //允许自动旋转到右横屏
        Screen.autorotateToLandscapeLeft = true;            //允许自动旋转到左横屏
        Screen.autorotateToPortrait = false;                //不允许自动旋转到纵向
        Screen.autorotateToPortraitUpsideDown = false;      //不允许自动旋转到纵向上下
        Screen.sleepTimeout = SleepTimeout.NeverSleep;      //睡眠时间为从不睡眠

        PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), "");

        //随机展示封面
        int randomIndex = (new System.Random()).Next(1, 4);
        if (randomIndex == 1)
        {
            canvas1.SetActive(true);
            canvas2.SetActive(false);
            canvas3.SetActive(false);
        }
        else if (randomIndex == 2)
        {
            canvas1.SetActive(false);
            canvas2.SetActive(true);
            canvas3.SetActive(false);
        }
        else if (randomIndex == 3)
        {
            canvas1.SetActive(false);
            canvas2.SetActive(false);
            canvas3.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void btnStartClick()
    {
        SceneManager.LoadScene(ScenesSelect.Select.ToString(), LoadSceneMode.Single); //加载场景
    }

    public void btnCloseClick()
    {
        #if UNITY_EDITOR //编辑器中退出游戏
        UnityEditor.EditorApplication.isPlaying = false;
        #else //应用程序中退出游戏
	    UnityEngine.Application.Quit();
        #endif
    }
}
