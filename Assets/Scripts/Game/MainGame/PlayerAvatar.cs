// PlayerAvatarEyesDual.cs  (добавлены поля/метод для мигания)
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class PlayerAvatarEyesDual : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text leftEyeText;
    [SerializeField] private TMP_Text rightEyeText;

    [Header("Символы")]
    [SerializeField] private char fillChar = 'Щ';
    [SerializeField] private char emptyChar = ' ';
    [SerializeField] private char eyelidChar = '—';

    [Header("Опции")]
    [SerializeField] private bool drawEyelids = true;
    [SerializeField] private bool radialReveal = true;

    // ── Новое: параметры мигания ─────────────────
    [Range(0f,1f)]
    [SerializeField] private float blink; // 0..1 (инспектор/анимация)
    private int firstFillRow, lastFillRow; // диапазон “глаза” по вертикали
    // ─────────────────────────────────────────────

    private static readonly string[] EYE_SHAPE =
    {
        "   -----------   ",
        "   ---xxxxx---   ",
        "   --x     x--   ",
        "   -x       x-   ",
        "   -x   p   x-   ",
        "   -x       x-   ",
        "   --x     x--   ",
        "   ---xxxxx---   ",
        "   -----------   "
    };

    private int W, H;
    private char[,] grid;
    private bool[,] maskFill;
    private bool[,] maskEyelid;
    private List<Vector2Int> revealOrder;
    private int revealedCount = 0;

    private void Reset()
    {
        var tmps = GetComponentsInChildren<TMP_Text>();
        if (tmps.Length >= 2)
        {
            leftEyeText = tmps[0];
            rightEyeText = tmps[1];
        }
    }

    private void Awake()
    {
        EnsureTMPSettings(leftEyeText);
        EnsureTMPSettings(rightEyeText);

        BuildFromShape();
        BuildRevealOrder();
        ApplyRevealCount(0);
        UpdateTexts();
        
        RevealAll();
    }

    private static void EnsureTMPSettings(TMP_Text t)
    {
        if (!t) return;
        t.enableWordWrapping = false;
        t.richText = false;
        t.enableKerning = false;
        t.characterSpacing = 0f;
        t.lineSpacing = 0f;
        t.alignment = TextAlignmentOptions.TopLeft;
    }

    private void BuildFromShape()
    {
        H = EYE_SHAPE.Length;
        W = EYE_SHAPE[0].Length;

        grid = new char[H, W];
        maskFill = new bool[H, W];
        maskEyelid = new bool[H, W];

        firstFillRow = int.MaxValue;
        lastFillRow  = int.MinValue;

        for (int y = 0; y < H; y++)
        {
            string row = EYE_SHAPE[y];
            for (int x = 0; x < W; x++)
            {
                char c = row[x];
                grid[y, x] = emptyChar;

                switch (c)
                {
                    case 'x':
                    case 'p':
                        maskFill[y, x] = true;
                        firstFillRow = Mathf.Min(firstFillRow, y);
                        lastFillRow  = Mathf.Max(lastFillRow, y);
                        break;
                    case '-':
                        maskEyelid[y, x] = true;
                        break;
                    default:
                        break;
                }
            }
        }

        if (firstFillRow == int.MaxValue) { firstFillRow = 0; lastFillRow = H-1; }
    }

    private void BuildRevealOrder()
    {
        revealOrder = new List<Vector2Int>(W * H);

        var pupils = new List<Vector2>();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (EYE_SHAPE[y][x] == 'p')
                    pupils.Add(new Vector2(x, y));
        if (pupils.Count == 0) pupils.Add(new Vector2(W * 0.5f, H * 0.5f));

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (maskFill[y, x]) revealOrder.Add(new Vector2Int(x, y));

        if (radialReveal)
        {
            revealOrder = revealOrder
                .OrderBy(p =>
                {
                    float best = float.MaxValue;
                    foreach (var c in pupils)
                    {
                        float d = (new Vector2(p.x, p.y) - c).sqrMagnitude;
                        if (d < best) best = d;
                    }
                    return best;
                })
                .ToList();
        }
        else
        {
            revealOrder = revealOrder.OrderBy(p => p.y).ThenBy(p => p.x).ToList();
        }
    }

    public void SetProgress01(float t)
    {
        t = Mathf.Clamp01(t);
        int target = Mathf.RoundToInt(revealOrder.Count * t);
        ApplyRevealCount(target);
        UpdateTexts();
    }

    public void SetProgressByLevels(int completedLevels, int totalLevels)
    {
        if (totalLevels <= 0) return;
        SetProgress01(completedLevels / (float)totalLevels);
    }

    public void RevealNext(int count = 1)
    {
        ApplyRevealCount(revealedCount + Mathf.Max(1, count));
        UpdateTexts();
    }

    public void RevealAll()
    {
        ApplyRevealCount(revealOrder.Count);
        UpdateTexts();
    }

    private void ApplyRevealCount(int newCount)
    {
        newCount = Mathf.Clamp(newCount, 0, revealOrder.Count);

        if (newCount > revealedCount)
        {
            for (int i = revealedCount; i < newCount; i++)
            {
                var p = revealOrder[i];
                grid[p.y, p.x] = fillChar;
            }
        }
        else if (newCount < revealedCount)
        {
            for (int i = newCount; i < revealedCount; i++)
            {
                var p = revealOrder[i];
                grid[p.y, p.x] = emptyChar;
            }
        }

        revealedCount = newCount;
    }

    // ── Новое: мигание 0..1 (накрываем центральные строки веками) ──
    public void SetBlink01(float t)
    {
        blink = Mathf.Clamp01(t);
        UpdateTexts();
    }

    private string BuildEyeString()
    {
        var sb = new StringBuilder(H * (W + 1));

        // сколько строк “сверху и снизу” накрыть веками при текущем t
        int innerHeight = Mathf.Max(0, lastFillRow - firstFillRow + 1);
        int coverEachSide = Mathf.RoundToInt(blink * innerHeight * 0.5f);

        int topCoverEnd    = firstFillRow + coverEachSide - 1;
        int bottomCoverBeg = lastFillRow  - coverEachSide + 1;

        for (int y = 0; y < H; y++)
        {
            bool coverTop    = (coverEachSide > 0) && (y >= firstFillRow) && (y <= topCoverEnd);
            bool coverBottom = (coverEachSide > 0) && (y >= bottomCoverBeg) && (y <= lastFillRow);

            for (int x = 0; x < W; x++)
            {
                char ch = grid[y, x];

                // Базовые веки (статично)
                if (drawEyelids && maskEyelid[y, x] && ch == emptyChar)
                    ch = eyelidChar;

                // Динамичное “накрытие” при мигании
                if (coverTop || coverBottom)
                {
                    // Накрываем только вертикальный диапазон глаза
                    if (y >= firstFillRow && y <= lastFillRow)
                        ch = eyelidChar;
                }

                sb.Append(ch);
            }
            if (y < H - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private void UpdateTexts()
    {
        string eye = BuildEyeString();
        if (leftEyeText)  leftEyeText.text  = eye;
        if (rightEyeText) rightEyeText.text = eye;
    }
}
