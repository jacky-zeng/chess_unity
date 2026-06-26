using System;
using System.Collections.Generic;
using System.IO;
using zengyanqiCard;
/*
 //var msgs = Regex.Split(e.Data, @"{-\$☋\$-}", RegexOptions.IgnoreCase);
 */
/*
 宜春麻将是流行于江西省宜春市的特色麻将，4人同时玩，该麻将玩法休闲而又刺激，技巧性大。牌面包括一到九万，一到九筒，一到九条，东南西北，中发白各四个，共计136张。
通过吃牌，碰牌，杠牌等方式，使手牌满足相关规定的牌型条件胡牌。宜春麻将的特色就是(买，飘）买：所有收益X2，损失也X2。飘：在原有番数基础上累加，收益增加，损失也
增加。宜春麻将黄庄不计算本局杠子的分，可以点炮胡。宜春麻将比较刺激，算分也比较简单，宜春人都爱玩！

一、基本规则

宜春麻将使用不带花牌的136张牌，四人游戏，可碰，可杠，可吃，字牌也可吃，可放炮胡，支持一炮多响，放冲独付，自摸三家给，有大七对、小七对、门清、清一色、字一色、
十三烂、天胡、地胡等计番牌型。

二、游戏结算

1、结束标志：有玩家胡牌或者抓完所有的牌。

2、黄庄： 抓完所有的牌。

3、庄家轮换：首局系统随机，以后一般谁胡谁坐庄，一炮多响后由放炮的人坐庄；

4、积分：积分=基础分×总分

5、总分=胡牌分+杠分；

6、杠分：明杠每人给1分，暗杠每人给2分。

7、银两：基础银×总分；

三、胡牌牌型

1、平胡：同一花色的牌任意3连张为1阙牌，东南西北风任意不同的3张为1阙牌，中发白为1阙牌，任意3张相同的牌为1阙牌。手中有4阙牌＋1对头（2张相同的任意牌）即可胡牌。
胡牌分=1分

2、抢杠：胡别人补碰杠的牌，只有杠牌的人扣分2番，胡牌分×2

3、杠上炮：杠了牌后再打出一张牌时放炮2番，胡牌分×2；

4、另算放炮的番

（1）杠开：开杠（碰杠，明杠，暗杠都可以），补张后胡牌，三家都要给2番，胡牌分×2

（2）门清：没吃没碰没明杠就胡牌，只有暗杠还可算是门清；天胡，地胡，小七对，烂胡都不再计算门清；只有自摸才有门清，放炮不算门清。2番，胡牌分×2

（3）单钓：手上只有1张牌，且无暗杠时胡牌2番，胡牌分×2

（4）大七对：所有的牌为三同张（或杠）加1对头,；即碰碰胡。3番，胡牌分×3

（5）清一色：所有的牌全是同一花色5番，胡牌分×5

（6）十三烂：胡牌时手上没有相同的牌，而且万筒条中相同花色的牌点数间隔2或以上；2番，胡牌分×2

（7）七星十三烂：有7个不同的字的十三烂3番，胡牌分×3

小七对：胡牌时为7个对子4番，胡牌分×4

（8）豪华小七对：豪华小七对中有4张相同的牌4×2n番，胡牌分×4×2n,n为牌中有四同张的个数

（9）字一色：所有的牌全是字，可以不成胡牌牌型；如果还成胡牌牌型分数加一倍。6番，胡牌分×6；12番（成胡牌牌型），胡牌分 ×12

（10）天胡：庄家起手就胡牌12番，胡牌分×12

（11）地胡：闲家胡庄家打的第一个牌12番，胡牌分×12
 */

