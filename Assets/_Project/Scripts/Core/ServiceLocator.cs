using System;
using System.Collections.Generic;
using UnityEngine;

namespace TxRpg.Core
{
    /// <summary>
    /// 전역 서비스 접근을 위한 경량 서비스 로케이터.
    /// BattleManager가 배틀 시작 시 서비스를 등록하고,
    /// 배틀 종료 시 Clear()를 호출합니다.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} 이미 등록됨, 덮어씀");
            }
            _services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            Debug.LogError($"[ServiceLocator] {type.Name} 서비스를 찾을 수 없음");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}
