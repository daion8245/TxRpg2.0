using UnityEngine;

namespace TxRpg.Data
{
    /// <summary>
    /// 이펙트 데이터 ScriptableObject.
    /// VFX 프리팹, 지속 시간, 위치 오프셋 등을 정의합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TxRpg/Data/Effect Data")]
    public class EffectDataSO : ScriptableObject
    {
        public string effectName;
        public GameObject prefab;
        public float duration = 1f;
        public Vector3 offset;
        public bool attachToTarget;
        public AudioClip sound;
    }
}
