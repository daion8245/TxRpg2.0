using UnityEngine;

namespace TxRpg.Data
{
    /// <summary>
    /// 유닛(플레이어/몬스터)의 기본 데이터를 정의하는 ScriptableObject.
    /// 런타임 시 EntityFactory가 이 데이터를 읽어 BattleEntity 인스턴스를 생성합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TxRpg/Data/Unit Data")]
    public class UnitDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string unitName;
        public Sprite portrait;
        public GameObject prefab;

        [Header("기본 스탯")]
        public int baseHp = 100;
        public int baseMp = 50;
        public int baseAtk = 10;
        public int baseDef = 5;
        public int baseSpd = 10;
        public int baseKd = 0;
        public int baseKr = 0;

        [Header("스킬")]
        public SkillDataSO[] skills;

        [Header("팀")]
        public EntityTeam team = EntityTeam.Player;
    }
}
