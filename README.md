# 宜春麻将 (Yichun Mahjong)

基于 **Unity 2022.3.62f2** 开发的宜春麻将游戏，支持**单机对战**和**在线联网**两种模式。

---

## 目录

- [游戏简介](#游戏简介)
- [项目结构](#项目结构)
- [核心模块说明](#核心模块说明)
  - [数据层 (Common)](#数据层-common)
  - [逻辑引擎 (Helper)](#逻辑引擎-helper)
  - [单机模式 (MainSingle)](#单机模式-mainsingle)
  - [在线模式 (MainOnline)](#在线模式-mainonline)
  - [界面管理 (Start / Select)](#界面管理-start--select)
  - [工具与组件](#工具与组件)
- [游戏流程](#游戏流程)
  - [单机流程](#单机流程)
  - [在线流程](#在线流程)
- [AI 策略](#ai-策略)
- [外部依赖](#外部依赖)
- [场景说明](#场景说明)

---

## 游戏简介

宜春麻将是一种流行于江西省宜春地区的麻将变体，使用标准的 136 张牌（万、筒、条、风、箭牌），支持：

- **吃 (Chi)** — 上家出牌后，用手中两张牌与之组成顺子
- **碰 (Peng)** — 任何一家出牌后，用手中有两张相同的牌与之组成刻子
- **杠 (Gang / AnGang)** — 明杠（已有三张相同，别人打出第四张）或暗杠（手中有四张相同）
- **胡 (Hu)** — 听牌后自摸或点炮胡
- **一炮多响** — 一张出牌可被多个玩家同时胡

**单机模式**：1 名人类玩家 + 3 名机器人 AI。
**在线模式**：通过 WebSocket 连接服务器，支持最多 4 名真人玩家，离线位置由房主控制机器人代为操作。

---

## 项目结构

```
Assets/
├── Models/                          # 3D 模型与材质
│   ├── ZYQAA.shader                 # 自定义抗锯齿着色器（Sobel + 旋转网格采样）
│   ├── bg.fbx                       # 麻将桌 3D 模型
│   └── zyq.mat                      # 材质
├── Prefabs/                         # 预制体
│   ├── MJ.prefab                    # 单机模式麻将牌（含 MJ.cs 脚本）
│   ├── MJ 1.prefab                  # 在线模式麻将牌变体（含 MJOnline.cs）
│   ├── MJ_HIDDEN.prefab             # 暗牌（牌库 / 机器人手牌不可见）
│   ├── MJ_NO_SCRIPT.prefab          # 纯视觉牌（无脚本）
│   └── Timer.prefab                 # 倒计时组件
├── Resources/
│   ├── Audios/bg/                   # 背景音乐
│   ├── Audios/style[1-4]/           # 按方位区分的音效（出牌、碰、杠、胡等）
│   └── Images/                      # 精灵图（牌面、头像、桌面背景等）
├── Scenes/
│   ├── Start.unity                  # 启动界面
│   ├── Select.unity                 # 房间号输入 / 连接界面
│   ├── Main.unity                   # 单机游戏主场景
│   └── MainOnline.unity             # 在线游戏主场景
├── Scripts/
│   ├── Common/                      # 公共数据模型与工具
│   │   ├── Card.cs                  # 麻将牌数据模型
│   │   ├── Common.cs                # 场景枚举、网络消息模型
│   │   └── Helper.cs                # 麻将规则引擎（胡牌检测/吃碰杠/AI 出牌）
│   ├── MainSingle/                  # 单机模式
│   │   ├── MJ.cs                    # 单机麻将牌 MonoBehaviour
│   │   ├── MainManager.cs           # 单机模式核心管理器
│   │   └── MainThreadDispatcher.cs  # 线程调度器（WebSocket 回调 → 主线程）
│   ├── MainOnline/                  # 在线模式
│   │   ├── MJOnline.cs              # 在线麻将牌 MonoBehaviour（支持拖拽）
│   │   └── MainOnlineManager.cs     # 在线模式核心管理器
│   ├── Select/
│   │   └── SelectMainManager.cs     # 房间号界面管理器
│   ├── Start/
│   │   └── StartMainManager.cs      # 启动界面管理器
│   ├── ObjectPool.cs                # 通用对象池（预留，当前未使用）
│   └── ObjectPool.cs.meta
├── Timer.cs                         # 倒计时组件
├── UnityPackages/
│   └── JsonNet-Lite/                # Newtonsoft.Json（WebSocket 消息序列化）
├── websocket-sharp.dll              # WebSocketSharp 客户端库
└── Ttf/
    └── xingshu.ttf                  # 行书字体
```

---

## 核心模块说明

### 数据层 (Common)

#### Card.cs — `namespace zengyanqiCard`

麻将牌的数据模型，包含：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Number` | `int` | 牌值 1-9 |
| `CardType` | `int` | 花色：0=筒, 1=条, 2=万, 3=风(东南西北), 4=箭(中发白) |
| `TagType` | `int` | 牌状态：0=普通, 1=吃, 2=碰, 3=明杠, 4=暗杠 |
| `UserType` | `int` | 所属：0=牌库, 1=手牌, 2=已打出 |
| `Hu` | `int` | 是否胡牌：0=否, 1=是 |

并定义了四个枚举类型：`CardType`、`TagType`、`UserType`、`Hu`。

#### Common.cs — `namespace zengyanqiCommon`

共享枚举与网络消息模型：

- **`ScenesSelect`** — 场景索引（Start=0, Select=1, Main=2, MainOnline=3）
- **`PlayerPrefsKey`** — PlayerPrefs 字符串键（Type, GroupNum, DeviceUniqueId 等）
- **`MessageType`** — WebSocket 消息类型（Connect, Prepare, Start, End, UserGrab, UserKnock, Chi, Peng, Gang, AnGang, Operate, Next 等）
- **`OnlineSide`** — 方位（East=1, South=2, West=3, North=4）
- **`ReceiveMessage`** — 服务器 → 客户端的消息模型（type, message, date）
- **`SendMessage`** — 客户端 → 服务器的消息模型（type, group_num, device_unique_id, message）

---

### 逻辑引擎 (Helper)

#### Helper.cs — `namespace zengyanqiHelper`

约 **1117 行**的麻将核心规则引擎，是项目中最核心的逻辑文件。

**关键方法：**

| 方法 | 功能 |
|------|------|
| `initCards(Card[])` | 将 `Card[]` 转换为 `int[5][]` 二维计数数组（按花色分组，`[i][0]` 为该组张数） |
| `canKan(Card[], Card)` | 检测手牌中是否有两张与给定牌相同的牌（即可碰） |
| `canShun(Card[], Card)` | 检测手牌是否能与给定牌组成顺子（处理筒条万的连续序列，以及东南西北/中发白的特殊组合） |
| `canAnGangList(Card[])` | 返回手牌中所有可暗杠的牌（4 张相同） |
| `canGang(Card[], Card)` | 检测手牌中是否有三张与给定牌相同的牌（即可明杠） |
| `isWin(Card[])` | **胡牌检测主函数**，依次检查：七小对 → 十三烂 → 字一色 → 标准胡（递归去将一个对子，其余牌为刻子或顺子） |
| `getSingleCard(Card[])` | **AI 出牌策略**——找出"最孤独"的牌打出（不连续、数量最少） |

**胡牌检测**支持：
1. **七小对** — 正好 7 个对子
2. **十三烂** — 序数牌间隔 ≥ 2，字牌不重复，共 14 张
3. **字一色** — 全部为字牌
4. **标准胡** — 去除一个对子后，其余均为刻子/顺子

---

### 单机模式 (MainSingle)

#### MainManager.cs — 约 2080 行

单例模式 (`_instance`) 的核心控制器，管理完整的单机麻将牌局。

**核心数据结构：**

| 成员 | 类型 | 说明 |
|------|------|------|
| `dictSingle` | `Dictionary<string, Sprite>` | `"number\|cardType"` → 牌面精灵图 |
| `dictWhole` | `Dictionary<int, Card>` | 整副 136 张牌 |
| `currentUserMj` | `Dictionary<int, GameObject>` | 当前玩家手牌 |
| `currentUserOutMj` | 系列变量 | 用户已打出的牌 |
| `robotList[]` | `Dictionary<int, GameObject>[]` | 按方位的机器人手牌 |
| `robotOutList[]` | 系列变量 | 机器人打出的牌 |
| `dictShun` | `Dictionary` 类型 | 吃牌候选 |

**游戏阶段：**
1. **`Init()`** — 生成 136 张牌 → 洗牌 → 发牌（每人 13 张）→ 掷骰子 → 定庄家
2. **`userGrabCard()`** — 玩家抓牌 → 检测可操作项（胡/暗杠/明杠）
3. **`currentUserOperate()`** — 显示可用操作按钮
4. 玩家点击手牌 → `currentUserKnock()` 打出
5. **`otherOperate()`** — 传给机器人 AI
6. 机器人循环：`robotGrabCard()` → `currentRobotOperate()` → `currentRobotKnock()` → `otherRobotHandle()`
7. **`end()`** — 结算界面，展示所有玩家手牌

**音效系统**：按方位区分配置独立音频文件，出牌/碰/杠/胡各有不同音效。

#### MJ.cs

单机模式的麻将牌组件，处理玩家点击。使用 `Update()` 中的射线检测，`Select()` 将牌上移 2 单位表示选中，`ResetSelect()` 恢复原位，点击已选中的牌触发 `MainManager._instance.Click()` 出牌。

---

### 在线模式 (MainOnline)

#### MainOnlineManager.cs — 约 3743 行（项目最大文件）

通过网络连接 `wss://www.zengyanqi.com:9600` 进行在线对战的完整实现，使用 **WebSocketSharp** 库 + **Newtonsoft.Json** 序列化。

**与单机模式的关键差异：**

| 概念 | 说明 |
|------|------|
| `gapDiceSide` | 方位偏移量，将服务器分配方位映射到本地显示（用户始终为 East） |
| `isHomeOwner` | 房主标识，房主可为离线机器人代操作 |
| `realUserDiceSide` | 服务器分配的玩家方位 |
| `isNotOnlineSides[]` | 记录哪些位置是离线机器人 |

**网络消息处理（`OnMessage`）：**

| 消息类型 | 说明 |
|---------|------|
| `Fail` | 连接/游戏失败 |
| `Connect` | 连接成功 |
| `Prepare` | 准备就绪 |
| `Start` | 牌局开始，分发手牌 |
| `UserGrab` | 玩家抓牌 |
| `UserKnock` | 玩家出牌 |
| `Operate` | 等待玩家操作（吃/碰/杠/胡选择） |
| `AnGang / Gang / Peng / Chi` | 相应操作 |
| `Next` | 轮到下家 |
| `End` | 牌局结束 |

**特色功能：**
- 所有 WebSocket 回调通过 `MainThreadDispatcher` 封送到主线程
- 支持拖拽出牌（`MJOnline.cs` 的 `OnMouseDown/Up`）
- 倒计时限制玩家操作（`Timer.cs`）
- 设置面板：开关"允许吃牌"、"允许机器人胡牌"

#### MJOnline.cs

在线模式的麻将牌组件，支持点击和拖拽：

- **`OnMouseDown()`** — 开始拖拽，牌上移选中
- **`OnMouseUp()`** — 停止拖拽，Y 坐标 > 39 判定为出牌，调用 `MainOnlineManager._instance.Click()`
- **`Select()` / `StandUp()`** — 程序化选中/弹起

#### MainThreadDispatcher.cs

标准主线程调度器（`Queue<Action>` + `Update()` 执行），解决 WebSocketSharp 后台线程回调无法直接调用 Unity API 的问题。

---

### 界面管理 (Start / Select)

#### StartMainManager.cs

启动场景管理器，随机显示三个启动背景之一，处理摄像机自适应分辨率，提供"开始"和"退出"按钮。

#### SelectMainManager.cs

房间号选择界面管理器，输入房间号和设备 ID，选择"创建房间"（Type=1）或"加入房间"（Type=2），数据存入 PlayerPrefs 后加载 `MainOnline` 场景。

---

### 工具与组件

#### Timer.cs

在线模式的倒计时组件，使用精灵图数字显示，≤5 秒时播放节拍音效，超时自动触发"过"按钮。

#### ObjectPool.cs

通用的 Unity 对象池（`Dictionary<string, Queue<GameObject>>` + 单例模式），当前项目中未使用，为未来性能优化预留。

#### ZYQAA.shader

自定义图像效果着色器，使用 Sobel 边缘检测结合旋转网格采样实现抗锯齿效果，可通过游戏内按钮开启/关闭。

---

## 游戏流程

### 单机流程

```
启动 → 选择 → 主场景
   │
   ├─ Init(): 生成136张牌 → 洗牌 → 发牌(每人13张)
   ├─ 掷骰子 → 定庄家 → 庄家抓第14张
   │
   └─ 游戏循环:
        ├─ 当前玩家抓牌
        ├─ 检查操作（胡/暗杠/明杠）
        ├─ 出牌
        ├─ 其他玩家按优先级应答（胡 > 杠 > 碰 > 吃）
        │    ├─ 有人胡 → 结束
        │    └─ 无人应答 → 下家继续
        └─ 直到有人胡牌或牌库空
```

### 在线流程

```
启动 → 输入房间号 → 创建/加入房间
   │
   ├─ PlayerPrefs 保存连接信息
   ├─ WebSocket 连接 wss://www.zengyanqi.com:9600
   ├─ 等待其他玩家加入
   ├─ 房主设置（吃牌/机器人胡牌开关）
   ├─ 服务器发牌 → 分发手牌
   │
   └─ 游戏循环（由服务器协调）:
        ├─ 服务器广播 UserGrab
        ├─ 服务器发送 Operate（等待操作）
        ├─ 玩家出牌/选择操作 → 发送 UserKnock/Chi/Peng/Gang
        ├─ 服务器广播到所有客户端
        ├─ 房主为离线机器人执行 AI 操作
        └─ 直到有人胡牌或牌库空
```

---

## AI 策略

机器人使用 **"打落单牌"策略**（`getSingleCard()`），优先打出最不连续的牌（与相邻牌差距最大、同花色张数最少），未实现防守型麻将 AI（如跟熟张、猜牌等高级策略）。

在在线模式中，房主负责为离线位置的机器人执行 AI 操作，使用与单机相同的策略逻辑。

---

## 外部依赖

| 依赖 | 用途 | 版本/来源 |
|------|------|-----------|
| **websocket-sharp.dll** | WebSocket 客户端连接 | 开源库 |
| **Newtonsoft.Json** (JsonNet-Lite) | WebSocket 消息 JSON 序列化/反序列化 | UnityPackage |
| **PHP Swoole** | 后端 WebSocket 服务器 | `wss://www.zengyanqi.com:9600` (TLS 1.2) |

> 注意：后端服务源码不在本项目中，仅包含 Unity 客户端代码。

---

## 场景说明

| 场景 | 场景名 | 对应管理器 | 说明 |
|------|--------|-----------|------|
| `Start.unity` | 启动界面 | `StartMainManager.cs` | 背景展示 + 进入/退出 |
| `Select.unity` | 房间选择 | `SelectMainManager.cs` | 创建/加入房间 |
| `Main.unity` | 单机游戏 | `MainManager.cs` | 单机对战主场景 |
| `MainOnline.unity` | 在线游戏 | `MainOnlineManager.cs` | 联网对战主场景 |

---

## 备注

- 所有方位相关操作（UI 布局、音效播放）以 **东(East)=1** 为基础坐标系，在线模式通过 `gapDiceSide` 偏移量进行适配。
- 使用 **Unity 2022.3 LTS** 构建，建议使用相同或更新版本打开。
- 日志系统通过 `openLog` 开关控制，可在 UI 上显示调试文本日志。
- 游戏中的一炮多响机制允许同一张出牌被多个玩家同时胡牌。
