using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 附加在池化对象上，用于追踪其原始预制体类型
/// </summary>
public class PoolTag : MonoBehaviour
{
    public string prefabName;
}

/// <summary>
/// 通用对象池：避免频繁 Instantiate/Destroy 造成的 GC 压力。
/// 支持按预制体名称分组存放，自动回收复用。
/// </summary>
public class ObjectPool
{
    private static ObjectPool instance;
    private Dictionary<string, Queue<GameObject>> objectPool = new Dictionary<string, Queue<GameObject>>();

    //单例
    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ObjectPool();
            }
            return instance;
        }
    }

    /// <summary>
    /// 清空对象池并销毁所有已池化的对象。
    /// 场景切换时调用，防止对象泄漏。
    /// </summary>
    public void init()
    {
        foreach (var kvp in objectPool)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null)
                    GameObject.Destroy(obj);
            }
        }
        objectPool.Clear();
    }

    /// <summary>
    /// 从对象池中获取一个对象。
    /// 如果池为空，则创建新的预制体实例并附加 PoolTag 组件。
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        string key = prefab.name;
        GameObject obj;

        if (!objectPool.TryGetValue(key, out var queue) || queue.Count == 0)
        {
            obj = GameObject.Instantiate(prefab);
            obj.name = prefab.name;
            var tag = obj.GetComponent<PoolTag>();
            if (tag == null)
                tag = obj.AddComponent<PoolTag>();
            tag.prefabName = key;
        }
        else
        {
            obj = queue.Dequeue();
        }

        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 将对象归还对象池。
    /// - 带有 PoolTag 的对象：回收到对应分组，SetActive(false) 等待复用。
    /// - 没有 PoolTag 的对象（如 new GameObject("init") 占位符）：直接 Destroy。
    /// </summary>
    public void Push(GameObject obj)
    {
        if (obj == null) return;

        var tag = obj.GetComponent<PoolTag>();
        if (tag == null)
        {
            // 没有 PoolTag 说明不是通过 ObjectPool 创建的，直接销毁
            GameObject.Destroy(obj);
            return;
        }

        obj.SetActive(false);
        string key = tag.prefabName;

        if (!objectPool.ContainsKey(key))
            objectPool[key] = new Queue<GameObject>();

        objectPool[key].Enqueue(obj);
    }
}
