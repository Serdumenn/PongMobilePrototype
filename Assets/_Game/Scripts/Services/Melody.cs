using UnityEngine;

public static class Melody
{
    private static readonly int[] Phrase = { 0, 4, 7, 4, 2, 7, 9, 7, 4, 9, 12, 9, 7, 12, 9, 12 };

    public static int Length => Phrase.Length;

    public static float Lift(int rally, float step, float max)
    {
        return Mathf.Min(Mathf.Max(0, rally) / Phrase.Length * Mathf.Max(0f, step), Mathf.Max(0f, max));
    }

    public static float Semitones(int rally, float step, float max)
    {
        rally = Mathf.Max(0, rally);
        return Phrase[rally % Phrase.Length] + Lift(rally, step, max);
    }

    public static float Pitch(float semitones)
    {
        return Mathf.Pow(2f, semitones / 12f);
    }
}
