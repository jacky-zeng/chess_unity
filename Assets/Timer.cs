using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    //开始时，剩余的时间
    [HideInInspector]
    public float timeLeft;

    private int oldIntTimeLeft = 0;

    //是否开始
    [HideInInspector]
    public bool isBegin = false;

    //显示秒的组件
    public GameObject second;

    //秒的贴图
    public Sprite[] sp;

    void Start()
    {

    }

    void Update()
    {
        if (isBegin)
        {
            oldIntTimeLeft = (int)System.Math.Floor(timeLeft);
            timeLeft -= Time.deltaTime;
            int newIntTimeLeft = (int)System.Math.Floor(timeLeft);

            if(oldIntTimeLeft != newIntTimeLeft && newIntTimeLeft <= 5) //说明秒发生了变化且剩余5秒，则播放秒钟音效
            {
                AudioSource audioSource = gameObject.GetComponent<AudioSource>();
                audioSource.Play();
            }

            change(newIntTimeLeft);
        }
    }

    public void begin(float timeLeftIn)
    {
        timeLeft = timeLeftIn;
        isBegin = true;
    }

    public void end()
    {
        isBegin = false;
    }

    private void change(int num)
    {
        if (num < 0) //倒计时结束
        {
            if (isBegin) //调用按钮“过”
            {
                MainOnlineManager._instance.btnPassClick();
            }
            isBegin = false;
        }
        else
        {
            (second.GetComponent<Image>()).sprite = sp[num];
        }
    }
}
