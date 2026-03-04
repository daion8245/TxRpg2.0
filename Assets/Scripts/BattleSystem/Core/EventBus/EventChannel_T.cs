using System;
using System.Collections.Generic;
using UnityEngine;

namespace TxRpg.Core
{
    /// <summary>
    /// 제네릭 페이로드를 가진 ScriptableObject 기반 이벤트 채널.
    /// Unity는 제네릭 SO를 직접 시리얼라이즈하지 못하므로
    /// 구체적 서브클래스를 만들어 사용합니다.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private readonly HashSet<Action<T>> _listeners = new();

        public void Register(Action<T> listener)
        {
            _listeners.Add(listener);
        }

        public void Unregister(Action<T> listener)
        {
            _listeners.Remove(listener);
        }

        public void Raise(T payload)
        {
            foreach (var listener in _listeners)
            {
                listener?.Invoke(payload);
            }
        }
    }
}
