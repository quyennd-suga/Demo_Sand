//using UnityEngine;

//public sealed class PooledObject : MonoBehaviour
//{
//    internal Pool Owner { get; set; }

//    // Fast path: không cần PoolManager.Despanw nữa
//    public void Release()
//    {
//        if (Owner != null) Owner.Release(gameObject);
//        else Destroy(gameObject);
//    }
//}
