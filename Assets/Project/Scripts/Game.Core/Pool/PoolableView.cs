using UnityEngine;

namespace Game.Core.Pool
{
    public abstract class PoolableView : MonoBehaviour, IPoolable
    {
        public virtual void OnActive() { }
        public virtual void OnInactive() { }
        protected virtual void OnDestroy() { }
    }
}
