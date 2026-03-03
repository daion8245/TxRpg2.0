using UnityEngine;

public enum Characters
{
    Knight,
}

public enum AnimationStates
{
    Idle,
    Walk,
    Jump,
}

public static class AnimationManager
{
    public static string GetAnimation(Characters character, AnimationStates state)
    {
        return $"{character.ToString()}_{state.ToString()}";
    }
}