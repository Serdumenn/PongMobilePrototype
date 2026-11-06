public enum BackdropScene
{
    Plain,
    Arcade,
    Adventure,
    Classic,
    Rush,
    Together,
    Living
}

public static class BackdropScenes
{
    public static BackdropScene For(GameModeKind kind)
    {
        return kind switch
        {
            GameModeKind.Arcade => BackdropScene.Arcade,
            GameModeKind.Adventure => BackdropScene.Adventure,
            GameModeKind.Rush => BackdropScene.Rush,
            GameModeKind.RushBattle => BackdropScene.Rush,
            _ => BackdropScene.Classic
        };
    }
}
