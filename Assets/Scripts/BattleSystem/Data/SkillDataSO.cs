using UnityEngine;

namespace TxRpg.Data
{
    /// <summary>
    /// 스킬 정의 ScriptableObject.
    /// timeline 배열에 실행 시퀀스를 정의하여 연출을 구성합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TxRpg/Data/Skill Data")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("속성")]
        public SkillCategory category = SkillCategory.Physical;
        public ElementType element = ElementType.None;
        public TargetType targetType = TargetType.SingleEnemy;
        public int mpCost;
        public int basePower = 100;
        public float cooldown;

        [Header("타임라인 시퀀스")]
        public SkillActionData[] timeline;
    }

    /// <summary>
    /// 스킬 타임라인의 개별 액션 데이터.
    /// actionType에 따라 관련 필드만 사용됩니다.
    /// </summary>
    [System.Serializable]
    public class SkillActionData
    {
        public SkillActionType actionType;

        [Header("애니메이션")]
        public string animationName;

        [Header("이동")]
        public float moveSpeed = 10f;

        [Header("타이밍")]
        public float duration = 0.5f;

        [Header("데미지")]
        public int damageMultiplierPercent = 100;

        [Header("이펙트")]
        public EffectDataSO effect;

        [Header("사운드")]
        public AudioClip soundClip;
        public float soundVolume = 1f;

        [Header("카메라")]
        public float shakeIntensity = 0.3f;
        public float zoomSize = 3f;

        [Header("플래시")]
        public Color flashColor = Color.white;

        [Header("병렬 액션")]
        public SkillActionData[] parallelActions;
    }
}
