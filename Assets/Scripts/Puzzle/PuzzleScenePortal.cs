using TxRpg.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// 열린 문 너머에 배치된 전환 포털. 플레이어가 통과하면 전투 씬으로 이동합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PuzzleScenePortal : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = GameSessionState.BattleSceneName;
        [SerializeField] private bool openDeckEditorOnEnter = true;

        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (other.GetComponentInParent<VillagePlayerMovement>() == null) return;

            _triggered = true;
            GameSessionState.OpenDeckEditorOnBattleEnter = openDeckEditorOnEnter;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
