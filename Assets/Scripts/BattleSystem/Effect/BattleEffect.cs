using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Effect
{
    public class BattleEffect : MonoBehaviour, IPoolable
    {
        private float _lifetime;
        private float _elapsed;
        private EffectManager _manager;
        private bool _active;

        public void Initialize(EffectManager manager, float lifetime)
        {
            _manager = manager;
            _lifetime = lifetime;
            _elapsed = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                _active = false;
                _manager?.ReturnEffect(this);
            }
        }

        public void OnSpawn()
        {
            _elapsed = 0f;
            _active = true;
        }

        public void OnDespawn()
        {
            _active = false;
        }
    }
}
