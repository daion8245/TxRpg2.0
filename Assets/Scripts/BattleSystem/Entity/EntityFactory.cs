using System.Collections.Generic;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// UnitDataSO를 기반으로 BattleEntity 인스턴스를 생성합니다.
    /// </summary>
    public class EntityFactory
    {
        private readonly Transform _battleRoot;

        public EntityFactory(Transform battleRoot)
        {
            _battleRoot = battleRoot;
        }

        public BattleEntity CreateEntity(UnitDataSO data, EntityTeam team, Vector2 position)
        {
            if (data.prefab == null)
            {
                Debug.LogError($"[EntityFactory] {data.unitName}의 프리팹이 없습니다.");
                return null;
            }

            var go = Object.Instantiate(data.prefab, position, Quaternion.identity, _battleRoot);
            go.name = $"{data.unitName}_{team}";

            var entity = go.GetComponent<BattleEntity>();
            if (entity == null)
            {
                entity = go.AddComponent<BattleEntity>();
            }

            entity.Initialize(data, team);
            entity.FormationPosition = position;

            return entity;
        }

        public List<BattleEntity> CreatePlayerParty(UnitDataSO[] party, Vector2[] slots)
        {
            var entities = new List<BattleEntity>();

            for (int i = 0; i < party.Length; i++)
            {
                if (party[i] == null) continue;
                var position = i < slots.Length ? slots[i] : new Vector2(-3f - i, 0f);
                var entity = CreateEntity(party[i], EntityTeam.Player, position);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public List<BattleEntity> CreateEnemyFormation(EnemySpawnData[] enemies)
        {
            var entities = new List<BattleEntity>();

            foreach (var spawnData in enemies)
            {
                if (spawnData.unitData == null) continue;
                var entity = CreateEntity(spawnData.unitData, EntityTeam.Enemy, spawnData.position);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }
    }
}
