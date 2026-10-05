using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ScoreSpark : VisualElement
{
    private const float LineWidth = 9f;
    private const float Dash = 26f;
    private const float Gap = 20f;

    private static readonly Color MineColor = new Color32(0xE0, 0x4E, 0x3A, 0xFF);
    private static readonly Color GhostColor = new Color32(0xB8, 0xA9, 0x9A, 0xFF);

    private readonly List<Vector2> points = new List<Vector2>();
    private GhostRun mine;
    private GhostRun ghost;
    private int mineScore;

    public ScoreSpark()
    {
        pickingMode = PickingMode.Ignore;
        style.flexGrow = 1f;
        generateVisualContent += Draw;
    }

    public void SetRuns(GhostRun myRun, int myScore, GhostRun ghostRun)
    {
        mine = myRun;
        mineScore = myScore;
        ghost = ghostRun;
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext context)
    {
        Rect rect = contentRect;
        if (rect.width < 4f || rect.height < 4f) return;

        float duration = Mathf.Max(1f, Mathf.Max(mine != null ? mine.Duration : 0f, ghost != null ? ghost.Duration : 0f));
        int top = Mathf.Max(1, Mathf.Max(mine != null ? mine.Score : mineScore, ghost != null ? ghost.Score : 0));
        rect = new Rect(rect.x + LineWidth, rect.y + LineWidth, rect.width - LineWidth * 2f, rect.height - LineWidth * 2f);

        var painter = context.painter2D;
        painter.lineWidth = LineWidth;
        painter.lineCap = LineCap.Round;
        painter.lineJoin = LineJoin.Round;

        if (ghost != null)
        {
            Collect(ghost, ghost.Score, duration, top, rect);
            painter.strokeColor = GhostColor;
            StrokeDashed(painter);
        }

        Collect(mine, mineScore, duration, top, rect);
        painter.strokeColor = MineColor;
        painter.BeginPath();
        painter.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++) painter.LineTo(points[i]);
        painter.Stroke();
    }

    private void Collect(GhostRun run, int final, float duration, int top, Rect rect)
    {
        points.Clear();
        points.Add(Point(0f, 0, duration, top, rect));
        if (run != null)
        {
            foreach (var key in run.Scores) points.Add(Point(key.Time, key.Score, duration, top, rect));
            points.Add(Point(run.Duration, run.Score, duration, top, rect));
            return;
        }
        points.Add(Point(duration, final, duration, top, rect));
    }

    private static Vector2 Point(float time, int score, float duration, int top, Rect rect)
    {
        return new Vector2(rect.x + rect.width * Mathf.Clamp01(time / duration), rect.yMax - rect.height * Mathf.Clamp01((float)score / top));
    }

    private void StrokeDashed(Painter2D painter)
    {
        float carry = 0f;
        bool drawing = true;
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 a = points[i - 1];
            Vector2 b = points[i];
            float length = Vector2.Distance(a, b);
            float at = 0f;
            while (at < length)
            {
                float span = (drawing ? Dash : Gap) - carry;
                float end = Mathf.Min(length, at + span);
                if (drawing)
                {
                    painter.BeginPath();
                    painter.MoveTo(Vector2.Lerp(a, b, at / length));
                    painter.LineTo(Vector2.Lerp(a, b, end / length));
                    painter.Stroke();
                }

                carry += end - at;
                at = end;
                if (carry >= (drawing ? Dash : Gap) - 0.001f)
                {
                    carry = 0f;
                    drawing = !drawing;
                }
            }
        }
    }
}
