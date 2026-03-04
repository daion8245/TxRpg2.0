using System;
using System.Collections.Generic;
using UnityEngine;

namespace TxRpg.Core
{
    /// <summary>
    /// 파라미터 없는 ScriptableObject 기반 이벤트 채널.
    /// 시스템 간 직접 참조 없이 느슨한 결합으로 통신합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TxRpg/Events/Void Event Channel")]
    public class EventChannel : ScriptableObject
    {
        private readonly HashSet<Action> _listeners = new();

        public void Register(Action listener)
        {
            _listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            _listeners.Remove(listener);
        }

        public void Raise()
        {
            foreach (var listener in _listeners)
            {
                listener?.Invoke();
            }
        }
    }
}
