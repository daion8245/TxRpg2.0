namespace TxRpg.Data
{
    public enum BattlePhaseType
    {
        BattleIntro,
        TurnStart,
        ActionSelect,
        Execute,
        TurnEnd,
        Result
    }

    public enum EntityTeam
    {
        Player,
        Enemy
    }

    public enum ActionType
    {
        Attack,
        Skill,
        Defend,
        Item,
        Flee
    }

    public enum BattleResult
    {
        Victory,
        Defeat,
        Flee
    }

    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }

    public enum SkillCategory
    {
        Physical,
        Magical,
        Healing,
        Buff,
        Debuff
    }

    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Dark,
        Holy
    }

    public enum SkillActionType
    {
        PlayAnimation,
        MoveToTarget,
        ReturnToPosition,
        ApplyDamage,
        SpawnEffect,
        PlaySound,
        CameraShake,
        CameraZoom,
        Flash,
        Wait,
        Parallel
    }
}
