using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using TxRpg.Animation;
using TxRpg.Core.Events;
using TxRpg.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.Entity
{
    /// <summary>
    /// 배틀 씬에서의 유닛 루트 컴포넌트.
    /// UnitDataSO로부터 초기화되며 월드 공간 HP바, 타겟 표시, 방패 연출을 관리합니다.
    /// </summary>
    public class BattleEntity : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private DamageEventChannel damageChannel;
        [SerializeField] private UnitDiedEventChannel unitDiedChannel;

        [Header("월드 UI / 비주얼 (프리팹에 연결)")]
        [SerializeField] private Slider worldHpSlider;
        [SerializeField] private TMP_Text worldNameText;
        [SerializeField] private TMP_Text worldStatusText;
        [SerializeField] private GameObject targetIndicator;
        [SerializeField] private GameObject shieldVisual;

        public UnitDataSO UnitData { get; private set; }
        public EntityTeam Team { get; private set; }
        public int SlotIndex { get; set; }

        public EntityStats Stats { get; private set; }
        public EntityAnimator Animator { get; private set; }
        public EntitySkills Skills { get; private set; }
        public EntityBuffs Buffs { get; private set; }
        public EntityHitbox Hitbox { get; private set; }

        public bool IsAlive => Stats != null && Stats.CurrentHp > 0;
        public Vector2 FormationPosition { get; set; }
        public event Action<int> Clicked;

        private SpriteRenderer _spriteRenderer;

        public void Initialize(UnitDataSO data, EntityTeam team)
        {
            UnitData = data;
            Team = team;

            Stats = new EntityStats(data);

            var unityAnimator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            IAnimationAdapter adapter = unityAnimator != null && unityAnimator.runtimeAnimatorController != null
                ? new SpriteAnimAdapter(unityAnimator)
                : null;
            Animator = new EntityAnimator(adapter, _spriteRenderer);

            Skills = new EntitySkills(data.skills ?? Array.Empty<SkillDataSO>());
            Buffs = new EntityBuffs(Stats);

            var col = GetComponentInChildren<Collider2D>();
            var hitPoint = transform.Find("HitPoint");
            Hitbox = new EntityHitbox(col, hitPoint, transform);

            if (team == EntityTeam.Enemy)
                Animator.SetFacingLeft(true);

            if (shieldVisual != null) shieldVisual.SetActive(false);
            if (targetIndicator != null) targetIndicator.SetActive(false);

            Animator.PlayIdle();
            RefreshWorldUI(data.unitName, string.Empty);
        }

        public void SyncHp(int currentHp, int maxHp, string label = null, string status = null, bool isGroggy = false)
        {
            if (Stats != null)
            {
                Stats.CurrentHp = Mathf.Clamp(currentHp, 0, Mathf.Max(1, maxHp));
            }

            if (worldHpSlider != null)
            {
                worldHpSlider.maxValue = Mathf.Max(1, maxHp);
                worldHpSlider.value = Mathf.Clamp(currentHp, 0, maxHp);
            }

            if (label != null && worldNameText != null)
                worldNameText.text = label;

            if (status != null && worldStatusText != null)
            {
                worldStatusText.text = status;
                worldStatusText.color = isGroggy ? new Color(1f, 0.88f, 0.35f) : new Color(0.85f, 0.9f, 1f);
            }

            if (_spriteRenderer != null)
            {
                if (currentHp <= 0)
                    _spriteRenderer.color = new Color(0.45f, 0.45f, 0.45f, 0.45f);
                else if (isGroggy)
                    _spriteRenderer.color = new Color(1f, 0.88f, 0.45f, 1f);
                else
                    _spriteRenderer.color = Color.white;
            }
        }

        public void RefreshWorldUI(string label, string status)
        {
            if (Stats == null) return;
            SyncHp(Stats.CurrentHp, Stats.MaxHp, label, status);
        }

        public void SetTargetHighlighted(bool active)
        {
            if (targetIndicator != null)
                targetIndicator.SetActive(active);
        }

        public void SetShieldActive(bool active)
        {
            if (shieldVisual != null)
                shieldVisual.SetActive(active);
        }

        public void TakeDamage(int damage, bool isCritical)
        {
            Stats.ApplyDamage(damage);

            if (damageChannel != null)
            {
                damageChannel.Raise(new DamagePayload
                {
                    Target = gameObject,
                    Damage = damage,
                    IsCritical = isCritical,
                    HitPosition = Hitbox.Center
                });
            }

            if (!IsAlive) Die();
        }

        public void Heal(int amount) => Stats.Heal(amount);

        private void Die()
        {
            if (unitDiedChannel != null)
            {
                unitDiedChannel.Raise(new UnitDiedPayload { Unit = gameObject });
            }
        }

        public async UniTask PlayDeathAsync(CancellationToken ct = default)
        {
            await Animator.PlayDieAsync(ct);
            if (_spriteRenderer != null)
                _spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.35f);
        }

        private void OnMouseDown()
        {
            Clicked?.Invoke(SlotIndex);
        }
    }
}
