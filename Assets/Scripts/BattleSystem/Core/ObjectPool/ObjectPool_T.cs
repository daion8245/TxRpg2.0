using System.Collections.Generic;
using UnityEngine;

namespace TxRpg.Core
{
    /// <summary>
    /// Component 기반 오브젝트 풀.
    /// 이펙트, 데미지 팝업, 투사체 등의 재사용에 활용합니다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();
        private readonly int _initialSize;

        public ObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
        }

        public void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _available.Enqueue(instance);
            }
        }

        public T Get()
        {
            T instance;

            if (_available.Count > 0)
            {
                instance = _available.Dequeue();
            }
            else
            {
                instance = CreateInstance();
            }

            instance.gameObject.SetActive(true);

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            return instance;
        }

        public void Release(T instance)
        {
            if (instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }

        private T CreateInstance()
        {
            return Object.Instantiate(_prefab, _parent);
        }
    }
}
