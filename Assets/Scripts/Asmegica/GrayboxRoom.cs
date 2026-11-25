using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asmegica
{
    // 탐험 방의 입력 한 프레임 분량. GrayboxScene이 키보드에서 만들고, 검사 코드도 같은 형식을 쓴다.
    public struct RoomInput
    {
        public bool Left;
        public bool Right;
        public bool AimUp;
        public bool AimDown;
        public bool JumpPressed;
        public MagicWord? WordPressed;
        public bool Backspace;
        public bool ClearWords;
        public bool CastPressed;
    }

    // 탐험 방의 상태와 2D 키네마틱 충돌. 렌더링은 GrayboxScene이 맡는다.
    // 문은 충격판이 "던진 방패"에 실제로 닿을 때 한 번만 열리고, 열린 뒤 통로 끝을 넘으면 덱 편성 전환을 요청한다.
    public sealed class GrayboxRoom
    {
        public const float WalkSpeed = 6f;
        public const float Gravity = 28f;
        public const float JumpVelocity = 12.5f;
        public const float MaxFall = 30f;
        public const float HalfWidth = 0.35f;
        public const float HalfHeight = 0.6f;
        public const float ShotSpeed = 16f;
        public const float ShotStrongSpeed = 24f;
        public const float ShotRange = 19f;
        public const float ShotRadius = 0.15f;
        public const float AimSpeed = 90f;
        public const float AimMin = -45f;
        public const float AimMax = 80f;
        public const float DefaultAim = 20f;
        public const int MaxWords = 4;
        public const float PlateFlashSeconds = 0.8f;
        public const float SelfShieldSeconds = 1.2f;

        private const float Substep = 1f / 120f;
        private const float MaxFrameSeconds = 0.1f;
        // 몸과 고정 충돌체가 이보다 얕게 겹치면 접촉으로 본다. 발판 윗면에 선 발 아래면은 2f + .6f의 반올림 때문에
        // 윗면보다 약 1.2e-7 아래로 잡힌다. 이를 겹침으로 세면 다음 옆 이동이 발판 옆면 밖으로 밀어내 올라선 발판에서 떨어진다.
        private const float ContactSkin = 1e-4f;

        public static readonly Vector2 Spawn = new Vector2(3f, 0.6f);
        public static readonly Vector2 DoorApproach = new Vector2(14.5f, 0.6f);

        // 문은 방의 오른쪽 끝에 있고, 통로는 x 17 이후다. 플레이어의 왼쪽 끝이 17을 넘으면 덱 전환이다.
        public static readonly Rect Door = new Rect(16f, 0f, 1f, 9f);
        public static readonly Rect Plate = new Rect(12.6f, 5.5f, 0.4f, 2f);
        public const float DoorRight = 17f;

        // 고정 충돌체: 바닥, 벽, 천장, 발판 세 개, 화염 발판 오른쪽 디딤턱, 충격판. 발사 발판은 강하게 발판의 던지기 선을 막지 않도록 왼쪽에 둔다.
        // 디딤턱은 문 앞 바닥에서 뛰어 올라설 수 있는 높이(2)다. 화염 발판(4) 오른쪽 면에 붙어 있어 조준선과 문 앞 통로를 막지 않는다.
        private static readonly Rect[] FixedSolids =
        {
            new Rect(-1f, -1f, 22f, 1f),   // 바닥
            new Rect(-1f, 0f, 1f, 10f),    // 입구 쪽 벽
            new Rect(-1f, 9f, 22f, 1f),    // 천장
            new Rect(20f, 0f, 1f, 10f),    // 통로 끝 벽
            new Rect(0f, 0f, 1.8f, 2.2f),  // 강하게 발판
            new Rect(6.5f, 0f, 2f, 2f),    // 발사 발판
            new Rect(10.5f, 0f, 1.5f, 4f), // 화염 발판
            new Rect(12f, 0f, 1f, 2f),     // 화염 발판 오른쪽 디딤턱: 문 앞 바닥에서 뛰어 올라 기둥 위로 넘는다
            Plate,                         // 충격판
        };

        // 단어를 얻는 구역. 플레이어 상자가 구역과 겹치면 습득한다.
        public static readonly MagicWord[] RuneWords = { MagicWord.Launch, MagicWord.Fire, MagicWord.Strong };
        public static readonly Rect[] RuneZones =
        {
            new Rect(6.6f, 2f, 1.8f, 1.2f),
            new Rect(10.6f, 4f, 1.3f, 1.2f),
            new Rect(0.2f, 2.2f, 1.4f, 1.2f),
        };

        private readonly HashSet<MagicWord> learned = new HashSet<MagicWord>();
        private readonly List<MagicWord> words = new List<MagicWord>();
        private readonly List<Rect> solids = new List<Rect>();

        private Vector2 center;
        private Vector2 velocity;
        private bool grounded;
        private float aimDegrees;
        private bool doorOpen;
        private int doorOpenings;
        private int plateHits;
        private float plateFlash;
        private float selfShield;
        private bool transitionPending;
        private string message = string.Empty;

        private bool shotActive;
        private Vector2 shotPos;
        private Vector2 shotDir;
        private float shotSpeed;
        private float shotTravel;
        private bool shotTriggersPlate;
        private bool shotBurning;
        private bool shotStrong;
        private string shotName = string.Empty;

        // 충격판이 처음 열릴 때만 울린다. 이후의 충돌에는 소리를 내지 않는다.
        public event Action PlateRang;

        public GrayboxRoom()
        {
            Reset();
        }

        public Vector2 PlayerCenter => center;
        public bool Grounded => grounded;
        public float AimDegrees => aimDegrees;
        public bool DoorOpen => doorOpen;
        // 열린 문의 통로 기준(오른쪽 17)을 넘었는지. 넘은 상태로 탐험에 다시 들어가면 덱 전환이 곧바로 다시 걸리므로 문 앞으로 옮긴다.
        public bool PassedDoor => doorOpen && center.x - HalfWidth >= DoorRight;
        // 문이 실제로 열린 횟수. 한 번 열린 뒤에는 1을 넘지 않아야 한다.
        public int DoorOpenings => doorOpenings;
        // 충격판에 투척 방패가 닿은 횟수. 열린 뒤의 중복 충돌도 센다.
        public int PlateHits => plateHits;
        public float PlateFlashLeft => plateFlash;
        public float SelfShieldLeft => selfShield;
        public bool ShotInFlight => shotActive;
        public Vector2 ShotPosition => shotPos;
        public bool ShotBurning => shotBurning;
        public bool ShotStrong => shotStrong;
        public string ShotName => shotName;
        public IReadOnlyList<MagicWord> Words => words;
        public IEnumerable<MagicWord> Learned => learned;
        public bool IsLearned(MagicWord word) => learned.Contains(word);
        public string Message => message;
        public static IReadOnlyList<Rect> FixedWalls => FixedSolids;

        public void Reset()
        {
            learned.Clear();
            learned.Add(MagicWord.Shield);
            words.Clear();
            doorOpen = false;
            doorOpenings = 0;
            plateHits = 0;
            plateFlash = 0f;
            selfShield = 0f;
            transitionPending = false;
            shotActive = false;
            aimDegrees = DefaultAim;
            RebuildSolids();
            PlaceAt(Spawn);
            message = "방을 처음 상태로 되돌렸습니다. 방패만 알고 있습니다.";
        }

        // 덱 편성에서 돌아온 뒤: 문 상태와 습득 단어는 유지하고 문 앞에 세운다.
        public void ReturnToDoorApproach()
        {
            words.Clear();
            shotActive = false;
            transitionPending = false;
            PlaceAt(DoorApproach);
            message = doorOpen
                ? "방으로 돌아왔습니다. 문은 열린 상태입니다. 통로로 다시 들어가면 덱 편성이 열립니다."
                : "방으로 돌아왔습니다. 문은 아직 닫혀 있습니다.";
        }

        public void PlaceAt(Vector2 position)
        {
            center = position;
            velocity = Vector2.zero;
            grounded = false;
        }

        public bool TakeTransition()
        {
            bool pending = transitionPending;
            transitionPending = false;
            return pending;
        }

        public bool AddWord(MagicWord word, out string error)
        {
            error = string.Empty;
            if (!Enum.IsDefined(typeof(MagicWord), word))
                error = "정의되지 않은 단어입니다.";
            else if (!learned.Contains(word))
                error = "아직 습득하지 않은 단어입니다: " + SpellResolver.WordName(word);
            else if (words.Count >= MaxWords)
                error = "주문은 최대 " + MaxWords + "단어입니다.";

            if (error.Length > 0)
            {
                message = error;
                return false;
            }
            words.Add(word);
            message = SpellResolver.WordName(word) + " 단어를 주문에 넣었습니다.";
            return true;
        }

        public bool RemoveLastWord()
        {
            if (words.Count == 0) return false;
            words.RemoveAt(words.Count - 1);
            message = "마지막 단어를 뺐습니다.";
            return true;
        }

        public void ClearWords()
        {
            words.Clear();
            message = "주문 단어를 모두 뺐습니다.";
        }

        // 화면 버튼용 조준 조정. 키 입력의 AimUp/AimDown과 같은 범위로 자른다.
        public void NudgeAim(float degrees)
        {
            aimDegrees = Mathf.Clamp(aimDegrees + degrees, AimMin, AimMax);
        }

        // 입력과 자원은 실패해도 그대로 둔다. 성공하면 단어 목록을 비운다.
        public bool TryCast(out string error)
        {
            error = string.Empty;
            if (shotActive)
            {
                error = "투척 방패가 아직 날아가는 중입니다.";
                message = error;
                return false;
            }
            if (!SpellResolver.TryBuildSpell(words, out Spell spell, out error))
            {
                message = "주문을 시전할 수 없습니다: " + error;
                return false;
            }

            words.Clear();
            if (spell.IsLaunched)
            {
                StartShot(spell);
                message = "시전 · " + spell.Name + " · 조준 " + Mathf.RoundToInt(aimDegrees) + "도";
            }
            else if (spell.Body == MagicWord.Shield)
            {
                selfShield = SelfShieldSeconds;
                message = "시전 · " + spell.Name + " · 제자리 방패는 충격판에 닿지 않습니다. 발사를 함께 넣으세요.";
            }
            else
            {
                message = "시전 · " + spell.Name + " · 발사하지 않아 충격판에 닿지 않습니다.";
            }
            return true;
        }

        public string PreviewText()
        {
            if (words.Count == 0) return "주문 미리보기: 단어를 넣으세요. 예) 방패 → 발사";
            if (SpellResolver.TryBuildSpell(words, out Spell spell, out string error))
                return "미리보기: " + spell.Name + " · 비용 " + spell.Cost;
            return "미리보기 불가: " + error;
        }

        public static string SequenceText(IEnumerable<MagicWord> sequence)
        {
            var parts = new List<string>();
            foreach (MagicWord word in sequence) parts.Add(SpellResolver.WordName(word));
            return parts.Count == 0 ? "(비어 있음)" : string.Join(" → ", parts);
        }

        // 던진 방패의 예상 경로. 실제 비행과 같은 선분 판정을 쓴다.
        public Vector2 PredictEnd(out bool reachesPlate)
        {
            Vector2 dir = AimDirection();
            Vector2 origin = center;
            Vector2 to = origin + dir * ShotRange;
            float bestT = float.MaxValue;
            int best = -1;
            for (int i = 0; i < solids.Count; i++)
            {
                if (SegmentHits(origin, to, Inflate(solids[i], ShotRadius), out float t) && t < bestT)
                {
                    bestT = t;
                    best = i;
                }
            }
            reachesPlate = best >= 0 && solids[best] == Plate;
            return best >= 0 ? Vector2.Lerp(origin, to, bestT) : to;
        }

        // 한 프레임 진행. 입력 단어·시전은 키 입력과 같은 메서드를 거치므로 검사 코드와 실제 게임이 같은 경로다.
        public void Step(RoomInput input, float dt)
        {
            dt = Mathf.Clamp(dt, 0f, MaxFrameSeconds);
            plateFlash = Mathf.Max(0f, plateFlash - dt);
            selfShield = Mathf.Max(0f, selfShield - dt);
            aimDegrees = Mathf.Clamp(aimDegrees + ((input.AimUp ? 1f : 0f) - (input.AimDown ? 1f : 0f)) * AimSpeed * dt, AimMin, AimMax);

            if (input.WordPressed.HasValue) AddWord(input.WordPressed.Value, out _);
            if (input.Backspace) RemoveLastWord();
            if (input.ClearWords) ClearWords();
            if (input.CastPressed) TryCast(out _);

            if (transitionPending) return;

            float move = (input.Right ? 1f : 0f) - (input.Left ? 1f : 0f);
            bool jump = input.JumpPressed;
            float remaining = dt;
            while (remaining > 0f)
            {
                float step = Mathf.Min(Substep, remaining);
                remaining -= step;
                MoveBody(move, jump, step);
                jump = false;
                if (shotActive) AdvanceShot(step);
            }

            CheckRunes();
            if (PassedDoor)
            {
                transitionPending = true;
                message = "문을 지나 편성 덱으로 들어갑니다.";
            }
        }

        private Vector2 AimDirection()
        {
            float rad = aimDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void StartShot(Spell spell)
        {
            shotDir = AimDirection();
            shotSpeed = spell.IsStrong ? ShotStrongSpeed : ShotSpeed;
            shotPos = center;
            shotTravel = 0f;
            shotActive = true;
            shotTriggersPlate = spell.Body == MagicWord.Shield;
            shotBurning = spell.IsBurning;
            shotStrong = spell.IsStrong;
            shotName = spell.Name;
        }

        private void AdvanceShot(float dt)
        {
            float distance = shotSpeed * dt;
            Vector2 origin = shotPos;
            Vector2 to = origin + shotDir * distance;
            float bestT = float.MaxValue;
            int best = -1;
            for (int i = 0; i < solids.Count; i++)
            {
                if (SegmentHits(origin, to, Inflate(solids[i], ShotRadius), out float t) && t < bestT)
                {
                    bestT = t;
                    best = i;
                }
            }

            if (best >= 0)
            {
                shotPos = Vector2.Lerp(origin, to, bestT);
                EndShot(solids[best] == Plate);
                return;
            }

            shotPos = to;
            shotTravel += distance;
            if (shotTravel >= ShotRange) EndShot(false);
        }

        private void EndShot(bool struckPlate)
        {
            shotActive = false;
            if (struckPlate && shotTriggersPlate)
            {
                OnPlateStruck();
                return;
            }
            if (struckPlate)
            {
                message = shotName + " · 충격판을 울리지 않습니다. 투척 방패만 판에 닿을 수 있습니다.";
                return;
            }
            message = shotTravel >= ShotRange
                ? shotName + " 빗나갔습니다. 충격판에 닿지 않아 문은 닫혀 있습니다."
                : shotName + " 벽에 막혔습니다. 충격판에 닿지 않아 문은 닫혀 있습니다.";
        }

        private void OnPlateStruck()
        {
            plateHits++;
            plateFlash = PlateFlashSeconds;
            if (doorOpen)
            {
                message = "충격판이 다시 울렸지만 문은 이미 열려 있습니다.";
                return;
            }
            doorOpen = true;
            doorOpenings++;
            RebuildSolids();
            message = "충격판이 울렸습니다! 문이 열렸습니다. 통로로 들어가세요.";
            if (PlateRang != null) PlateRang();
        }

        private void MoveBody(float move, bool jump, float dt)
        {
            if (jump && grounded)
            {
                velocity.y = JumpVelocity;
                grounded = false;
            }

            velocity.x = move * WalkSpeed;
            float nx = center.x + velocity.x * dt;
            for (int i = 0; i < solids.Count; i++)
            {
                Rect s = solids[i];
                if (!Overlaps(Box(nx, center.y), s, ContactSkin)) continue;
                if (velocity.x > 0f) nx = s.xMin - HalfWidth;
                else if (velocity.x < 0f) nx = s.xMax + HalfWidth;
            }

            velocity.y = Mathf.Max(velocity.y - Gravity * dt, -MaxFall);
            float ny = center.y + velocity.y * dt;
            bool landed = false;
            for (int i = 0; i < solids.Count; i++)
            {
                Rect s = solids[i];
                if (!Overlaps(Box(nx, ny), s, ContactSkin)) continue;
                if (velocity.y <= 0f)
                {
                    ny = s.yMax + HalfHeight;
                    landed = true;
                }
                else
                {
                    ny = s.yMin - HalfHeight;
                }
                velocity.y = 0f;
            }

            center = new Vector2(nx, ny);
            grounded = landed;
        }

        private void CheckRunes()
        {
            Rect box = Box(center.x, center.y);
            for (int i = 0; i < RuneWords.Length; i++)
            {
                if (learned.Contains(RuneWords[i]) || !Overlaps(box, RuneZones[i])) continue;
                learned.Add(RuneWords[i]);
                message = "새 단어 습득 · " + SpellResolver.WordName(RuneWords[i]) + " · 주문에 넣을 수 있습니다.";
            }
        }

        private void RebuildSolids()
        {
            solids.Clear();
            solids.AddRange(FixedSolids);
            if (!doorOpen) solids.Add(Door);
        }

        private static Rect Box(float x, float y) =>
            new Rect(x - HalfWidth, y - HalfHeight, 2f * HalfWidth, 2f * HalfHeight);

        private static Rect Inflate(Rect r, float by) =>
            new Rect(r.xMin - by, r.yMin - by, r.width + 2f * by, r.height + 2f * by);

        // skin만큼 이하로 얕게 겹치는 것은 접촉으로 본다. 기본 0은 엄격한 겹침이며 룬 구역 판정에 그대로 쓴다.
        private static bool Overlaps(Rect a, Rect b, float skin = 0f) =>
            a.xMin < b.xMax - skin && a.xMax > b.xMin + skin && a.yMin < b.yMax - skin && a.yMax > b.yMin + skin;

        private static bool SegmentHits(Vector2 a, Vector2 b, Rect box, out float t)
        {
            float tMin = 0f;
            float tMax = 1f;
            t = 0f;
            if (!Slab(a.x, b.x, box.xMin, box.xMax, ref tMin, ref tMax)) return false;
            if (!Slab(a.y, b.y, box.yMin, box.yMax, ref tMin, ref tMax)) return false;
            t = tMin;
            return true;
        }

        private static bool Slab(float p0, float p1, float min, float max, ref float tMin, ref float tMax)
        {
            float d = p1 - p0;
            if (Mathf.Abs(d) < 1e-6f) return p0 >= min && p0 <= max;
            float t1 = (min - p0) / d;
            float t2 = (max - p0) / d;
            if (t1 > t2)
            {
                float swap = t1;
                t1 = t2;
                t2 = swap;
            }
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMin <= tMax;
        }
    }
}
