using System.Collections.Generic;
using TxRpg.Core;
using TxRpg.Core.Events;
using TxRpg.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private TurnChangedEventChannel turnChangedChannel;
        [SerializeField] private DamageEventChannel damageChannel;

        [Header("UI 요소")]
        [SerializeField] private Text turnText;
        [SerializeField] private Transform playerHpBarRoot;
        [SerializeField] private GameObject hpBarPrefab;

        private readonly Dictionary<BattleEntity, Slider> _hpBars = new();

        private void OnEnable()
        {
            turnChangedChannel?.Register(OnTurnChanged);
            damageChannel?.Register(OnDamage);
        }

        private void OnDisable()
        {
            turnChangedChannel?.Unregister(OnTurnChanged);
            damageChannel?.Unregister(OnDamage);
        }

        public void InitializeHpBars(List<BattleEntity> players)
        {
            // 기존 HP 바 제거
            foreach (var bar in _hpBars.Values)
            {
                if (bar != null) Destroy(bar.gameObject);
            }
            _hpBars.Clear();

            if (hpBarPrefab == null || playerHpBarRoot == null) return;

            foreach (var player in players)
            {
                var go = Instantiate(hpBarPrefab, playerHpBarRoot);
                var slider = go.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.maxValue = player.Stats.MaxHp;
                    slider.value = player.Stats.CurrentHp;
                    _hpBars[player] = slider;

                    var label = go.GetComponentInChildren<Text>();
                    if (label != null)
                        label.text = player.UnitData.unitName;
                }
            }
        }

        public void UpdateHpBar(BattleEntity entity)
        {
            if (_hpBars.TryGetValue(entity, out var slider))
            {
                slider.value = entity.Stats.CurrentHp;
            }
        }

        private void OnTurnChanged(TurnChangedPayload payload)
        {
            if (turnText != null)
                turnText.text = $"Turn {payload.TurnNumber}";
        }

        private void OnDamage(DamagePayload payload)
        {
            var entity = payload.Target?.GetComponent<BattleEntity>();
            if (entity != null)
                UpdateHpBar(entity);
        }
    }
}
