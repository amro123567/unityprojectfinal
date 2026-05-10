using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public string poolTag;
    public float lifetime;

    void OnEnable()
    {
        if (lifetime > 0.01f)
            Invoke(nameof(ReturnNow), lifetime);
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void ReturnNow()
    {
        if (ObjectPool.Instance != null && !string.IsNullOrEmpty(poolTag))
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
    }
}
