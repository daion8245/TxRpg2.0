using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// 유닛의 피격 판정 영역과 이펙트 스폰 포인트를 관리합니다.
    /// </summary>
    public class EntityHitbox
    {
        private readonly Collider2D _collider;
        private readonly Transform _hitPoint;
        private readonly Transform _owner;

        public Vector2 Center => _collider != null
            ? (Vector2)_collider.bounds.center
            : (_hitPoint != null ? (Vector2)_hitPoint.position : (_owner != null ? (Vector2)_owner.position : Vector2.zero));

        public Vector2 Top => _collider != null
            ? new Vector2(_collider.bounds.center.x, _collider.bounds.max.y)
            : Center + Vector2.up * 0.9f;

        public EntityHitbox(Collider2D collider, Transform hitPoint = null, Transform owner = null)
        {
            _collider = collider;
            _hitPoint = hitPoint;
            _owner = owner;
        }
    }
}
