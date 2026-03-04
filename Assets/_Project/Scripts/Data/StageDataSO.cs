using UnityEngine;

namespace TxRpg.Data
{
    /// <summary>
    /// 스테이지(배틀 인카운터) 데이터 ScriptableObject.
    /// 적 구성, 배경, 보상 등을 정의합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TxRpg/Data/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string stageName;
        public Sprite background;
        public AudioClip bgm;

        [Header("적 진형")]
        public EnemySpawnData[] enemies;

        [Header("아군 배치 슬롯")]
        public Vector2[] playerSlots = new[]
        {
            new Vector2(-3f, 0f),
            new Vector2(-4f, 1f),
            new Vector2(-4f, -1f),
            new Vector2(-5f, 0f)
        };

        [Header("보상")]
        public int goldReward;
        public int expReward;
    }

    [System.Serializable]
    public class EnemySpawnData
    {
        public UnitDataSO unitData;
        public Vector2 position;
        public int level = 1;
    }
}
