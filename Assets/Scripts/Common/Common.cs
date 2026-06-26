namespace zengyanqiCommon
{

    //场景枚举
    public enum ScenesSelect
    {
        Start = 0,       //开始页
        Select = 1,      //选择面板（填写房间号）
        Main = 2,        //单机版
        MainOnline = 3,  //联网版
    }

    //全局字段枚举
    public enum PlayerPrefsKey
    {
        Type = 0,             //类型 1-创建房间  2-加入房间
        GroupNum = 1,
        DeviceUniqueId = 2,
        Tip = 3,
        isOnChi = 4,          //允许 “吃” 牌
        isOnRobotHu = 5       //允许机器人 “胡”
    }

    public enum MessageType
    {
        Fail = -1,     //失败退出
        Connect = 0,   //连接成功
        Prepare = 1,   //准备
        End = 2,       //牌局结束（有人胡了/没牌了）
        Start = 3,     //牌局开始
        UserGrab = 4,  //用户抓牌
        UserKnock = 5, //用户出牌
        Pass = 6,      //过
        Chi = 7,       //吃
        Peng = 8,      //碰
        Gang = 9,      //明杠
        AnGang = 10,   //暗杠
        Operate = 11,  //等待用户操作
        Next = 12,     //轮到下一个
    }

    public enum OnlineSide
    {
        East = 1,   //东
        Sorth = 2,  //南
        West = 3,   //西
        North = 4,  //北
    }

    public class ReceiveMessage
    {
        private string Type;

        public string type            //类型
        {
            get { return Type; }
            set { Type = value; }
        }

        private string Message;

        public string message         //消息内容
        {
            get { return Message; }
            set { Message = value; }
        }

        private string Date;

        public string date             //时间
        {
            get { return Date; }
            set { Date = value; }
        }

        public ReceiveMessage(string Type, string Message, string Date)
        {
            type = Type;
            message = Message;
            date = Date;
        }
    }

    public class SendMessage
    {
        private string Type;

        public string type               //类型
        {
            get { return Type; }
            set { Type = value; }
        }

        private string Group_num;

        public string group_num          //牌桌号
        {
            get { return Group_num; }
            set { Group_num = value; }
        }
        private string Device_unique_id;

        public string device_unique_id   //设备id
        {
            get { return Device_unique_id; }
            set { Device_unique_id = value; }
        }
        private string Message;
        public string message            //消息内容
        {
            get { return Message; }
            set { Message = value; }
        }


        public SendMessage(string Type, string groupNum, string deviceUniqueId, string Message)
        {
            type = Type;
            group_num = groupNum;
            device_unique_id = deviceUniqueId;
            message = Message;
        }
    }

}