namespace zengyanqiHelper
{
    public class Helper
    {
        public static int[][] initCards(Card[] cards)
        {
            //手牌
            int[][] handcards = new int[5][] {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   //筒  (第一个0：当前类型牌的总数，其他的0表示：一万 二万 ... 九万 的数量)
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   //条
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   //万
            new int[] { 0, 0, 0, 0, 0},                   //东南西北
            new int[] { 0, 0, 0, 0}                       //中发白
        };

            for (int i = 0; i < cards.Length; i++)
            {
                //CardType为0,1,2,3,4则为 筒,条,万,"东南西北","中发白"。   [CardType = 0,1,2,3,4]代表每种牌型的总数量
                switch (cards[i].CardType)
                {
                    case 0:
                        handcards[0][cards[i].Number]++;  //这个大小的牌，有几个
                        handcards[0][0]++;                //代表存在这个牌型
                        break;
                    case 1:
                        handcards[1][cards[i].Number]++;
                        handcards[1][0]++;
                        break;
                    case 2:
                        handcards[2][cards[i].Number]++;
                        handcards[2][0]++;
                        break;
                    case 3:
                        handcards[3][cards[i].Number]++;
                        handcards[3][0]++;
                        break;
                    case 4:
                        handcards[4][cards[i].Number]++;
                        handcards[4][0]++;
                        break;
                }
            }
            return handcards;
        }

        //碰
        public static bool canKan(Card[] cards, Card card)
        {
            int cardType = card.CardType;
            int[][] handcards = initCards(cards);

            if (handcards[cardType][card.Number] == 2)  //2张相同的牌 加上后 刚好3张
            {
                AddTxtTextByFileInfo("判断是否可以碰 可以" + card.ToString());
                return true;

            }

            return false;
        }

