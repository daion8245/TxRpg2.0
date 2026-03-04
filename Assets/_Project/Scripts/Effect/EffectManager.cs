using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Effect
{
    public class EffectManager : MonoBehaviour
    {
        [SerializeField] private SpawnEffectEventChannel spawnEffectChannel;
        [SerializeField] private Transform effectRoot;

        private void Awake()
        {
            if (effectRoot == null)
                effectRoot = transform;

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            if (spawnEffectChannel != null)
                spawnEffectChannel.Register(OnSpawnEffect);
        }

        private void OnDisable()
        {
            if (spawnEffectChannel != null)
                spawnEffectChannel.Unregister(OnSpawnEffect);
        }

        private void OnSpawnEffect(SpawnEffectPayload payload)
        {
            if (payload.Prefab == null) return;

            var parent = payload.Parent != null ? payload.Parent : effectRoot;
            var go = Instantiate(payload.Prefab, payload.Position, payload.Rotation, parent);

            var effect = go.GetComponent<BattleEffect>();
            if (effect == null)
                effect = go.AddComponent<BattleEffect>();

            effect.Initialize(this, payload.Duration > 0 ? payload.Duration : 1f);
        }

        public void SpawnEffect(EffectDataSO data, Vector2 position, Transform parent = null)
        {
            if (data == null || data.prefab == null) return;

            var targetParent = parent != null ? parent : effectRoot;
            var pos = (Vector3)position + data.offset;
            var go = Instantiate(data.prefab, pos, Quaternion.identity, targetParent);

            var effect = go.GetComponent<BattleEffect>();
            if (effect == null)
                effect = go.AddComponent<BattleEffect>();

            effect.Initialize(this, data.duration);
        }

        public void ReturnEffect(BattleEffect effect)
        {
            if (effect != null)
                Destroy(effect.gameObject);
        }
    }
}
