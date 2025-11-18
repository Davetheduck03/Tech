using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}

[Serializable]
public class PoolItem
{
    public GameObject prefab;
    public int size = 10;
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [Header("Pools to initialize")]
    public List<PoolItem> poolItems;

    private Dictionary<string, Queue<GameObject>> pools = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        foreach (var item in poolItems)
        {
            CreatePool(item.prefab, item.size);
        }
    }

    public void CreatePool(GameObject prefab, int count)
    {
        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = key;
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pools[key].Enqueue(obj);
        }
    }

    public GameObject Spawn(string poolName, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' does not exist.");
            return null;
        }

        GameObject obj;

        if (pools[poolName].Count > 0)
        {
            obj = pools[poolName].Dequeue();
        }
        else
        {
            Debug.LogWarning($"Pool '{poolName}' empty. Instantiating extra.");
            GameObject prefab = poolItems.Find(p => p.prefab.name == poolName)?.prefab;
            if (prefab == null) return null;
            obj = Instantiate(prefab);
            obj.name = poolName;
        }

        obj.transform.SetPositionAndRotation(pos, rot);
        if (parent != null) obj.transform.SetParent(parent);

        obj.SetActive(true);

        foreach (var poolable in obj.GetComponentsInChildren<IPoolable>())
            poolable.OnSpawned();

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        string key = obj.name;

        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"Despawn called on unpooled object '{key}'. Destroying.");
            Destroy(obj);
            return;
        }

        foreach (var poolable in obj.GetComponentsInChildren<IPoolable>())
            poolable.OnDespawned();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[key].Enqueue(obj);
    }
}
