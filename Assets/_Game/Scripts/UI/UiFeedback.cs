public static class UiFeedback
{
    public static void Tap()
    {
        HapticManager.Soft();
        AudioManager.PlayOne(Sfx.UiTap);
    }

    public static void Back()
    {
        HapticManager.Soft();
        AudioManager.PlayOne(Sfx.UiBack);
    }
}
