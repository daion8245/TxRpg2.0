using System.Collections.Generic;
using TxRpg.Data;

namespace TxRpg.Core
{
    /// <summary>
    /// 퍼즐(탐험) 씬과 전투 씬 사이에서 습득한 마법 단어와 편성 덱을 공유하는 경량 세션 상태.
    /// </summary>
    public static class GameSessionState
    {
        public const string PuzzleSceneName = "PuzzleRoom";
        public const string BattleSceneName = "BattleTest";

        private static readonly HashSet<MagicWordType> _learnedWords = new()
        {
            MagicWordType.Shield
        };

        private static readonly List<MagicWordType> _customDeck = new();

        public static bool PuzzleDoorOpened { get; set; }
        public static bool OpenDeckEditorOnBattleEnter { get; set; }
        public static int PreferredEnemyCount { get; set; } = 1;

        public static IReadOnlyCollection<MagicWordType> LearnedWords => _learnedWords;
        public static IReadOnlyList<MagicWordType> CustomDeck => _customDeck;

        public static bool IsLearned(MagicWordType word) => _learnedWords.Contains(word);

        public static bool LearnWord(MagicWordType word)
        {
            return _learnedWords.Add(word);
        }

        public static void LearnAllWords()
        {
            _learnedWords.Add(MagicWordType.Fire);
            _learnedWords.Add(MagicWordType.Shield);
            _learnedWords.Add(MagicWordType.Launch);
            _learnedWords.Add(MagicWordType.Strong);
        }

        public static void ResetPuzzleProgress()
        {
            _learnedWords.Clear();
            _learnedWords.Add(MagicWordType.Shield);
            PuzzleDoorOpened = false;
        }

        public static void SetCustomDeck(IEnumerable<MagicWordType> words)
        {
            _customDeck.Clear();
            if (words != null)
                _customDeck.AddRange(words);
        }
    }
}
