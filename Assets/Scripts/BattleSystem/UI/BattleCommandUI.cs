using System.Threading;
using Cysharp.Threading.Tasks;
using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    /// <summary>
    /// 배틀 커맨드 UI - 공격, 스킬, 아이템, 도망 4개 버튼
    /// </summary>
    public class BattleCommandUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button fleeButton;
        [SerializeField] private Text entityNameText;

        private ActionType _selectedCommand;
        private bool _selectionMade;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (attackButton != null)
                attackButton.onClick.AddListener(() => SelectCommand(ActionType.Attack));

            if (skillButton != null)
                skillButton.onClick.AddListener(() => SelectCommand(ActionType.Skill));

            if (itemButton != null)
                itemButton.onClick.AddListener(() => SelectCommand(ActionType.Item));

            if (fleeButton != null)
                fleeButton.onClick.AddListener(() => SelectCommand(ActionType.Flee));
        }

        private void SelectCommand(ActionType type)
        {
            _selectedCommand = type;
            _selectionMade = true;
        }

        /// <summary>
        /// 커맨드 선택 대기. 플레이어가 4개 버튼 중 하나를 누를 때까지 대기.
        /// </summary>
        public async UniTask<ActionType> WaitForCommand(BattleEntity entity, CancellationToken ct = default)
        {
            _selectedCommand = default;
            _selectionMade = false;

            if (panel != null) panel.SetActive(true);
            if (entityNameText != null) entityNameText.text = entity.UnitData.unitName;

            await UniTask.WaitUntil(() => _selectionMade, cancellationToken: ct);

            if (panel != null) panel.SetActive(false);

            return _selectedCommand;
        }

        /// <summary>
        /// 패널 강제 닫기
        /// </summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
