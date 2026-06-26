using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using zengyanqiCommon;

public class SelectMainManager : MonoBehaviour
{
    //牌桌房间号
    public GameObject txtGroupNum;

    //用户设备id
    public GameObject txtDeviceUniqueId;

    //提示语
    public GameObject txtTip;

    private void Awake()
    {
        #if UNITY_EDITOR
            //Debug.Log("现在是编辑器");
            txtDeviceUniqueId.SetActive(true);
        #endif
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

        //txtGroupNum.gameObject.GetComponent<InputField>().ActivateInputField();

        //txtGroupNum.gameObject.GetComponent<InputField>().onValueChanged.AddListener(OnInputValueChange);

        txtDeviceUniqueId.gameObject.GetComponent<InputField>().text = SystemInfo.deviceUniqueIdentifier;

        txtTip.gameObject.GetComponent<Text>().text = PlayerPrefs.GetString(PlayerPrefsKey.Tip.ToString());
        PlayerPrefs.SetString(PlayerPrefsKey.Tip.ToString(), "");

        PlayerPrefs.SetInt(PlayerPrefsKey.isOnChi.ToString(), 1);
        PlayerPrefs.SetInt(PlayerPrefsKey.isOnRobotHu.ToString(), 1); 
    }

    // Update is called once per frame
    void Update()
    {

    }

    //// 输入变化事件处理函数
    //void OnInputValueChange(string value)
    //{
    //    Debug.Log("User input: " + value);
    //}

    public void btnStartClick()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey.Type.ToString(), 1);

        begin();
    }

    public void btnJoinClick()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey.Type.ToString(), 2);

        begin();
    }

    private void begin()
    {
        string groupNum = txtGroupNum.gameObject.GetComponent<InputField>().text.Trim();
        string deviceUniqueId = txtDeviceUniqueId.gameObject.GetComponent<InputField>().text.Trim();

        #if UNITY_EDITOR
        //Debug.Log("现在是编辑器");
        deviceUniqueId = "C" + deviceUniqueId;
        #endif

        print(deviceUniqueId + "|" + groupNum);

        if (groupNum.Length == 0)
        {
            txtTip.gameObject.GetComponent<Text>().text = "请填写房间号";
            //SceneManager.LoadScene(ScenesSelect.Main.ToString(), LoadSceneMode.Single);       //加载场景(单机版)
        }
        else
        {
            PlayerPrefs.SetString(PlayerPrefsKey.GroupNum.ToString(), groupNum);
            PlayerPrefs.SetString(PlayerPrefsKey.DeviceUniqueId.ToString(), deviceUniqueId);

            SceneManager.LoadScene(ScenesSelect.MainOnline.ToString(), LoadSceneMode.Single); //加载场景（联网版）
        }
    }

    public void btnCloseClick()
    {
        Time.timeScale = 1; //游戏重新开始
        SceneManager.LoadScene(ScenesSelect.Start.ToString(), LoadSceneMode.Single); //加载场景(回到首页)
    }
}