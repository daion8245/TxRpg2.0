namespace TxRpg.Animation
{
    /// <summary>
    /// 배틀 엔티티의 애니메이션 상태 전이를 관리합니다.
    /// idle ↔ attack ↔ hit ↔ die
    /// </summary>
    public enum AnimState
    {
        Idle,
        Attack,
        Hit,
        Die,
        Move
    }

    public class AnimStateMachine
    {
        private readonly IAnimationAdapter _adapter;
        private AnimState _currentState = AnimState.Idle;

        public AnimState CurrentState => _currentState;

        public AnimStateMachine(IAnimationAdapter adapter)
        {
            _adapter = adapter;
        }

        public void TransitionTo(AnimState newState)
        {
            if (_currentState == AnimState.Die && newState != AnimState.Idle)
                return; // 사망 상태에서는 Idle 복귀만 허용

            _currentState = newState;
            _adapter.Play(GetAnimationName(newState));
        }

        public static string GetAnimationName(AnimState state)
        {
            return state switch
            {
                AnimState.Idle => "idle",
                AnimState.Attack => "attack",
                AnimState.Hit => "hit",
                AnimState.Die => "die",
                AnimState.Move => "move",
                _ => "idle"
            };
        }
    }
}
