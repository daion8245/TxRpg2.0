using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TxRpg.Data;
using TxRpg.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    public class SkillSelectUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private GameObject skillButtonPrefab;
        [SerializeField] private Text entityNameText;

        private SkillDataSO _selectedSkill;
        private BattleEntity _selectedTarget;
        private bool _selectionMade;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public async UniTask<SkillDataSO> WaitForSelection(BattleEntity entity, CancellationToken ct = default)
        {
            _selectedSkill = null;
            _selectionMade = false;

            if (panel != null) panel.SetActive(true);
            if (entityNameText != null) entityNameText.text = entity.UnitData.unitName;

            // 스킬 버튼 생성
            ClearButtons();

            var allSkills = entity.Skills.AllSkills;
            for (int i = 0; i < allSkills.Length; i++)
            {
                var skill = allSkills[i];
                bool canUse = entity.Skills.CanUse(i, entity.Stats.CurrentMp);
                CreateSkillButton(skill, canUse);
            }

            // 선택 대기
            await UniTask.WaitUntil(() => _selectionMade, cancellationToken: ct);

            if (panel != null) panel.SetActive(false);

            return _selectedSkill;
        }

        public async UniTask<List<BattleEntity>> WaitForTargetSelection(
            SkillDataSO skill, BattleEntity caster,
            List<BattleEntity> allies, List<BattleEntity> enemies,
            CancellationToken ct = default)
        {
            switch (skill.targetType)
            {
                case TargetType.AllEnemies:
                    return enemies.FindAll(e => e.IsAlive);

                case TargetType.AllAllies:
                    return allies.FindAll(e => e.IsAlive);

                case TargetType.Self:
                    return new List<BattleEntity> { caster };

                case TargetType.SingleEnemy:
                    var target = await WaitForSingleTarget(enemies.FindAll(e => e.IsAlive), ct);
                    return new List<BattleEntity> { target };

                case TargetType.SingleAlly:
                    var allyTarget = await WaitForSingleTarget(allies.FindAll(e => e.IsAlive), ct);
                    return new List<BattleEntity> { allyTarget };

                default:
                    return new List<BattleEntity>();
            }
        }

        private async UniTask<BattleEntity> WaitForSingleTarget(List<BattleEntity> candidates, CancellationToken ct)
        {
            _selectedTarget = null;

            // 후보가 1명이면 자동 선택
            if (candidates.Count == 1)
                return candidates[0];

            // 클릭 대기 (실제 구현 시 클릭 검출 로직 추가)
            // 임시: 첫 번째 타겟 자동 선택
            _selectedTarget = candidates.Count > 0 ? candidates[0] : null;
            await UniTask.Yield(ct);

            return _selectedTarget;
        }

        private void CreateSkillButton(SkillDataSO skill, bool interactable)
        {
            if (skillButtonPrefab == null || buttonRoot == null) return;

            var go = Instantiate(skillButtonPrefab, buttonRoot);
            var button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<Text>();

            if (text != null)
                text.text = $"{skill.skillName}\nMP:{skill.mpCost}";

            if (button != null)
            {
                button.interactable = interactable;
                var capturedSkill = skill;
                button.onClick.AddListener(() =>
                {
                    _selectedSkill = capturedSkill;
                    _selectionMade = true;
                });
            }
        }

        private void ClearButtons()
        {
            if (buttonRoot == null) return;

            for (int i = buttonRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonRoot.GetChild(i).gameObject);
            }
        }
    }
}
