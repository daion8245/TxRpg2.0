using System;
using System.Linq;
using Asmegica;
using UnityEditor;
using UnityEngine;

// 탐험 방 재현 검사. 편집 모드에 임시 GrayboxScene을 만들어 StepExploration·ReturnToRoom 같은
// 실행 진입점만 쓴다. 메인 카메라와 씬 오브젝트는 건드리지 않고, 검사가 끝나면 임시 오브젝트를 지운다.
// 실행: 메뉴 Asmegica/탐험 방 검사, 또는 -executeMethod AsmegicaExplorationChecks.Run
public static class AsmegicaExplorationChecks
{
    private const float Dt = 1f / 60f;
    private const float AimFromSpawnToPlate = 31f;   // 입구에서 충격판에 닿는 조준 구간(약 27~36도) 가운데
    private const float AimIntoCeiling = 60f;        // 천장에 막히는 빗나감

    private static readonly RoomInput None = new RoomInput();
    private static readonly RoomInput HoldRight = new RoomInput { Right = true };
    private static readonly RoomInput HoldLeft = new RoomInput { Left = true };
    private static readonly RoomInput JumpRight = new RoomInput { Right = true, JumpPressed = true };
    private static readonly RoomInput JumpLeft = new RoomInput { Left = true, JumpPressed = true };
    private static readonly RoomInput AimUp = new RoomInput { AimUp = true };
    private static readonly RoomInput AimDown = new RoomInput { AimDown = true };

    private sealed class Probe
    {
        public GrayboxScene Host;
        public int PlateRings;
        public GrayboxRoom Room => Host.Room;
    }

