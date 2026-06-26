namespace zengyanqiCard
{
    public class Card
    {
        private int number;

        public int Number       //牌的点数
        {
            get { return number; }
            set { number = value; }
        }
        private int cardType;

        public int CardType     //筒 条 万 东南西北中发白
        {
            get { return cardType; }
            set { cardType = value; }
        }

        private int tagType;    //类型：正常，吃，碰，杠

        public int TagType
        {
            get { return tagType; }
            set { tagType = value; }
        }

        public int userType;    //牌在哪：0-在牌库中  1-在用户手中  2-牌已打出

        public int UserType
        {
            get { return userType; }
            set { userType = value; }
        }

        public int hu;          //是否靠这张牌胡的：0-否  1-是

        public int Hu
        {
            get { return hu; }
            set { hu = value; }
        }


        public Card(int num, int ct, int tt = 0, int ut = 0, int huhu = 0)
        {
            number = num;
            cardType = ct;
            tagType = tt;
            userType = ut;
            hu = huhu;
        }

        //重写ToString方法
        public override string ToString()
        {
            string str = "";
            if (this.CardType == 0)
            {
                str = (this.number) + "筒";
            }
            else if (this.CardType == 1)
            {
                str = (this.number) + "条";
            }
            else if (this.CardType == 2)
            {
                str = (this.number) + "万";
            }
            else if (this.CardType == 3)
            {
                if (this.number == 1)
                {
                    str = "东";
                }
                else if (this.number == 2)
                {
                    str = "南";
                }
                else if (this.number == 3)
                {
                    str = "西";
                }
                else if (this.number == 4)
                {
                    str = "北";
                }
            }
            else if (this.CardType == 4)
            {
                if (this.number == 1)
                {
                    str = "中";
                }
                else if (this.number == 2)
                {
                    str = "发";
                }
                else if (this.number == 3)
                {
                    str = "白";
                }
            }

            if (this.TagType == 0)
            {
                str += "";
            }
            else if (this.TagType == 1)
            {
                str += "（吃）";
            }
            else if (this.TagType == 2)
            {
                str += "（碰）";
            }
            else if (this.TagType == 3)
            {
                str += "（杠）";
            }
            else if (this.TagType == 4)
            {
                str += "（暗杠）";
            }

            if (this.UserType == 0)
            {
                str += "（牌库）";
            }
            else if (this.UserType == 1)
            {
                str += "（用户）";
            }
            else if (this.UserType == 2)
            {
                str += "（已打出）";
            }

            return "牌型：" + str;
        }
    }

    public enum CardType
    {
        Tong = 0,    //筒
        Tiao = 1,    //条
        Wan = 2,     //万
        Feng = 3,    //东南西北
        Zi = 4,      //中发白
    }

    public enum TagType
    {
        Normal = 0,   //正常（默认）
        Chi = 1,      //吃
        Peng = 2,     //碰
        Gang = 3,     //杠
        AnGang = 4,   //暗杠
    }

    public enum UserType
    {
        Normal = 0,   //在牌库中，不可见
        User = 1,     //在用户手中
        Out = 2,      //牌已打出
    }

    //是否靠这张牌胡
    public enum Hu
    {
        No = 0,      //否
        Yes = 1,     //是
    }
}
