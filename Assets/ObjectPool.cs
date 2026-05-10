using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools = new List<Pool>();
    readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        WarmPools();
    }

    void WarmPools()
    {
        poolDictionary.Clear();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null || string.IsNullOrEmpty(pool.tag))
                continue;

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            poolDictionary[pool.tag] = queue;
        }
    }

    public void RegisterRuntimePool(string tag, GameObject prefab, int size)
    {
        if (prefab == null || string.IsNullOrEmpty(tag))
            return;

        if (poolDictionary.ContainsKey(tag))
            return;

        Queue<GameObject> queue = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            QueuePooledBehaviour(obj, tag);
            queue.Enqueue(obj);
        }

        poolDictionary[tag] = queue;
    }

    static void QueuePooledBehaviour(GameObject obj, string tag)
    {
        PooledObject po = obj.GetComponent<PooledObject>();
        if (po == null)
            po = obj.AddComponent<PooledObject>();
        po.poolTag = tag;
        po.lifetime = 0f;
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> queue) || queue.Count == 0)
        {
            Debug.LogWarning("Pool tag missing or empty: " + tag);
            return null;
        }

        GameObject obj = queue.Dequeue();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(string tag, GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);

        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> queue))
            return;

        queue.Enqueue(obj);
    }
}