    [MenuItem("Asmegica/탐험 방 검사")]
    public static void Run()
    {
        var go = new GameObject("AsmegicaExplorationProbe") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var probe = new Probe { Host = go.AddComponent<GrayboxScene>() };
            probe.Room.PlateRang += () => probe.PlateRings++;

            CheckStartsWithShield(probe);
            LearnLaunch(probe);
            LearnFire(probe);
            LearnStrong(probe);
            CheckWrongSpell(probe);
            CheckMiss(probe);
            CheckHit(probe);
            CheckNoRepeatedOpening(probe);
            CheckTransitionAndBattle(probe);
            CheckRightToLeftClimb(probe);
            CheckRestart(probe);
            Debug.Log("아스메지카 탐험 방 검사 통과");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // 시작: 방패만 습득, 문 닫힘, 제자리 방패와 미습득 단어는 문을 열지 못한다.
    private static void CheckStartsWithShield(Probe p)
    {
        p.Host.ResetRoom();
        Require(p.Host.IsExploring && !p.Room.DoorOpen && p.Room.DoorOpenings == 0, "시작 · 탐험 방, 문 닫힘");
        Require(p.Room.Learned.SequenceEqual(new[] { MagicWord.Shield }), "시작 · 방패만 습득");
        Require(!p.Room.AddWord(MagicWord.Fire, out string unlearned) && unlearned.Contains("습득"), "미습득 화염은 주문에 들어가지 않음");
        Require(!p.Room.AddWord(MagicWord.Launch, out _), "미습득 발사는 주문에 들어가지 않음");
        p.Room.ClearWords();
        Require(p.Room.AddWord(MagicWord.Shield, out _) && p.Room.TryCast(out _), "방패 단독 시전");
        Require(!p.Room.ShotInFlight && p.Room.Message.Contains("제자리"), "제자리 방패는 던지지 않음");
        Idle(p, 90);
        Require(!p.Room.DoorOpen && p.PlateRings == 0 && p.Room.PlateHits == 0, "제자리 방패 뒤에도 문 닫힘");
    }

    // 발사 발판: 4.4 지점에서 점프해 발판 구역에 닿으면 발사를 습득한다.
    private static void LearnLaunch(Probe p)
    {
        p.Room.PlaceAt(GrayboxRoom.Spawn);
        Idle(p, 3);
        RunUntil(p, HoldRight, () => p.Room.PlayerCenter.x >= 4.4f, 600, "발사 발판 앞까지 걷기");
        Require(p.Room.Grounded, "점프 전 바닥에 서 있음");
        Frame(p, JumpRight);
        RunUntil(p, HoldRight, () => p.Room.IsLearned(MagicWord.Launch), 240, "발사 발판에서 발사 습득");
        Require(p.Room.Message.Contains("습득"), "발사 습득 안내");
        Idle(p, 3);
    }

    // 화염 발판: 발사 발판 위에서 오른쪽 점프로 화염 발판 구역에 닿는다.
    private static void LearnFire(Probe p)
    {
        p.Room.PlaceAt(new Vector2(7.5f, 2f + GrayboxRoom.HalfHeight));
        Idle(p, 3);
        Require(p.Room.Grounded, "발사 발판 위에 서 있음");
        Frame(p, JumpRight);
        RunUntil(p, HoldRight, () => p.Room.IsLearned(MagicWord.Fire), 240, "화염 발판에서 화염 습득");
        Idle(p, 3);
    }

    // 강하게 발판: 입구 쪽에서 왼쪽 점프로 왼쪽 발판 구역에 닿는다.
    private static void LearnStrong(Probe p)
    {
        p.Room.PlaceAt(new Vector2(4f, GrayboxRoom.HalfHeight));
        Idle(p, 3);
        Require(p.Room.Grounded, "바닥에 서 있음");
        Frame(p, JumpLeft);
        RunUntil(p, HoldLeft, () => p.Room.IsLearned(MagicWord.Strong), 240, "강하게 발판에서 강하게 습득");
        Idle(p, 3);
    }

    // 잘못된 주문: 메인 없는 단어, 중복 단어, 미습득 단어는 거절하고 입력과 자원을 그대로 둔다.
    private static void CheckWrongSpell(Probe p)
    {
        p.Room.ClearWords();
        Require(p.Room.AddWord(MagicWord.Launch, out _), "습득한 발사 추가");
        Require(!p.Room.TryCast(out string noMain) && noMain.Contains("메인") && p.Room.Words.Count == 1,
            "발사만 있는 주문 거절 · 입력 보존");
        Require(p.Room.AddWord(MagicWord.Shield, out _) && p.Room.AddWord(MagicWord.Shield, out _), "중복 단어 준비");
        Require(!p.Room.TryCast(out string duplicate) && duplicate.Contains("한 번") && p.Room.Words.Count == 3,
            "중복 단어 주문 거절 · 입력 보존");
        p.Room.ClearWords();
        Require(!p.Room.ShotInFlight && !p.Room.DoorOpen && p.PlateRings == 0, "거절된 주문은 던지지 않음");
    }

    // 빗나감: 천장 쪽으로 던지면 충격판에 닿지 않고 문은 닫혀 있다.
    private static void CheckMiss(Probe p)
    {
        p.Room.PlaceAt(GrayboxRoom.Spawn);
        Idle(p, 3);
        p.Room.ClearWords();
        AimAt(p, AimIntoCeiling);
        CastWords(p, MagicWord.Shield, MagicWord.Launch);
        WaitForShot(p);
        Require(!p.Room.DoorOpen && p.Room.PlateHits == 0 && p.PlateRings == 0, "빗나간 방패는 문을 열지 않음");
        Require(p.Room.Message.Contains("막혔") || p.Room.Message.Contains("빗나갔"), "빗나감 안내");
    }

    // 명중: 입구에서 같은 투척 방패가 충격판에 실제로 닿으면 문이 열리고 소리가 한 번 난다.
    private static void CheckHit(Probe p)
    {
        p.Room.PlaceAt(GrayboxRoom.Spawn);
        Idle(p, 3);
        p.Room.ClearWords();
        AimAt(p, AimFromSpawnToPlate);
        CastWords(p, MagicWord.Shield, MagicWord.Launch);
        WaitForShot(p);
        Require(p.Room.DoorOpen && p.Room.DoorOpenings == 1 && p.Room.PlateHits == 1, "충격판에 닿으면 문 열림");
        Require(p.PlateRings == 1, "처음 열릴 때 소리 한 번");
    }

    // 중복 열림 방지: 열린 뒤 같은 충돌이 다시 와도 문을 다시 열지 않고 소리도 나지 않는다.
    private static void CheckNoRepeatedOpening(Probe p)
    {
        p.Room.PlaceAt(GrayboxRoom.Spawn);
        Idle(p, 3);
        p.Room.ClearWords();
        AimAt(p, AimFromSpawnToPlate);
        CastWords(p, MagicWord.Shield, MagicWord.Launch);
        WaitForShot(p);
        Require(p.Room.DoorOpenings == 1 && p.PlateRings == 1 && p.Room.PlateHits == 2, "열린 뒤 중복 충돌은 다시 열지 않음");
        Require(p.Room.Message.Contains("이미 열려"), "중복 충돌 안내");
    }

    // 전환: 문을 지나면 학습 단어 덱 편성이 열리고, 전투를 거쳐 방으로 돌아온 뒤 재입장해도 문은 한 번만 열린다.
    private static void CheckTransitionAndBattle(Probe p)
    {
        p.Room.PlaceAt(GrayboxRoom.DoorApproach);
        Idle(p, 3);
        RunUntil(p, HoldRight, () => p.Host.IsDeckOpen, 600, "통로를 지나 덱 편성 열기");
        Require(p.Host.DeckOpenedFromRoom, "방에서 연 덱 편성 표시");
        Require(p.Host.DeckWords.Count >= 3 && p.Host.DeckWords.All(word => p.Room.IsLearned(word)), "덱은 습득 단어만");
        Require(p.Host.AdjustDeck(MagicWord.Shield, 1), "습득 단어 방패 장수 증가");
        p.Host.StartDeckBattle();
        Require(p.Host.IsBattleOpen, "덱으로 전투 시작");
        p.Host.LeaveBattle();
        Require(p.Host.IsDeckOpen, "전투 나가기 → 덱 편성");
        p.Host.ReturnToRoom();
        Require(p.Host.IsExploring && p.Room.DoorOpen, "방으로 돌아와도 문은 열린 상태");
        Require(p.Room.PlayerCenter.x < 15f, "문 앞에 다시 세움");
        RunUntil(p, HoldRight, () => p.Host.IsDeckOpen, 600, "재입장으로 덱 편성 다시 열기");
        Require(p.Room.DoorOpenings == 1 && p.PlateRings == 1, "재입장은 문 열림을 반복하지 않음");
        p.Host.ReturnToRoom();
    }

    // 우측 복귀: 문 앞 바닥에서 왼쪽 키와 점프 키만으로 화염 발판 오른쪽 디딤턱에 올라 기둥을 넘어 왼쪽 구역까지 간다.
    // 위치를 옮기는 것은 시작 위치뿐이고, 나머지는 실제 키 입력과 같은 RoomInput을 StepExploration에 넣는다.
    private static void CheckRightToLeftClimb(Probe p)
    {
        const float PillarTopCenter = 4.6f;   // 화염 발판(높이 4) 위에 선 플레이어 중심 높이
        p.Room.PlaceAt(GrayboxRoom.DoorApproach);
        Idle(p, 3);
        Require(p.Room.Grounded, "문 앞 바닥에 서 있음");
        float highest = p.Room.PlayerCenter.y;
        for (int i = 0; i < 600 && p.Room.PlayerCenter.x > 10f; i++)
        {
            Frame(p, p.Room.Grounded ? JumpLeft : HoldLeft);
            highest = Mathf.Max(highest, p.Room.PlayerCenter.y);
        }
        Require(p.Room.PlayerCenter.x <= 10f, "문 앞 바닥에서 화염 발판을 넘어 왼쪽으로 · 제한 시간 안에 끝나지 않음" + TerminalState(p, highest));
        Require(highest >= PillarTopCenter, "화염 발판 꼭대기 위를 지나 넘음" + TerminalState(p, highest));
    }

    // 초기화: 문 닫힘, 방패만 습득, 입구에서 시작. 초기화 뒤에도 같은 방식으로 다시 열린다.
    private static void CheckRestart(Probe p)
    {
        p.Host.ResetRoom();
        Require(p.Host.IsExploring && !p.Room.DoorOpen && p.Room.DoorOpenings == 0, "초기화 · 문 닫힘");
        Require(p.Room.Learned.SequenceEqual(new[] { MagicWord.Shield }), "초기화 · 방패만 습득");
        Require(p.Room.PlayerCenter == GrayboxRoom.Spawn && !p.Host.DeckOpenedFromRoom, "초기화 · 입구에서 시작");
        LearnLaunch(p);
        p.Room.ClearWords();
        AimAt(p, AimFromSpawnToPlate);
        CastWords(p, MagicWord.Shield, MagicWord.Launch);
        WaitForShot(p);
        Require(p.Room.DoorOpen && p.Room.DoorOpenings == 1, "초기화 뒤 다시 열림");
    }

    private static void Frame(Probe p, RoomInput input) => p.Host.StepExploration(input, Dt);

    private static void Idle(Probe p, int frames)
    {
        for (int i = 0; i < frames; i++) Frame(p, None);
    }

    private static void RunUntil(Probe p, RoomInput input, Func<bool> done, int maxFrames, string step)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (done()) return;
            Frame(p, input);
        }
        Require(done(), step + " · 제한 시간 안에 끝나지 않음");
    }

    private static void AimAt(Probe p, float degrees)
    {
        for (int i = 0; i < 600; i++)
        {
            float gap = degrees - p.Room.AimDegrees;
            if (Mathf.Abs(gap) <= 0.75f) return;
            Frame(p, gap > 0f ? AimUp : AimDown);
        }
        Require(false, "조준 각도 " + degrees + "도에 맞추지 못함");
    }

    private static void CastWords(Probe p, params MagicWord[] words)
    {
        foreach (MagicWord word in words)
            Require(p.Room.AddWord(word, out _), "주문 단어 추가 · " + SpellResolver.WordName(word));
        Require(p.Room.TryCast(out _), "주문 시전");
    }

    private static void WaitForShot(Probe p)
    {
        RunUntil(p, None, () => !p.Room.ShotInFlight, 240, "던진 방패가 도착하거나 빗나감");
    }

    // 실패 메시지에 끝 상태를 붙인다: 끝 위치(x, y), 지나간 최고 y, 바닥 여부.
    private static string TerminalState(Probe p, float highest)
    {
        Vector2 c = p.Room.PlayerCenter;
        return " · 끝 x=" + c.x.ToString("F3") + " y=" + c.y.ToString("F3") + " 최고y=" + highest.ToString("F3") + " 바닥=" + p.Room.Grounded;
    }

    private static void Require(bool success, string check)
    {
        if (!success) throw new InvalidOperationException("탐험 방 검사 실패 · " + check);
    }
}
