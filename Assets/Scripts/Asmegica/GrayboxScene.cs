using System;
using System.Collections.Generic;
using System.Linq;
using TxRpg.Entity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Asmegica
{
    public sealed class GrayboxScene : MonoBehaviour
    {
        private enum Page { Setup, Deck, Battle, Explore }

        // 탐험 방 화면 배치: 세계 좌표 범위와 픽셀 크기. 세계 높이 11.4 단위, 너비 23 단위다.
        private const float RoomPixelWidth = 900f, RoomPixelHeight = 440f;
        private const float WorldMinX = -1.5f, WorldMaxX = 21.5f, WorldMinY = -1.2f, WorldMaxY = 10.2f;
        private const float GuiMoveHoldSeconds = 0.12f;

        // 일반 인카운터 시험값: 적 체력 4, 내 체력은 편성 화면에서 6·10·14 중 고른다.
        private const int NormalEnemyHp = 4;
        private static readonly int[] PlayerHpChoices = { 6, 10, 14 };
        // 편성 순서. deckCounts[i]는 AllWords[i]의 장수다.
        private static readonly MagicWord[] AllWords = { MagicWord.Fire, MagicWord.Shield, MagicWord.Launch, MagicWord.Strong };

        [SerializeField] private Font interfaceFont;
        private readonly FormationManager formation = new FormationManager(null);
        private readonly HashSet<MagicWord> learned = new HashSet<MagicWord>();
        private readonly int[] deckCounts = new int[4];
        private Page page = Page.Explore;
        private readonly GrayboxRoom room = new GrayboxRoom();
        private bool deckFromRoom;
        private bool battleFromRoom;
        private AudioClip plateChime;
        private GUIStyle roomLabel;
        private int guiMoveDir;
        private float guiMoveUntil;
        private bool guiJumpPending;
        private GrayboxBattle battle;
        private GrayboxBattleView battleView;
        private bool battleFromDeck;
        private int enemyCount = 1;
        private int enemyHp = 6;
        private int deckEnemyCount = 1;
        private int deckPlayerHp = 10;
        private string seedText = "1";
        private string deckMessage = string.Empty;
        private Vector2 scroll;
        private GUISkin skin;
        private Texture2D knight;
        private Texture2D slug;

        private void Awake()
        {
            knight = Resources.Load<Texture2D>("entitys/knight_m_idle_anim_f0");
            slug = Resources.Load<Texture2D>("entitys/slug_anim_f0");
            plateChime = MakePlateChime();
            room.PlateRang += PlayPlateChime;
        }

        private void Update()
        {
            if (page == Page.Explore) StepExploration(ReadRoomInput(), Time.deltaTime);
        }

        // 탐험 방 한 프레임. 키보드 입력과 검사 코드가 모두 이 진입점을 쓴다.
        public void StepExploration(RoomInput input, float dt)
        {
            if (page != Page.Explore) return;
            room.Step(input, dt);
            if (room.TakeTransition()) OpenDeckFromRoom();
        }

        public GrayboxRoom Room => room;
        public bool IsExploring => page == Page.Explore;
        public bool IsDeckOpen => page == Page.Deck;
        public bool IsBattleOpen => page == Page.Battle;
        public bool DeckOpenedFromRoom => deckFromRoom;
        public IReadOnlyList<MagicWord> DeckWords => BuildDeckWords();

        private void OnGUI()
        {
            if (!skin)
            {
                skin = Instantiate(GUI.skin);
                skin.font = interfaceFont;
                skin.label.fontSize = 16;
                skin.button.fontSize = 15;
                skin.box.fontSize = 15;
                skin.button.padding = new RectOffset(8, 8, 5, 5);
                skin.label.wordWrap = true;
            }
            var oldSkin = GUI.skin;
            var oldMatrix = GUI.matrix;
            GUI.skin = skin;
            float scale = Mathf.Clamp(Screen.width / 1050f, 0.25f, 1.65f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUILayout.BeginArea(new Rect(18, 10, width - 36, height - 20));
            scroll = GUILayout.BeginScrollView(scroll, false, false);
            GUILayout.Label("아스메지카   /   예지의 방");
            switch (page)
            {
                case Page.Explore:
                    DrawExplore();
                    break;
                case Page.Deck:
                    DrawDeckEditor();
                    break;
                case Page.Battle:
                    DrawFormation();
                    battleView.Draw();
                    if (battleFromRoom) DrawRoomResult();
                    break;
                default:
                    DrawSetup();
                    break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.skin = oldSkin;
            GUI.matrix = oldMatrix;
            if (page == Page.Battle && battleView.WantsExit) LeaveBattle();
        }

        // 탐험 방 입구: 습득한 단어 집합으로 덱 편성 화면을 연다. 자유 연습도 같은 입구를 쓴다.
        public void OpenDeckEditor(IEnumerable<MagicWord> learnedWords)
        {
            deckFromRoom = false;
            learned.Clear();
            if (learnedWords != null)
                foreach (MagicWord word in learnedWords)
                    if (Enum.IsDefined(typeof(MagicWord), word)) learned.Add(word);
            for (int i = 0; i < AllWords.Length; i++)
                deckCounts[i] = learned.Contains(AllWords[i]) ? 1 : 0;
            seedText = UnityEngine.Random.Range(1, 1000000).ToString();
            deckMessage = string.Empty;
            battle = null;
            battleView = null;
            battleFromDeck = false;
            page = Page.Deck;
            scroll = Vector2.zero;
        }

        private void DrawSetup()
        {
            GUILayout.Space(12);
            GUILayout.Label("고정 손패 실험");
            GUILayout.Label("슬러그의 다섯 행동을 읽고 화염·발사·방패를 주문으로 묶으세요.");
            GUILayout.BeginHorizontal();
            GUILayout.Label("적 수", GUILayout.Width(75));
            enemyCount = GUILayout.SelectionGrid(enemyCount - 1, new[] { "1마리", "2마리", "3마리" }, 3) + 1;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("적 체력", GUILayout.Width(75));
            enemyHp = GUILayout.SelectionGrid(enemyHp == 6 ? 0 : 1, new[] { "6 · 계획 비교", "4 · 처치 시 행동 취소" }, 2) == 0 ? 6 : 4;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("고정 손패 전투 시작"))
            {
                battle = new GrayboxBattle(enemyCount, enemyHp);
                battleView = new GrayboxBattleView(battle);
                battleFromDeck = false;
                battleFromRoom = false;
                page = Page.Battle;
                scroll = Vector2.zero;
            }

            GUILayout.Space(16);
            GUILayout.Label("자유 연습");
            GUILayout.Label("화염·방패·발사·강하게 4단어를 모두 습득한 상태로 덱을 편성해 일반 인카운터 규칙으로 싸웁니다.");
            if (GUILayout.Button("자유 연습 · 4단어 전부 덱 편성")) OpenDeckEditor(AllWords);

            GUILayout.Space(16);
            GUILayout.Label("탐험 방");
            if (GUILayout.Button("탐험 방으로 돌아가기 · 문과 충격판")) EnterRoom();
        }

        private void DrawDeckEditor()
        {
            GUILayout.Space(8);
            GUILayout.Label("편성 덱 · 일반 인카운터");
            if (deckFromRoom) GUILayout.Label("탐험 방의 문을 지나 열렸습니다. 전투가 끝나면 방으로 돌아갈 수 있습니다.");
            GUILayout.Label("습득한 단어만 넣을 수 있습니다. 덱은 3~8장, 단어당 최대 2장입니다.");
            for (int i = 0; i < AllWords.Length; i++)
            {
                bool known = learned.Contains(AllWords[i]);
                GUILayout.BeginHorizontal();
                GUILayout.Label(SpellResolver.WordName(AllWords[i]) + (known ? "" : " · 미습득"), GUILayout.Width(200));
                GUI.enabled = known && deckCounts[i] > 0;
                if (GUILayout.Button("-", GUILayout.Width(44))) AdjustDeck(AllWords[i], -1);
                GUI.enabled = true;
                GUILayout.Label(deckCounts[i] + "장", GUILayout.Width(60));
                GUI.enabled = known && deckCounts[i] < DeckRules.MaxPerWord && DeckSize() < DeckRules.MaxSize;
                if (GUILayout.Button("+", GUILayout.Width(44))) AdjustDeck(AllWords[i], 1);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            List<MagicWord> words = BuildDeckWords();
            bool valid = DeckRules.TryValidate(learned, words, out string error);
            GUILayout.Label(valid ? "덱 " + words.Count + "장 · 손패 최대 " + DeckRules.HandSize + "장 · 매 턴 마나 3" : error);

            GUILayout.BeginHorizontal();
            GUILayout.Label("적 수", GUILayout.Width(75));
            deckEnemyCount = GUILayout.SelectionGrid(deckEnemyCount - 1, new[] { "1마리", "2마리", "3마리" }, 3) + 1;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("내 체력", GUILayout.Width(75));
            int hpIndex = Math.Max(0, Array.IndexOf(PlayerHpChoices, deckPlayerHp));
            hpIndex = GUILayout.SelectionGrid(hpIndex, new[] { "6", "10", "14" }, 3);
            deckPlayerHp = PlayerHpChoices[hpIndex];
            GUILayout.EndHorizontal();
            GUILayout.Label("적 체력 " + NormalEnemyHp + " · 일반 인카운터 고정");

            GUILayout.BeginHorizontal();
            GUILayout.Label("시드", GUILayout.Width(75));
            seedText = GUILayout.TextField(seedText, GUILayout.Width(160));
            if (GUILayout.Button("새 시드", GUILayout.Width(110))) seedText = UnityEngine.Random.Range(1, 1000000).ToString();
            GUILayout.EndHorizontal();

            if (deckMessage.Length > 0) GUILayout.Label(deckMessage);
            GUILayout.BeginHorizontal();
            GUI.enabled = valid;
            if (GUILayout.Button("전투 시작", GUILayout.Width(160))) StartDeckBattle();
            GUI.enabled = true;
            if (GUILayout.Button(deckFromRoom ? "탐험 방으로" : "뒤로", GUILayout.Width(deckFromRoom ? 160 : 120)))
            {
                if (deckFromRoom)
                {
                    ReturnToRoom();
                }
                else
                {
                    page = Page.Setup;
                    deckMessage = string.Empty;
                    scroll = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
        }

        // 시작 전 검사: 덱 규칙, 시드, 적 수와 체력을 확인한 뒤에만 전투를 만든다.
        public void StartDeckBattle()
        {
            List<MagicWord> words = BuildDeckWords();
            if (!DeckRules.TryValidate(learned, words, out string error))
            {
                deckMessage = error;
                return;
            }
            if (!int.TryParse(seedText, out int seed))
            {
                deckMessage = "시드는 정수로 입력하세요.";
                return;
            }
            if (deckEnemyCount < 1 || deckEnemyCount > 3 || deckPlayerHp < 1)
            {
                deckMessage = "적은 1~3마리, 내 체력은 1 이상이어야 합니다.";
                return;
            }
            if (!CardDeck.TryCreate(learned, words, seed, out CardDeck deck, out error))
            {
                deckMessage = error;
                return;
            }

            battle = new GrayboxBattle(deckEnemyCount, NormalEnemyHp, deck.Draw, deck.Discard, deckPlayerHp);
            battleView = new GrayboxBattleView(battle)
            {
                PileInfo = () => "시드 " + seed + " · 뽑을 더미 " + deck.DrawPile.Count + " · 버린 더미 " + deck.DiscardPile.Count
            };
            battleFromDeck = true;
            battleFromRoom = deckFromRoom;
            deckMessage = string.Empty;
            page = Page.Battle;
            scroll = Vector2.zero;
        }

        // 전투가 끝나거나 나가면 덱 편성으로 돌아간다. 고정 손패 전투는 설정 화면으로 돌아간다.
        public void LeaveBattle()
        {
            battle = null;
            battleView = null;
            battleFromRoom = false;
            page = battleFromDeck ? Page.Deck : Page.Setup;
            scroll = Vector2.zero;
        }

        // 문을 지나면 열린다. 편성 화면은 기존 OpenDeckEditor를 그대로 쓰고, 방 출처만 표시한다.
        public void OpenDeckFromRoom()
        {
            OpenDeckEditor(room.Learned);
            deckFromRoom = true;
        }

        // 덱 편성 화면의 증감 버튼과 검사 코드가 함께 쓰는 진입점. 습득 단어와 장수 한도를 지킨다.
        public bool AdjustDeck(MagicWord word, int delta)
        {
            int index = Array.IndexOf(AllWords, word);
            if (index < 0 || !learned.Contains(word)) return false;
            int next = deckCounts[index] + delta;
            if (next < 0 || next > DeckRules.MaxPerWord) return false;
            if (delta > 0 && DeckSize() >= DeckRules.MaxSize) return false;
            deckCounts[index] = next;
            return true;
        }

        // 방으로 돌아간다. 문 상태와 습득 단어는 그대로 두고 문 앞에 세운다.
        public void ReturnToRoom()
        {
            battle = null;
            battleView = null;
            battleFromDeck = false;
            battleFromRoom = false;
            deckMessage = string.Empty;
            deckFromRoom = false;   // 방으로 돌아가면 방에서 연 덱 표시를 지운다
            room.ReturnToDoorApproach();
            page = Page.Explore;
            scroll = Vector2.zero;
        }

        // 설정 화면에서 방으로 들어간다. 멈춘 자리와 문 상태는 그대로 두고, 문을 이미 지났다면 문 앞에 세운다(덱으로 바로 튕기지 않도록).
        public void EnterRoom()
        {
            deckFromRoom = false;
            if (room.PassedDoor) room.ReturnToDoorApproach();
            battle = null;
            battleView = null;
            battleFromDeck = false;
            battleFromRoom = false;
            page = Page.Explore;
            scroll = Vector2.zero;
        }

        // 방을 처음 상태로 되돌린다: 문 닫힘, 방패만 습득, 입구에서 시작.
        public void ResetRoom()
        {
            room.Reset();
            deckFromRoom = false;
            battle = null;
            battleView = null;
            battleFromDeck = false;
            battleFromRoom = false;
            deckMessage = string.Empty;
            page = Page.Explore;
            scroll = Vector2.zero;
        }

        // 탐험 방 페이지: 위치·상태, 방 그림, 조작 안내, 주문 미리보기, 화면 버튼을 한 화면에 둔다.
        private void DrawExplore()
        {
            GUILayout.Space(6);
            GUILayout.Label("탐험 · 예지의 방   /   위치: " + ZoneName());
            var field = GUILayoutUtility.GetRect(RoomPixelWidth, RoomPixelHeight);
            DrawRoom(field);
            GUILayout.Label("문: " + (room.DoorOpen ? "열림" : "닫힘") + " · 충격판: " + PlateStateText()
                + " · 습득: " + LearnedText() + " · 조준 " + Mathf.RoundToInt(room.AimDegrees) + "°");
            GUILayout.Label("조작: ←→ / A D 이동 · Space 점프 · ↑↓ / W S 조준 · 1 화염 · 2 방패 · 3 발사 · 4 강하게 · Backspace 한 칸 지우기 · Delete 전부 지우기 · Enter 시전");
            GUILayout.Label("주문: " + GrayboxRoom.SequenceText(room.Words) + " · " + room.PreviewText());
            GUILayout.Label("안내: " + room.Message);

            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("← 왼쪽", GUILayout.Width(120))) HoldMove(-1);
            if (GUILayout.RepeatButton("오른쪽 →", GUILayout.Width(120))) HoldMove(1);
            if (GUILayout.Button("점프 (Space)", GUILayout.Width(130))) guiJumpPending = true;
            if (GUILayout.Button("조준 ↑ 5°", GUILayout.Width(110))) room.NudgeAim(5f);
            if (GUILayout.Button("조준 ↓ 5°", GUILayout.Width(110))) room.NudgeAim(-5f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawWordButton(MagicWord.Fire, "1 화염");
            DrawWordButton(MagicWord.Shield, "2 방패");
            DrawWordButton(MagicWord.Launch, "3 발사");
            DrawWordButton(MagicWord.Strong, "4 강하게");
            if (GUILayout.Button("한 칸 지우기", GUILayout.Width(120))) room.RemoveLastWord();
            if (GUILayout.Button("전부 지우기", GUILayout.Width(120))) room.ClearWords();
            if (GUILayout.Button("시전 (Enter)", GUILayout.Width(130))) room.TryCast(out _);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("설정 화면 · 고정 손패 / 자유 연습", GUILayout.Width(260)))
            {
                page = Page.Setup;
                scroll = Vector2.zero;
            }
            if (GUILayout.Button("방 초기화", GUILayout.Width(120))) ResetRoom();
            GUILayout.EndHorizontal();
        }

        private void DrawWordButton(MagicWord word, string caption)
        {
            // 미습득 단어도 누르면 한국어 사유를 안내 줄에 보여 준다.
            if (GUILayout.Button(caption, GUILayout.Width(120))) room.AddWord(word, out _);
        }

        private void DrawRoom(Rect field)
        {
            float worldWidth = WorldMaxX - WorldMinX;
            float worldHeight = WorldMaxY - WorldMinY;
            float unit = Mathf.Min(field.width / worldWidth, field.height / worldHeight);
            float originX = field.x + (field.width - worldWidth * unit) / 2f;
            float originY = field.y + (field.height - worldHeight * unit) / 2f;
            Rect ToGui(Rect w) => new Rect(originX + (w.xMin - WorldMinX) * unit, originY + (WorldMaxY - w.yMax) * unit, w.width * unit, w.height * unit);
            Vector2 ToGuiPoint(Vector2 p) => new Vector2(originX + (p.x - WorldMinX) * unit, originY + (WorldMaxY - p.y) * unit);

            Fill(field, new Color(0.05f, 0.06f, 0.09f));
            IReadOnlyList<Rect> walls = GrayboxRoom.FixedWalls;
            for (int i = 0; i < walls.Count; i++)
            {
                Rect w = walls[i];
                if (w == GrayboxRoom.Plate) continue;
                bool ledge = w.yMin == 0f && w.xMin >= 0f && w.xMax <= 13f;
                Fill(ToGui(w), ledge ? new Color(0.36f, 0.4f, 0.5f) : new Color(0.18f, 0.2f, 0.26f));
            }

            for (int i = 0; i < GrayboxRoom.RuneWords.Length; i++)
            {
                MagicWord word = GrayboxRoom.RuneWords[i];
                Rect zone = GrayboxRoom.RuneZones[i];
                bool got = room.IsLearned(word);
                Color tone = got ? new Color(0.55f, 0.55f, 0.6f) : RuneColor(word);
                Fill(ToGui(new Rect(zone.center.x - 0.2f, zone.yMin, 0.4f, 0.4f)), tone);
                string title = SpellResolver.WordName(word);
                RoomText(ToGui(new Rect(zone.xMin, zone.yMax, zone.width, 0.5f)),
                    got ? title + " · 습득 완료" : title + " 발판 · 올라서서 얻기", tone);
            }

            var door = ToGui(GrayboxRoom.Door);
            if (room.DoorOpen)
            {
                Frame(door, new Color(0.35f, 0.9f, 0.5f, 0.9f), 3f);
                RoomText(new Rect(door.x - 12f, door.y - 22f, 150f, 20f), "문 · 열림 → 통로", new Color(0.6f, 1f, 0.7f));
            }
            else
            {
                Fill(door, new Color(0.55f, 0.22f, 0.18f));
                RoomText(new Rect(door.x - 12f, door.y - 22f, 150f, 20f), "문 · 닫힘", new Color(1f, 0.7f, 0.6f));
            }

            var plate = ToGui(GrayboxRoom.Plate);
            Color plateColor = room.PlateFlashLeft > 0f ? new Color(1f, 0.84f, 0.31f)
                : room.DoorOpen ? new Color(0.4f, 0.85f, 0.5f) : new Color(0.45f, 0.62f, 0.85f);
            Fill(plate, plateColor);
            RoomText(new Rect(plate.x - 30f, plate.y - 22f, 160f, 20f), "충격판 · " + PlateStateText(), plateColor);
            RoomText(ToGui(new Rect(0.2f, 4f, 2.4f, 0.6f)), "입구 · 시작", new Color(0.8f, 0.85f, 0.95f));

            if (!room.ShotInFlight)
            {
                Vector2 end = room.PredictEnd(out bool reaches);
                Vector2 start = room.PlayerCenter;
                Color dot = reaches ? new Color(1f, 0.84f, 0.31f, 0.95f) : new Color(0.8f, 0.85f, 0.95f, 0.5f);
                const int dots = 18;
                for (int k = 1; k <= dots; k++)
                {
                    Vector2 p = ToGuiPoint(Vector2.Lerp(start, end, k / (dots + 1f)));
                    Fill(new Rect(p.x - 2f, p.y - 2f, 4f, 4f), dot);
                }
            }
            else
            {
                Vector2 p = ToGuiPoint(room.ShotPosition);
                Color shot = room.ShotBurning ? new Color(1f, 0.55f, 0.2f)
                    : room.ShotStrong ? new Color(0.7f, 0.95f, 1f) : new Color(0.85f, 0.9f, 1f);
                Fill(new Rect(p.x - 0.18f * unit, p.y - 0.18f * unit, 0.36f * unit, 0.36f * unit), shot);
            }

            Vector2 c = room.PlayerCenter;
            if (room.SelfShieldLeft > 0f)
                Frame(ToGui(new Rect(c.x - GrayboxRoom.HalfWidth - 0.15f, c.y - GrayboxRoom.HalfHeight - 0.15f,
                    2f * GrayboxRoom.HalfWidth + 0.3f, 2f * GrayboxRoom.HalfHeight + 0.3f)), new Color(0.5f, 0.8f, 1f, 0.9f), 3f);
            var body = ToGui(new Rect(c.x - 0.7f, c.y - 0.7f, 1.4f, 1.4f));
            if (knight) GUI.DrawTexture(body, knight, ScaleMode.ScaleToFit);
            else Fill(body, new Color(0.95f, 0.8f, 0.3f));
        }

        private string ZoneName()
        {
            float x = room.PlayerCenter.x;
            if (x < 6f) return "입구 구역";
            if (x < 12f) return "발판 구역";
            if (x <= 15.5f) return "충격판 · 문 앞";
            return "통로";
        }

        private string PlateStateText()
        {
            if (room.PlateFlashLeft > 0f) return "울림";
            return room.DoorOpen ? "열린 상태" : "대기";
        }

        private string LearnedText()
        {
            return string.Join(", ", room.Learned.OrderBy(word => (int)word).Select(word => SpellResolver.WordName(word)));
        }

        private static Color RuneColor(MagicWord word)
        {
            switch (word)
            {
                case MagicWord.Launch: return new Color(0.4f, 0.85f, 1f);
                case MagicWord.Fire: return new Color(1f, 0.6f, 0.25f);
                default: return new Color(0.75f, 0.5f, 1f);
            }
        }

        private GUIStyle RoomLabelStyle()
        {
            if (roomLabel == null) roomLabel = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            return roomLabel;
        }

        private void RoomText(Rect rect, string text, Color color)
        {
            GUIStyle style = RoomLabelStyle();
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void Frame(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        // 화면 버튼은 누른 순간부터 짧게 이동을 유지한다. RepeatButton이 프레임마다 다시 불러 준다.
        private void HoldMove(int direction)
        {
            guiMoveDir = direction;
            guiMoveUntil = Time.unscaledTime + GuiMoveHoldSeconds;
        }

        private RoomInput ReadRoomInput()
        {
            var input = new RoomInput();
            if (Time.unscaledTime < guiMoveUntil)
            {
                input.Left = guiMoveDir < 0;
                input.Right = guiMoveDir > 0;
            }
            if (guiJumpPending)
            {
                input.JumpPressed = true;
                guiJumpPending = false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return input;
            input.Left |= keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            input.Right |= keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            input.AimUp = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            input.AimDown = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            input.JumpPressed |= keyboard.spaceKey.wasPressedThisFrame;
            input.Backspace = keyboard.backspaceKey.wasPressedThisFrame;
            input.ClearWords = keyboard.deleteKey.wasPressedThisFrame;
            input.CastPressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            input.WordPressed = WordFromKeys(keyboard);
            return input;
        }

        private static MagicWord? WordFromKeys(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return MagicWord.Fire;
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return MagicWord.Shield;
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return MagicWord.Launch;
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return MagicWord.Strong;
            return null;
        }

        // 방에서 시작한 전투가 끝나면 방으로 돌아가기 · 같은 덱 다시 · 방 초기화를 보여 준다.
        private void DrawRoomResult()
        {
            GUILayout.Space(8);
            RoundPhase phase = battle.Phase;
            if (phase != RoundPhase.Victory && phase != RoundPhase.Defeat)
            {
                GUILayout.Label("전투가 끝나면 결과 버튼이 나타납니다. 나가기는 덱 편성으로 돌아갑니다.");
                return;
            }
            GUILayout.Label(phase == RoundPhase.Victory ? "전투 결과 · 승리" : "전투 결과 · 패배");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("탐험 방으로 돌아가기", GUILayout.Width(220))) ReturnToRoom();
            if (GUILayout.Button("같은 덱·시드로 다시", GUILayout.Width(220))) StartDeckBattle();
            if (GUILayout.Button("방 초기화", GUILayout.Width(140))) ResetRoom();
            GUILayout.EndHorizontal();
        }

        // 짧은 두 음 사이를 오가는 차임. 파일 없이 코드로 만든다.
        private static AudioClip MakePlateChime()
        {
            const int rate = 22050;
            int length = Mathf.RoundToInt(rate * 0.32f);
            var samples = new float[length];
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                phase += 2f * Mathf.PI * Mathf.Lerp(660f, 990f, t) / rate;
                float fade = Mathf.Min(1f, i / (rate * 0.01f)) * (1f - t);
                samples[i] = Mathf.Sin(phase) * fade * 0.5f;
            }
            var clip = AudioClip.Create("AsmegicaPlateChime", length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayPlateChime()
        {
            if (plateChime != null) AudioSource.PlayClipAtPoint(plateChime, Vector3.zero, 0.6f);
        }

        private List<MagicWord> BuildDeckWords()
        {
            var words = new List<MagicWord>();
            for (int i = 0; i < AllWords.Length; i++)
                for (int c = 0; c < deckCounts[i]; c++) words.Add(AllWords[i]);
            return words;
        }

        private int DeckSize() => deckCounts.Sum();

        private void DrawFormation()
        {
            var field = GUILayoutUtility.GetRect(400, 104, GUILayout.ExpandWidth(true));
            GUI.Box(field, GUIContent.none);
            DrawUnit(field, formation.GetPlayerSlot(0), knight, "나");
            for (int i = 0; i < battle.EnemyCount; i++)
            {
                var tint = GUI.color;
                if (battle.GetEnemyHp(i) == 0) GUI.color = Color.gray;
                var position = formation.GetEnemySlot(i);
                if (battle.IsNear(i)) position.x -= 1.3f;
                DrawUnit(field, position, slug, "적 " + (i + 1));
                GUI.color = tint;
            }
        }

        private static void DrawUnit(Rect field, Vector2 slot, Texture2D texture, string caption)
        {
            var x = field.center.x + slot.x * field.width * 0.086f;
            var y = field.center.y - slot.y * 24;
            if (texture) GUI.DrawTexture(new Rect(x - 24, y - 28, 48, 48), texture, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(x + 18, y - 10, 90, 26), caption);
        }

        private void OnDestroy()
        {
            if (skin) Destroy(skin);
        }
    }
}
