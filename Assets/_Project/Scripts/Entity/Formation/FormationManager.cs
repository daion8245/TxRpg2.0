using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// 배틀 필드에서 아군/적군의 배치 위치를 관리합니다.
    /// </summary>
    public class FormationManager
    {
        private readonly StageDataSO _stageData;

        private static readonly Vector2[] DefaultPlayerSlots =
        {
            new(-3f, 0f),
            new(-4f, 1f),
            new(-4f, -1f),
            new(-5f, 0f)
        };

        private static readonly Vector2[] DefaultEnemySlots =
        {
            new(3f, 0f),
            new(4f, 1f),
            new(4f, -1f),
            new(5f, 0f)
        };

        public FormationManager(StageDataSO stageData)
        {
            _stageData = stageData;
        }

        public Vector2 GetPlayerSlot(int index)
        {
            if (_stageData != null && _stageData.playerSlots != null && index < _stageData.playerSlots.Length)
            {
                return _stageData.playerSlots[index];
            }

            return index < DefaultPlayerSlots.Length
                ? DefaultPlayerSlots[index]
                : new Vector2(-3f - index, 0f);
        }

        public Vector2 GetEnemySlot(int index)
        {
            return index < DefaultEnemySlots.Length
                ? DefaultEnemySlots[index]
                : new Vector2(3f + index, 0f);
        }

        /// <summary>
        /// 공격자가 대상 앞으로 이동할 위치를 반환합니다.
        /// </summary>
        public Vector2 GetAttackPosition(BattleEntity attacker, BattleEntity target)
        {
            var targetPos = (Vector2)target.transform.position;
            float offsetX = attacker.Team == EntityTeam.Player ? -1.5f : 1.5f;
            return targetPos + new Vector2(offsetX, 0f);
        }
    }
}