        //吃
        public static Dictionary<int, Dictionary<int, bool>> canShun(Card[] cards, Card card)
        {
            int cardType = card.CardType;
            int[][] handcards = initCards(cards);
            /*
             * 数据结构如下：
             index => [3,false]                             [4,true]      [5,true]
             说明：    [牌的点数, false-外部的牌 true-自己的牌]
             */
            Dictionary<int, Dictionary<int, bool>> dictShun = new Dictionary<int, Dictionary<int, bool>>();
            Dictionary<int, bool> temp = new Dictionary<int, bool>();
            int index = 0;
            if (handcards[cardType][0] >= 2) //这个牌型至少有2张牌才能吃
            {
                if (cardType <= 2)      //筒条万
                {
                    if (card.Number - 2 > 0 && handcards[cardType][card.Number - 2] > 0 && handcards[cardType][card.Number - 1] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(card.Number - 2, true);
                        temp.Add(card.Number - 1, true);
                        temp.Add(card.Number, false);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                    if (card.Number <= 8 && card.Number - 1 > 0 && handcards[cardType][card.Number - 1] > 0 && handcards[cardType][card.Number + 1] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(card.Number - 1, true);
                        temp.Add(card.Number, false);
                        temp.Add(card.Number + 1, true);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                    if (card.Number + 2 <= 9 && handcards[cardType][card.Number + 1] > 0 && handcards[cardType][card.Number + 2] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(card.Number, false);
                        temp.Add(card.Number + 1, true);
                        temp.Add(card.Number + 2, true);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                }
                else if (cardType == 3) //东南西北
                {
                    if (card.Number == 1)
                    {
                        if ((handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 3)
                        {
                            temp = new Dictionary<int, bool>();
                            temp.Add(1, false);
                            temp.Add(2, true);
                            temp.Add(3, true);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(1, false);
                            temp.Add(2, true);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(1, false);
                            temp.Add(3, true);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;
                        }
                        else if ((handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 2)
                        {
                            if (handcards[cardType][2] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, false);
                                temp.Add(3, true);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][3] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, false);
                                temp.Add(2, true);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][4] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, false);
                                temp.Add(2, true);
                                temp.Add(3, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                        }
                    }
                    else if (card.Number == 2)
                    {
                        if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 3)
                        {
                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(2, false);
                            temp.Add(3, true);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(2, false);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(2, false);
                            temp.Add(3, true);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;
                        }
                        else if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 2)
                        {
                            if (handcards[cardType][1] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(2, false);
                                temp.Add(3, true);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][3] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(2, false);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][4] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(2, false);
                                temp.Add(3, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                        }
                    }
                    else if (card.Number == 3)
                    {
                        if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 3)
                        {
                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(2, true);
                            temp.Add(3, false);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(3, false);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(2, true);
                            temp.Add(3, false);
                            temp.Add(4, true);
                            dictShun.Add(index, temp);
                            ++index;
                        }
                        else if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][4] > 0 ? 1 : 0) == 2)
                        {
                            if (handcards[cardType][1] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(2, true);
                                temp.Add(3, false);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][2] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(3, false);
                                temp.Add(4, true);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][4] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(2, true);
                                temp.Add(3, false);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                        }
                    }
                    else if (card.Number == 4)
                    {
                        if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) == 3)
                        {
                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(2, true);
                            temp.Add(4, false);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(2, true);
                            temp.Add(3, true);
                            temp.Add(4, false);
                            dictShun.Add(index, temp);
                            ++index;

                            temp = new Dictionary<int, bool>();
                            temp.Add(1, true);
                            temp.Add(3, true);
                            temp.Add(4, false);
                            dictShun.Add(index, temp);
                            ++index;
                        }
                        else if ((handcards[cardType][1] > 0 ? 1 : 0) + (handcards[cardType][2] > 0 ? 1 : 0) + (handcards[cardType][3] > 0 ? 1 : 0) == 2)
                        {
                            if (handcards[cardType][1] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(2, true);
                                temp.Add(3, true);
                                temp.Add(4, false);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][2] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(3, true);
                                temp.Add(4, false);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                            else if (handcards[cardType][3] == 0)
                            {
                                temp = new Dictionary<int, bool>();
                                temp.Add(1, true);
                                temp.Add(2, true);
                                temp.Add(4, false);
                                dictShun.Add(index, temp);
                                ++index;
                            }
                        }
                    }
                }
                else if (cardType == 4) //中发白
                {
                    if (card.Number == 1 && handcards[cardType][2] > 0 && handcards[cardType][3] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(1, false);
                        temp.Add(2, true);
                        temp.Add(3, true);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                    else if (card.Number == 2 && handcards[cardType][1] > 0 && handcards[cardType][3] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(1, true);
                        temp.Add(2, false);
                        temp.Add(3, true);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                    else if (card.Number == 3 && handcards[cardType][1] > 0 && handcards[cardType][2] > 0)
                    {
                        temp = new Dictionary<int, bool>();
                        temp.Add(1, true);
                        temp.Add(2, true);
                        temp.Add(3, false);
                        dictShun.Add(index, temp);
                        ++index;
                    }
                }
            }

            return dictShun;
        }

        //暗杠
        public static List<Card> canAnGangList(Card[] cards)
        {
            int[][] handcards = initCards(cards);
            List<Card> ganList = new List<Card>();
            for (int i = 0; i < handcards.GetLength(0); i++)
            {
                AddTxtTextByFileInfo("判断暗杠 i=" + i + " | handcards[i][0]=" + handcards[i][0]);
                if (handcards[i][0] >= 4) //这个牌型至少有4张牌
                {
                    for (int j = 1; j < handcards[i].Length; j++)
                    {
                        if (handcards[i][j] == 4)  //4张相同的牌
                        {
                            AddTxtTextByFileInfo("判断暗杠 4张相同的牌 i=" + i + " | j=" + j);
                            ganList.Add(new Card(j, i));
                        }
                    }
                }
            }
            return ganList;
        }

        //公杠
        public static bool canGang(Card[] cards, Card card)
        {
            int cardType = card.CardType;
            int[][] handcards = initCards(cards);

            if (handcards[cardType][card.Number] == 3)  //3张相同的牌 加上后 刚好4张
            {
                AddTxtTextByFileInfo("判断是否可以公杠 可以" + card.ToString());
                return true;

            }

            return false;
        }

        //是否可以胡了
        public static bool isWin(Card[] cards)
        {
            int[][] handcards = initCards(cards);

            int countCards = cards.Length;
            //判断是否是七小对
            if (countCards == 14)
            {
                int count = 0;
                for (int i = 0; i < handcards.GetLength(0); i++)
                {
                    if (handcards[i][0] > 0)
                    {
                        for (int j = 1; j < handcards[i].Length; j++)
                        {
                            count += (handcards[i][j] % 2 == 0 ? handcards[i][j] / 2 : 0);
                        }
                    }
                }
                AddTxtTextByFileInfo("7小对？  count=" + count);
                if (count == 7)
                {
                    AddTxtTextByFileInfo("7小对");
                    return true;
                }
            }

            //判断是否是十三烂
            List<int> coupleIndexs = getCoupleIndexs(handcards);

            if (is13Lan(handcards, coupleIndexs)) //满足13烂
            {
                AddTxtTextByFileInfo("13烂");
                return true;
            }

            //判断是否13张牌都是字
            if (handcards[3][0] == 13)
            {
                AddTxtTextByFileInfo("字一色");
                return true;
            }

            foreach (int coupleIndex in coupleIndexs)
            {
                AddTxtTextByFileInfo("");
                AddTxtTextByFileInfo("=============================");
                AddTxtTextByFileInfo("coupleIndex= " + coupleIndex);
                //throw new Exception("coupleIndex = " + coupleIndex + "|" + handcards.GetLength(0));

                //循环下面的，即使多个对子也能判断

                AddTxtTextByFileInfo("除去对子所在的牌型，其他牌型是不是满足刻字或者砍");
                bool isBreak = false;
                //除去对子所在的牌型，其他牌型是不是满足刻字或者砍
                for (int i = 0; i < handcards.GetLength(0); i++)
                {
                    if (i != coupleIndex)
                    {
                        if (i == 3) //东南西北
                        {
                            if (!IsKanOrShun(handcards[i], i) && !IsKanOrShunForFeng(handcards[i]))
                            {
                                isBreak = true;
                                break;
                            }
                        }
                        else
                        {
                            if (!IsKanOrShun(handcards[i], i))
                            {
                                isBreak = true;
                                break;
                            }
                        }
                    }
                }

                if (isBreak)
                {
                    continue;
                }

                AddTxtTextByFileInfo("再来分析含对子的类型牌 （除去对子后，其他牌是否满足 砍或顺）");

                bool success = false;
                //再来分析含对子的类型牌 （除去对子后，其他牌是否满足 砍或顺）
                for (int i = 1; i <= 9; i++)
                {
                    if ((coupleIndex == 3 && i > 4) || (coupleIndex == 4 && i > 3))
                    {
                        continue;
                    }
                    if (handcards[coupleIndex][i] >= 2)   //对子所在的类型牌
                    {
                        handcards[coupleIndex][i] -= 2;   //模拟去除这个对子 （对子牌-2）
                        handcards[coupleIndex][0] -= 2;   //模拟去除这个对子 （所在类型牌总数-2）
                        AddTxtTextByFileInfo("对子类型 " + coupleIndex + " | 对子点数 " + i);

                        if (coupleIndex == 3)  //东南西北
                        {
                            if (IsKanOrShun(handcards[coupleIndex], coupleIndex)) //对去除了对子的剩余牌进行判断
                            {
                                AddTxtTextByFileInfo("成功了 coupleIndex=" + coupleIndex + " | i=" + i);
                                success = true;
                                break;
                            }
                            else if (IsKanOrShunForFeng(handcards[coupleIndex]))
                            {
                                AddTxtTextByFileInfo("成功了 coupleIndex=" + coupleIndex + " | i=" + i);
                                success = true;
                                break;
                            }
                            else
                            {
                                handcards[coupleIndex][i] += 2; //因为前面模拟去除了，得加回来
                                handcards[coupleIndex][0] += 2; //因为前面模拟去除了，得加回来
                            }
                        }
                        else
                        {
                            if (IsKanOrShun(handcards[coupleIndex], coupleIndex)) //对去除了对子的剩余牌进行判断
                            {
                                AddTxtTextByFileInfo("成功了 coupleIndex=" + coupleIndex + " | i=" + i);
                                success = true;
                                break;
                            }
                            else
                            {
                                handcards[coupleIndex][i] += 2; //因为前面模拟去除了，得加回来
                                handcards[coupleIndex][0] += 2; //因为前面模拟去除了，得加回来
                            }
                        }
                    }
                }
                return success;
            }

            return false;
        }

        //是否十三烂（手中十四张牌中，序数牌间隔大于2 ，字牌没有重复）
        public static bool is13Lan(int[][] handcards, List<int> coupleIndexs)
        {
            //不能有对子
            if (coupleIndexs.Count != 0)
            {
                return false;
            }

            //必须是14张手牌
            if (handcards[0][0] + handcards[1][0] + handcards[2][0] + handcards[3][0] + handcards[4][0] != 14)
            {
                return false;
            }

            //先判断没有对子
            for (int i = 0; i < 3; i++)   // 0筒 1条 2万
            {
                if (handcards[i][0] > 0)
                {
                    for (int j = 1; j < handcards[i].Length - 2; j++)
                    {
                        if (j == 7 && handcards[i][j + 1] > 0 && handcards[i][j + 2] > 0) //判断一下8和9连续
                        {
                            return false;
                        }
                        if (handcards[i][j] > 0 && (handcards[i][j + 1] > 0 || handcards[i][j + 2] > 0))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //获取所有对子所在的牌类型
        public static List<int> getCoupleIndexs(int[][] handcards)
        {
            List<int> coupleIndexs = new List<int>();

            for (int i = 0; i < handcards.GetLength(0); i++)
            {
                if (handcards[i][0] != 0)
                {
                    for (int j = 1; j <= 9; j++)
                    {
                        if (i == 3 && j > 4)
                        {
                            break;
                        }
                        if (i == 4 && j > 3)
                        {
                            break;
                        }
                        if (handcards[i][j] >= 2) //有一对
                        {
                            if (!coupleIndexs.Contains(i))
                            {
                                coupleIndexs.Add(i);
                            }
                        }
                    }
                }
            }

            return coupleIndexs;
        }

        //判断是否满足牌组为顺子或砍（三个一样）组成
        public static bool IsKanOrShun(int[] arr, int cardType)
        {
            AddTxtTextByFileInfo("IsKanOrShun cardType =" + cardType);

            if (arr[0] == 0)
            {
                AddTxtTextByFileInfo("没这种类型的牌");
                return true;   //说明没这种类型的牌
            }

            AddTxtTextByFileInfo("arr =" + string.Join(",", arr));  //注意：数组第一个表示牌的总数

            int index = -1;
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] > 0) //存在大小为i的牌
                {
                    index = i;
                    break;
                }
            }
            bool result;

            ///////////
            if (index == -1)
            {
                throw new Exception("index ====== -1");
            }
            ///////////

            //是否满足全是砍（三个一样）
            if (arr[index] >= 3) //说明是砍（三个一样），或者杠（四个一样）
            {
                arr[index] -= 3; //todo 如果是杠 是不是 -arr[index] ？？？
                arr[0] -= 3;
                result = IsKanOrShun(arr, cardType); //递归判断
                arr[index] += 3; //为什么要加3 ？ 因为前面模拟去除了，得加回来
                arr[0] += 3;     //为什么要加3 ？ 因为前面模拟去除了，得加回来
                AddTxtTextByFileInfo("判断砍 result =" + result + (result ? " 砍/杠：" + (new Card(index, cardType)) : ""));
                return result;
            }
            //是否满足为顺子
            if (cardType <= 2) //不是字
            {
                if (index < 8 && arr[index + 1] > 0 && arr[index + 2] > 0)  //存在三个连续的牌  todo index就不会等于-1 ？
                {
                    arr[index] -= 1;
                    arr[index + 1] -= 1;
                    arr[index + 2] -= 1;
                    arr[0] -= 3;
                    result = IsKanOrShun(arr, cardType);
                    arr[index] += 1;
                    arr[index + 1] += 1;
                    arr[index + 2] += 1;
                    arr[0] += 3;
                    AddTxtTextByFileInfo("不是字 result =" + result + (result ? " 顺子：" + (new Card(index, cardType, 0, 1)) + (new Card(index + 1, cardType, 0, 1)) + (new Card(index + 2, cardType, 0, 1)) : ""));
                    return result;
                }
            }
            else if (cardType == Convert.ToInt32(CardType.Feng)) //"东南西北"   注意：无需连续，有3个即可，也就是必须有3个不同的牌
            {
                AddTxtTextByFileInfo("判断风" + arr[index]);
                if (index == 1) //"东" 有值
                {
                    AddTxtTextByFileInfo("东有值" + arr[index]);
                    if (arr[index + 1] > 0)  //"南"有值
                    {
                        AddTxtTextByFileInfo("南有值" + arr[index + 1]);
                        if (arr[index + 2] > 0) //"西"有值
                        {
                            AddTxtTextByFileInfo("西有值" + arr[index + 2]);
                            arr[index] -= 1;
                            arr[index + 1] -= 1;
                            arr[index + 2] -= 1;
                            arr[0] -= 3;
                            result = IsKanOrShun(arr, cardType);
                            arr[index] += 1;
                            arr[index + 1] += 1;
                            arr[index + 2] += 1;
                            arr[0] += 3;
                            AddTxtTextByFileInfo("东南西 result =" + result + (result ? " 顺子：东南西" : ""));
                            return result;
                        }
                        else if (arr[index + 3] > 0) //"北"必须有值
                        {
                            AddTxtTextByFileInfo("北有值" + arr[index + 3]);
                            arr[index] -= 1;
                            arr[index + 1] -= 1;
                            arr[index + 3] -= 1;
                            arr[0] -= 3;
                            result = IsKanOrShun(arr, cardType);
                            arr[index] += 1;
                            arr[index + 1] += 1;
                            arr[index + 3] += 1;
                            arr[0] += 3;
                            AddTxtTextByFileInfo("东南北 result =" + result + (result ? " 顺子：东南北" : ""));
                            return result;
                        }
                        AddTxtTextByFileInfo("失败1");
                    }
                    else if (arr[index + 2] > 0 && arr[index + 3] > 0)  //"南"没值， 那必须“西北”有值
                    {
                        AddTxtTextByFileInfo("南没值,那必须“西北”有值" + arr[index + 2] + "|" + arr[index + 3]);
                        arr[index] -= 1;
                        arr[index + 2] -= 1;
                        arr[index + 3] -= 1;
                        arr[0] -= 3;
                        result = IsKanOrShun(arr, cardType);
                        arr[index] += 1;
                        arr[index + 2] += 1;
                        arr[index + 3] += 1;
                        arr[0] += 3;
                        AddTxtTextByFileInfo("东西北 result =" + result + (result ? " 顺子：东西北" : ""));
                        return result;
                    }
                    AddTxtTextByFileInfo("失败2");
                }
                else  //只剩 “南西北”
                {
                    AddTxtTextByFileInfo("没有东，只剩 “南西北”");
                    if (index == 2 && arr[index + 1] > 0 && arr[index + 2] > 0)  //"西北"必须2个都有值
                    {
                        arr[index] -= 1;
                        arr[index + 1] -= 1;
                        arr[index + 2] -= 1;
                        arr[0] -= 3;
                        result = IsKanOrShun(arr, cardType);
                        arr[index] += 1;
                        arr[index + 1] += 1;
                        arr[index + 2] += 1;
                        arr[0] += 3;
                        AddTxtTextByFileInfo("南西北 result =" + result + (result ? " 顺子：南西北" : ""));
                        return result;
                    }
                    AddTxtTextByFileInfo("失败3");
                }
            }
            else if (cardType == Convert.ToInt32(CardType.Zi))   //"中发白"
            {
                if (index == 1 && arr[index + 1] > 0 && arr[index + 2] > 0)  //“中发白”必须三个都有值
                {
                    arr[index] -= 1;
                    arr[index + 1] -= 1;
                    arr[index + 2] -= 1;
                    arr[0] -= 3;
                    result = IsKanOrShun(arr, cardType);
                    arr[index] += 1;
                    arr[index + 1] += 1;
                    arr[index + 2] += 1;
                    arr[0] += 3;
                    AddTxtTextByFileInfo("中发白 result =" + result + (result ? " 顺子：中发白" : ""));
                    return result;
                }
                AddTxtTextByFileInfo("失败4");
            }

            AddTxtTextByFileInfo("失败5");

            return false;
        }

        //判断是否满足牌组为顺子或砍（三个一样）组成（仅针对东南西北）
        public static bool IsKanOrShunForFeng(int[] arr)
        {
            AddTxtTextByFileInfo("IsKanOrShunForFeng");

            if (arr[0] == 0)
            {
                AddTxtTextByFileInfo("没这种类型的牌");
                return true;   //说明没这种类型的牌
            }

            AddTxtTextByFileInfo("arr =" + string.Join(",", arr));  //注意：数组第一个表示牌的总数

            int index = -1;
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] > 0) //存在大小为i的牌
                {
                    index = i;
                    break;
                }
            }
            bool result;

            ///////////
            if (index == -1)
            {
                throw new Exception("index ====== -1");
            }
            ///////////

            //是否满足全是砍（三个一样）
            if (arr[index] >= 3) //说明是砍（三个一样），或者杠（四个一样）
            {
                arr[index] -= 3; //todo 如果是杠 是不是 -arr[index] ？？？
                arr[0] -= 3;
                result = IsKanOrShunForFeng(arr); //递归判断
                arr[index] += 3; //为什么要加3 ？ 因为前面模拟去除了，得加回来
                arr[0] += 3;     //为什么要加3 ？ 因为前面模拟去除了，得加回来
                AddTxtTextByFileInfo("判断砍 result =" + result + (result ? " 砍/杠：" + (new Card(index, 3)) : ""));
                return result;
            }

            AddTxtTextByFileInfo("换一种方式判断风" + arr[index]);
            if (index == 1) //"东" 有值
            {
                AddTxtTextByFileInfo("东有值" + arr[index]);
                if (arr[index + 1] > 0)  //"南"有值
                {
                    AddTxtTextByFileInfo("南有值" + arr[index + 1]);
                    if (arr[index + 3] > 0) //"北"必须有值
                    {
                        AddTxtTextByFileInfo("北有值" + arr[index + 3]);
                        arr[index] -= 1;
                        arr[index + 1] -= 1;
                        arr[index + 3] -= 1;
                        arr[0] -= 3;
                        result = IsKanOrShunForFeng(arr);
                        arr[index] += 1;
                        arr[index + 1] += 1;
                        arr[index + 3] += 1;
                        arr[0] += 3;
                        AddTxtTextByFileInfo("东南北 result =" + result + (result ? " 顺子：东南北" : ""));
                        return result;
                    }
                    else if (arr[index + 2] > 0) //"西"有值
                    {
                        AddTxtTextByFileInfo("西有值" + arr[index + 2]);
                        arr[index] -= 1;
                        arr[index + 1] -= 1;
                        arr[index + 2] -= 1;
                        arr[0] -= 3;
                        result = IsKanOrShunForFeng(arr);
                        arr[index] += 1;
                        arr[index + 1] += 1;
                        arr[index + 2] += 1;
                        arr[0] += 3;
                        AddTxtTextByFileInfo("东南西 result =" + result + (result ? " 顺子：东南西" : ""));
                        return result;
                    }
                    AddTxtTextByFileInfo("失败1");
                }
                else if (arr[index + 2] > 0 && arr[index + 3] > 0)  //"南"没值， 那必须“西北”有值
                {
                    AddTxtTextByFileInfo("南没值,那必须“西北”有值" + arr[index + 2] + "|" + arr[index + 3]);
                    arr[index] -= 1;
                    arr[index + 2] -= 1;
                    arr[index + 3] -= 1;
                    arr[0] -= 3;
                    result = IsKanOrShunForFeng(arr);
                    arr[index] += 1;
                    arr[index + 2] += 1;
                    arr[index + 3] += 1;
                    arr[0] += 3;
                    AddTxtTextByFileInfo("东西北 result =" + result + (result ? " 顺子：东西北" : ""));
                    return result;
                }
                AddTxtTextByFileInfo("失败2");
            }
            else  //只剩 “南西北”
            {
                AddTxtTextByFileInfo("没有东，只剩 “南西北”");
                if (index == 2 && arr[index + 1] > 0 && arr[index + 2] > 0)  //"西北"必须2个都有值
                {
                    arr[index] -= 1;
                    arr[index + 1] -= 1;
                    arr[index + 2] -= 1;
                    arr[0] -= 3;
                    result = IsKanOrShunForFeng(arr);
                    arr[index] += 1;
                    arr[index + 1] += 1;
                    arr[index + 2] += 1;
                    arr[0] += 3;
                    AddTxtTextByFileInfo("南西北 result =" + result + (result ? " 顺子：南西北" : ""));
                    return result;
                }
                AddTxtTextByFileInfo("失败3");
            }

            AddTxtTextByFileInfo("失败4");

            return false;
        }

        //尽量出一张落单的牌
        public static Card getSingleCard(Card[] cards)
        {
            int[][] handcards = initCards(cards);

            Random random = new Random();
            int randomI = 0;
            int randomJ = 0;
            for (int i = 0; i < handcards.GetLength(0); i++)
            {
                if (handcards[i][0] > 0)
                {
                    for (int j = 1; j < handcards[i].Length - 1; j++)
                    {
                        if (handcards[i][j] > 0)
                        {
                            if (randomJ == 0 || random.Next(1, 6) % 3 == 0) //假如之前已经有值了，那么必须随机化才会改变值
                            {
                                randomI = i;
                                randomJ = j;
                            }

                        }
                        else if (handcards[i][j + 1] > 0) //因为总数减了1
                        {
                            if (randomJ == 0 || random.Next(1, 6) % 3 == 0)
                            {
                                randomI = i;
                                randomJ = j + 1;
                            }
                        }
                        if (i != 3 && j > 1 && handcards[i][j] >= 1 && (handcards[i][j + 1] == 0 || handcards[i][j - 1] == 0))
                        {
                            randomI = i;
                            randomJ = j;
                        }
                        if (i == 3) //东南西北
                        {
                            if (j == 1)
                            {
                                if ((handcards[i][j] > 0 ? 1 : 0) + (handcards[i][j + 1] > 0 ? 1 : 0) + (handcards[i][j + 2] > 0 ? 1 : 0) + (handcards[i][j + 3] > 0 ? 1 : 0) < 3)
                                {
                                    if (handcards[i][j] > 0)
                                    {
                                        AddTxtTextByFileInfo("成功getSingleCard" + new Card(j, i).ToString());
                                        return new Card(j, i);
                                    }
                                    else if (handcards[i][j + 1] > 0)
                                    {
                                        AddTxtTextByFileInfo("成功getSingleCard" + new Card(j + 1, i).ToString());
                                        return new Card(j + 1, i);
                                    }
                                    else if (handcards[i][j + 2] > 0)
                                    {
                                        AddTxtTextByFileInfo("成功getSingleCard" + new Card(j + 2, i).ToString());
                                        return new Card(j + 2, i);
                                    }
                                    else if (handcards[i][j + 3] > 0)
                                    {
                                        AddTxtTextByFileInfo("成功getSingleCard" + new Card(j + 3, i).ToString());
                                        return new Card(j + 3, i);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //当前牌型只有1张 且不连续
                            if (j == 1 && handcards[i][j] == 1 && handcards[i][j + 1] == 0)
                            {
                                AddTxtTextByFileInfo("成功getSingleCard" + new Card(j, i).ToString());
                                return new Card(j, i);
                            }
                            else if (j == handcards[i].Length - 2 && handcards[i][j] == 0 && handcards[i][j + 1] == 1)
                            {
                                AddTxtTextByFileInfo("成功getSingleCard" + new Card(j + 1, i).ToString());
                                return new Card(j + 1, i);
                            }
                            else if (j > 1 && handcards[i][j] == 1 && handcards[i][j + 1] == 0 && handcards[i][j - 1] == 0)
                            {
                                AddTxtTextByFileInfo("成功getSingleCard" + new Card(j, i).ToString());
                                return new Card(j, i);
                            }
                        }

                    }
                }
            }

            AddTxtTextByFileInfo("失败getSingleCard" + new Card(randomJ, randomI).ToString());
            return new Card(randomJ, randomI); //没符合要求的，那就只能返回一张有的牌了
        }

        //记录日志
        public static void AddTxtTextByFileInfo(string txtText)
        {
//#if UNITY_EDITOR
//            string path = "/Users/zengyanqi/Desktop/MyInfo.txt";
//            StreamWriter sw;
//            FileInfo fi = new FileInfo(path);

//            if (!File.Exists(path))
//            {
//                sw = fi.CreateText();
//            }
//            else
//            {
//                sw = fi.AppendText();   //在原文件后面追加内容      
//            }
//            sw.WriteLine(txtText);
//            sw.Close();
//            sw.Dispose();
//#endif
        }

        //#if UNITY_EDITOR
        //        //Debug.Log("现在是编辑器");
        //#elif UNITY_ANDROID
        //        //Debug.Log("现在是ANDROID");
        //#elif UNITY_IOS
        //        //Debug.Log("现在是IOS");
        //#else
        //        //Debug.Log("其他平台");
        //#endif

        //#if UNITY_IOS || UNITY_ANDROID 

        //switch (Application.platform)
        //{
        //  case RuntimePlatform.WindowsEditor:
        //	  Debug.Log("PC");
        //	  break;

        //  case RuntimePlatform.Android:
        //	  Debug.Log("Android");
        //	  break;

        //  case RuntimePlatform.IPhonePlayer:
        //	  Debug.Log("IOS");
        //	  break;
        //}
    }
}
