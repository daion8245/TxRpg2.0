using System.Collections.Generic;
using System.Linq;
using TMPro;
using TxRpg.Core;
using TxRpg.Data;
using TxRpg.Magic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// 탐험(퍼즐) 방의 실시간 주문 조합기(UGUI + TMP) · 조준(LineRenderer) · 시전 컨트롤러.
    /// 전투 씬과 동일한 SpellResolver.TryBuildSpell을 호출합니다.
    /// </summary>
    public class PuzzleRoomController : MonoBehaviour
    {
        [Header("월드 참조")]
        [SerializeField] private VillagePlayerMovement playerMovement;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private PuzzleDoor puzzleDoor;
        [SerializeField] private PuzzleImpactPlate impactPlate;
        [SerializeField] private PuzzleWordPickup[] wordPickups;
        [SerializeField] private PuzzleSpellProjectile projectilePrefab;
        [SerializeField] private GameObject playerSelfShieldVisual;
        [SerializeField] private LineRenderer aimGuideLine;

        [Header("UGUI + TMP 참조")]
        [SerializeField] private TMP_Text roomStatusText;
        [SerializeField] private TMP_Text sequenceText;
        [SerializeField] private TMP_Text previewText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text aimAngleText;
        [SerializeField] private Button[] wordButtons; // 0:Fire, 1:Shield, 2:Launch, 3:Strong
        [SerializeField] private Button castButton;
        [SerializeField] private Button backspaceButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button aimUpButton;
        [SerializeField] private Button aimDownButton;
        [SerializeField] private Button resetRoomButton;
        [SerializeField] private Button goBattleButton;

        [Header("조준 설정")]
        [SerializeField] private float aimDegrees = 22f;
        [SerializeField] private float aimMinDegrees = -35f;
        [SerializeField] private float aimMaxDegrees = 78f;
        [SerializeField] private float aimGuideLength = 9f;

        private readonly List<MagicWordType> _draftWords = new();
        private float _selfShieldTimer;
        private string _feedback = "이동(A/D), 점프(Space)로 발사 룬을 찾고 [발사 + 방패]로 오른쪽 위 충격판을 맞혀 문을 여세요!";

        private static readonly Color ColWordUnlocked = new(0.22f, 0.30f, 0.44f, 1f);
        private static readonly Color ColWordLocked = new(0.14f, 0.14f, 0.16f, 0.55f);

        private void Awake()
        {
            BindUI();
            if (playerSelfShieldVisual != null)
                playerSelfShieldVisual.SetActive(false);
        }

        private void Start()
        {
            if (wordPickups != null)
            {
                foreach (var p in wordPickups)
                {
                    if (p != null && GameSessionState.IsLearned(p.Word))
                        p.gameObject.SetActive(false);
                }
            }

            if (GameSessionState.PuzzleDoorOpened && puzzleDoor != null)
                puzzleDoor.Open();

            RefreshUI();
        }

        private void BindUI()
        {
            if (wordButtons != null)
            {
                for (int i = 0; i < wordButtons.Length && i < 4; i++)
                {
                    MagicWordType word = (MagicWordType)i;
                    wordButtons[i]?.onClick.AddListener(() => AddWord(word));
                }
            }

            castButton?.onClick.AddListener(TryCastSpell);
            backspaceButton?.onClick.AddListener(RemoveLastWord);
            clearButton?.onClick.AddListener(ClearWords);
            aimUpButton?.onClick.AddListener(() => NudgeAim(10f));
            aimDownButton?.onClick.AddListener(() => NudgeAim(-10f));
            resetRoomButton?.onClick.AddListener(ResetRoom);
            goBattleButton?.onClick.AddListener(() =>
            {
                GameSessionState.OpenDeckEditorOnBattleEnter = false;
                SceneManager.LoadScene(GameSessionState.BattleSceneName);
            });
        }

        private void Update()
        {
            if (_selfShieldTimer > 0f)
            {
                _selfShieldTimer -= Time.deltaTime;
                if (_selfShieldTimer <= 0f && playerSelfShieldVisual != null)
                    playerSelfShieldVisual.SetActive(false);
            }

            HandleShortcuts();
            UpdateAimGuide();
        }

        private void HandleShortcuts()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) AddWord(MagicWordType.Fire);
                if (kb.digit2Key.wasPressedThisFrame) AddWord(MagicWordType.Shield);
                if (kb.digit3Key.wasPressedThisFrame) AddWord(MagicWordType.Launch);
                if (kb.digit4Key.wasPressedThisFrame) AddWord(MagicWordType.Strong);
                if (kb.backspaceKey.wasPressedThisFrame) RemoveLastWord();
                if (kb.cKey.wasPressedThisFrame) ClearWords();
                if (kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) TryCastSpell();

                if (kb.qKey.isPressed || kb.upArrowKey.isPressed)
                    NudgeAim(80f * Time.deltaTime);
                if (kb.eKey.isPressed || kb.downArrowKey.isPressed)
                    NudgeAim(-80f * Time.deltaTime);
            }

            // 마우스 우클릭으로 마우스 방향 조준 + 시전 가능
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                AimAtMouse();
                TryCastSpell();
            }
        }

        private void AimAtMouse()
        {
            if (playerMovement == null || UnityEngine.Camera.main == null || Mouse.current == null) return;
            Vector3 world = UnityEngine.Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 delta = (Vector2)world - playerMovement.BodyCenter;
            if (delta.sqrMagnitude < 0.01f) return;

            float deg = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
            aimDegrees = Mathf.Clamp(deg, aimMinDegrees, aimMaxDegrees);
            RefreshUI();
        }

        private Vector2 GetAimDirection()
        {
            int dirX = playerMovement != null ? playerMovement.FacingDir : 1;
            float rad = aimDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * dirX, Mathf.Sin(rad)).normalized;
        }

        private void UpdateAimGuide()
        {
            if (aimGuideLine == null || playerMovement == null) return;

            Vector2 origin = playerMovement.BodyCenter + Vector2.up * 0.25f;
            Vector2 dir = GetAimDirection();
            Vector2 end = origin + dir * aimGuideLength;

            var hits = new RaycastHit2D[4];
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.Raycast(origin + dir * 0.45f, dir, filter, hits, aimGuideLength);
            RaycastHit2D hit = default;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<VillagePlayerMovement>() == null)
                {
                    hit = hits[i];
                    break;
                }
            }
            if (hit.collider != null)
            {
                end = hit.point;
                bool hitsPlate = hit.collider.GetComponent<PuzzleImpactPlate>() != null;
                Color lineCol = hitsPlate ? new Color(0.35f, 1f, 0.55f, 0.85f) : new Color(1f, 0.85f, 0.35f, 0.55f);
                aimGuideLine.startColor = lineCol;
                aimGuideLine.endColor = lineCol;
            }
            else
            {
                Color lineCol = new Color(1f, 0.85f, 0.35f, 0.45f);
                aimGuideLine.startColor = lineCol;
                aimGuideLine.endColor = lineCol;
            }

            aimGuideLine.positionCount = 2;
            aimGuideLine.SetPosition(0, new Vector3(origin.x, origin.y, -0.5f));
            aimGuideLine.SetPosition(1, new Vector3(end.x, end.y, -0.5f));
        }

        public void CollectWord(MagicWordType word)
        {
            GameSessionState.LearnWord(word);
            _feedback = $"새 단어 [{SpellResolver.WordName(word)}] 습득! 주문 조합에 사용할 수 있습니다.";
            RefreshUI();
        }

        public void OnDoorUnlocked()
        {
            _feedback = "충격판이 울리며 문이 열렸습니다! 오른쪽 열린 문 통로로 이동하면 전투 씬(덱 편성)으로 연결됩니다.";
            RefreshUI();
        }

        public void SetFeedback(string message)
        {
            _feedback = message;
            RefreshUI();
        }

        public void AddWord(MagicWordType word)
        {
            if (!GameSessionState.IsLearned(word))
            {
                _feedback = $"아직 [{SpellResolver.WordName(word)}] 단어를 배우지 않았습니다. 방에서 룬을 찾으세요.";
                RefreshUI();
                return;
            }
            if (_draftWords.Count >= 4)
            {
                _feedback = "주문은 최대 4단어까지 묶을 수 있습니다.";
                RefreshUI();
                return;
            }

            _draftWords.Add(word);
            _feedback = $"[{SpellResolver.WordName(word)}] 추가됨.";
            RefreshUI();
        }

        public void RemoveLastWord()
        {
            if (_draftWords.Count == 0) return;
            _draftWords.RemoveAt(_draftWords.Count - 1);
            RefreshUI();
        }

        public void ClearWords()
        {
            _draftWords.Clear();
            _feedback = "주문 조합을 비웠습니다.";
            RefreshUI();
        }

        public void NudgeAim(float deltaDegrees)
        {
            aimDegrees = Mathf.Clamp(aimDegrees + deltaDegrees, aimMinDegrees, aimMaxDegrees);
            RefreshUI();
        }

        public void TryCastSpell()
        {
            if (!SpellResolver.TryBuildSpell(_draftWords, out SpellData spell, out string error))
            {
                _feedback = $"시전 불가: {error}";
                RefreshUI();
                return;
            }

            _draftWords.Clear();
            Vector2 origin = playerMovement != null ? playerMovement.BodyCenter + Vector2.up * 0.25f : Vector2.zero;

            if (spell.IsLaunched)
            {
                if (projectilePrefab != null)
                {
                    Vector2 dir = GetAimDirection();
                    PuzzleSpellProjectile proj = Instantiate(projectilePrefab, origin + dir * 0.55f, Quaternion.identity);
                    proj.Launch(spell, dir, this);
                }
                _feedback = $"시전: [{spell.Name}] 발사! (조준 {Mathf.RoundToInt(aimDegrees)}°)";
            }
            else if (spell.Body == MagicWordType.Shield)
            {
                _selfShieldTimer = 1.2f;
                if (playerSelfShieldVisual != null) playerSelfShieldVisual.SetActive(true);
                _feedback = $"시전: [{spell.Name}] (제자리 방패). 멀리 있는 충격판에 닿게 하려면 [발사]를 함께 묶으세요!";
            }
            else
            {
                _feedback = $"시전: [{spell.Name}] (근거리 화염). 멀리 보내려면 [발사]를 함께 묶으세요!";
            }

            RefreshUI();
        }

        public void ResetRoom()
        {
            GameSessionState.ResetPuzzleProgress();
            _draftWords.Clear();
            puzzleDoor?.CloseImmediate();
            impactPlate?.ResetPlate();

            if (wordPickups != null)
                foreach (var p in wordPickups) p?.ResetPickup();

            if (playerMovement != null && spawnPoint != null)
                playerMovement.TeleportTo(spawnPoint.position);

            _feedback = "퍼즐 방을 초기화했습니다. 현재 습득 단어: 방패";
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (roomStatusText != null)
            {
                string learned = string.Join(", ", SpellResolver.AllWords
                    .Where(GameSessionState.IsLearned)
                    .Select(SpellResolver.WordName));
                string door = puzzleDoor != null && puzzleDoor.IsOpen ? "열림 (오른쪽 통로로 이동)" : "닫힘 (충격판 작동 필요)";
                roomStatusText.text = $"탐험 · 퍼즐 방  |  습득 단어: [{learned}]  |  문: {door}";
            }

            if (sequenceText != null)
            {
                sequenceText.text = _draftWords.Count == 0
                    ? "주문 조합: (단어 버튼 또는 1~4 키로 선택)"
                    : "주문 조합: [" + string.Join(" + ", _draftWords.Select(SpellResolver.WordName)) + "]";
            }

            if (previewText != null)
            {
                if (_draftWords.Count == 0)
                {
                    previewText.text = "미리보기: 단어를 조합해 F키 또는 시전 버튼으로 발동";
                    previewText.color = new Color(0.75f, 0.8f, 0.88f);
                }
                else if (SpellResolver.TryBuildSpell(_draftWords, out SpellData spell, out string err))
                {
                    previewText.text = $"미리보기: <b>{spell.Name}</b> (비용 {spell.Cost})";
                    previewText.color = new Color(0.55f, 0.95f, 0.72f);
                }
                else
                {
                    previewText.text = $"시전 불가: {err}";
                    previewText.color = new Color(1f, 0.45f, 0.45f);
                }
            }

            if (aimAngleText != null)
                aimAngleText.text = $"조준 {Mathf.RoundToInt(aimDegrees)}° (Q/E)";

            if (feedbackText != null)
                feedbackText.text = _feedback;

            if (wordButtons != null)
            {
                for (int i = 0; i < wordButtons.Length && i < 4; i++)
                {
                    Button btn = wordButtons[i];
                    if (btn == null) continue;
                    MagicWordType word = (MagicWordType)i;
                    bool learned = GameSessionState.IsLearned(word);
                    btn.interactable = learned;

                    Image img = btn.GetComponent<Image>();
                    if (img != null) img.color = learned ? ColWordUnlocked : ColWordLocked;

                    TMP_Text lbl = btn.GetComponentInChildren<TMP_Text>();
                    if (lbl != null)
                        lbl.text = learned ? $"{i + 1}. {SpellResolver.WordName(word)}" : $"{i + 1}. {SpellResolver.WordName(word)} (잠김)";
                }
            }
        }
    }
}
