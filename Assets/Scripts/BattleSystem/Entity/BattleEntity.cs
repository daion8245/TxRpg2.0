using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using TxRpg.Core.Events;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// 배틀 씬에서의 유닛 루트 컴포넌트.
    /// UnitDataSO로부터 초기화되며, 각 서브 컴포넌트를 관리합니다.
    /// </summary>
    public class BattleEntity : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private DamageEventChannel damageChannel;
        [SerializeField] private UnitDiedEventChannel unitDiedChannel;

        // 데이터
        public UnitDataSO UnitData { get; private set; }
        public EntityTeam Team { get; private set; }

        // 컴포넌트
        public EntityStats Stats { get; private set; }
        public EntityAnimator Animator { get; private set; }
        public EntitySkills Skills { get; private set; }
        public EntityBuffs Buffs { get; private set; }
        public EntityHitbox Hitbox { get; private set; }

        // 배틀 상태
        public bool IsAlive => Stats != null && Stats.CurrentHp > 0;
        public Vector2 FormationPosition { get; set; }

        public void Initialize(UnitDataSO data, EntityTeam team)
        {
            UnitData = data;
            Team = team;

            // 스탯 초기화
            Stats = new EntityStats(data);

            // 애니메이터 초기화
            var unityAnimator = GetComponentInChildren<Animator>();
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            IAnimationAdapter adapter = unityAnimator != null
                ? new SpriteAnimAdapter(unityAnimator)
                : null;
            Animator = new EntityAnimator(adapter, spriteRenderer);

            // 스킬 초기화
            Skills = new EntitySkills(data.skills);

            // 버프 초기화
            Buffs = new EntityBuffs(Stats);

            // 히트박스 초기화
            var collider = GetComponentInChildren<Collider2D>();
            var hitPoint = transform.Find("HitPoint");
            Hitbox = new EntityHitbox(collider, hitPoint);

            // 적은 왼쪽을 바라봄
            if (team == EntityTeam.Enemy)
            {
                Animator.SetFacingLeft(true);
            }

            // 초기 idle 애니메이션
            Animator.PlayIdle();
        }

        public void TakeDamage(int damage, bool isCritical)
        {
            Stats.ApplyDamage(damage);

            // 데미지 이벤트 발행
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

            if (!IsAlive)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            Stats.Heal(amount);
        }

        private void Die()
        {
            if (unitDiedChannel != null)
            {
                unitDiedChannel.Raise(new UnitDiedPayload
                {
                    Unit = gameObject
                });
            }
        }

        public async UniTask PlayDeathAsync(CancellationToken ct = default)
        {
            await Animator.PlayDieAsync(ct);
            gameObject.SetActive(false);
        }
    }
}
