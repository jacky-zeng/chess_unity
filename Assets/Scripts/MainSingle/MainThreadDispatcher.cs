/**
 C# WebSocketSharp在OnMessage方法中无法直接调用Unity中的GameObject，原因在于WebSocketSharp在子线程运行，而Unity只能在主线程中访问和更新游戏对象。因此，如果需要在OnMessage方法中调用Unity中的GameObject，需要使用Unity提供的MainThreadDispatcher工具，在主线程上执行代码。

以下是一种使用MainThreadDispatcher的示例方式，用于在子线程接收Websocket消息后，在主线程中更新Unity游戏对象。

首先，在Unity中构造一个静态类，用于处理主线程任务的调度：

using System.Collections.Generic;
using UnityEngine;

public static class MainThreadDispatcher
{
    private static readonly Queue<System.Action> s_Actions = new Queue<System.Action>();

    public static void Enqueue(System.Action action)
    {
        lock (s_Actions)
        {
            s_Actions.Enqueue(action);
        }
    }

    public static void Execute()
    {
        while (true)
        {
            System.Action action = null;
            lock (s_Actions)
            {
                if (s_Actions.Count > 0)
                {
                    action = s_Actions.Dequeue();
                }
            }
            if (action == null)
            {
                break;
            }
            action.Invoke();
        }
    }
}
C#
然后，在OnMessage方法中，将要在主线程执行的代码通过MainThreadDispatcher的Enqueue方法，添加到主线程执行队列中，等待主线程执行。在Unity的Update方法中，通过MainThreadDispatcher的Execute方法执行主线程任务队列中的任务。

以下是OnMessage方法的示例：

private void OnMessage(object sender, MessageEventArgs e)
{
    // 接收到的websocket消息
    string msg = e.Data;

    // 将要在主线程执行的处理代码
    System.Action action = () =>
    {
        // 在这里处理游戏对象更新逻辑
        GameObject go = GameObject.Find("GameObjectName");
        if (go != null)
        {
            // 更新游戏对象的逻辑
            // ...
        }
    };

    // 将任务添加到主线程任务队列中等待执行
    MainThreadDispatcher.Enqueue(action);
}

// 在Unity的Update方法中执行主线程任务队列中的任务
void Update()
{
    MainThreadDispatcher.Execute();
}
C#
通过上述的方法，在OnMessage方法中就可以成功调用Unity中的GameObject了。注意在OnMessage中只能执行在主线程中不需要耗时操作的代码，否则会影响整个游戏的性能和流畅度。
 * **/

using System.Collections.Generic;
using UnityEngine;

public static class MainThreadDispatcher
{
    private static readonly Queue<System.Action> s_Actions = new Queue<System.Action>();

    public static void Enqueue(System.Action action)
    {
        lock (s_Actions)
        {
            s_Actions.Enqueue(action);
        }
    }

    public static void Execute()
    {
        while (true)
        {
            System.Action action = null;
            lock (s_Actions)
            {
                if (s_Actions.Count > 0)
                {
                    action = s_Actions.Dequeue();
                }
            }
            if (action == null)
            {
                break;
            }
            action.Invoke();
        }
    }
}

