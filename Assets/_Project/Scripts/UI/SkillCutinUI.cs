using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using TxRpg.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    public class SkillCutinUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private GameObject cutinPanel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text skillNameText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("설정")]
        [SerializeField] private float showDuration = 1f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private void Awake()
        {
            if (cutinPanel != null)
                cutinPanel.SetActive(false);
        }

        public async UniTask ShowCutinAsync(UnitDataSO unit, SkillDataSO skill, CancellationToken ct = default)
        {
            if (cutinPanel == null) return;

            // 설정
            if (portraitImage != null && unit.portrait != null)
                portraitImage.sprite = unit.portrait;

            if (skillNameText != null)
                skillNameText.text = skill.skillName;

            // 페이드 인
            cutinPanel.SetActive(true);
            if (canvasGroup != null)
            {
                await TweenHelper.FadeAsync(canvasGroup, 0f, 1f, fadeInDuration, ct);
            }

            // 표시 유지
            await UniTask.Delay((int)(showDuration * 1000), cancellationToken: ct);

            // 페이드 아웃
            if (canvasGroup != null)
            {
                await TweenHelper.FadeAsync(canvasGroup, 1f, 0f, fadeOutDuration, ct);
            }

            cutinPanel.SetActive(false);
        }
    }
}
