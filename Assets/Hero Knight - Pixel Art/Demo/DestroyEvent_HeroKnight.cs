using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyEvent_HeroKnight : MonoBehaviour
{
    // Destroy particles when animation has finished playing. 
    // destroyEvent() is called as an event in animations.
    public void destroyEvent()
    {
        var pool = ObjectPool.Instance;
        PooledObject po = GetComponent<PooledObject>();

        if (pool != null && po != null && !string.IsNullOrEmpty(po.poolTag))
        {
            pool.ReturnToPool(po.poolTag, gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
