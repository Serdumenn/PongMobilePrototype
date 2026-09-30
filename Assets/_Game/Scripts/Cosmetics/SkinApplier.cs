using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkinApplier : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private CosmeticsService Cosmetics;

    [Header("World")]
    [SerializeField] private BallFace Ball;
    [SerializeField] private SpriteRenderer Paddle;
    [SerializeField] private Camera WorldCamera;
    [SerializeField] private SpriteRenderer BlobPrimary;
    [SerializeField] private SpriteRenderer BlobSecondary;

    [Header("UI")]
    [SerializeField] private UIDocument Document;

    private string appliedThemeClass;

    private void Start()
    {
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (WorldCamera == null) WorldCamera = Camera.main;

        Cosmetics.Changed += Apply;
        Apply();
    }

    private void OnDestroy()
    {
        if (Cosmetics != null) Cosmetics.Changed -= Apply;
    }

    private void Apply()
    {
        var ball = Cosmetics.Equipped(CosmeticCategory.Ball);
        if (ball != null && Ball != null) Ball.SetExpressions(ball.Idle, ball.Happy, ball.Sad);

        var paddle = Cosmetics.Equipped(CosmeticCategory.Paddle);
        if (paddle != null && Paddle != null) Paddle.sprite = paddle.PaddleSprite;

        var theme = Cosmetics.Equipped(CosmeticCategory.Theme);
        if (theme == null) return;

        if (WorldCamera != null) WorldCamera.backgroundColor = theme.BackgroundColor;
        if (BlobPrimary != null) BlobPrimary.color = theme.BlobPrimary;
        if (BlobSecondary != null) BlobSecondary.color = theme.BlobSecondary;

        var root = Document != null ? Document.rootVisualElement?.Q("root") : null;
        if (root == null) return;

        if (!string.IsNullOrEmpty(appliedThemeClass)) root.RemoveFromClassList(appliedThemeClass);
        appliedThemeClass = theme.UiClass;
        if (!string.IsNullOrEmpty(appliedThemeClass)) root.AddToClassList(appliedThemeClass);
    }
}
