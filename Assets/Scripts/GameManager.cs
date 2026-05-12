using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 상태(점수·게임오버) 관리 및 씬에 배치된 환경 오브젝트에 스프라이트 적용.
/// 씬 오브젝트 생성은 하지 않고, GameObject.Find 로 미리 배치된 오브젝트를 찾아 처리.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─── 게임 월드 상수 ──────────────────────────────────────────────────────
    public const float WallX          = 4.5f;
    public const float FloorY         = -9f;
    public const float GameOverLineY  = 6.5f;
    public const float SpawnerY       = 8.5f;

    // ─── 런타임 참조 ─────────────────────────────────────────────────────────
    private FruitSpawner spawner;
    private Text         scoreText;
    private GameObject   gameOverPanel;
    private Text         finalScoreLabel;
    private bool         isGameOver;
    private int          score;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Physics2D.gravity = new Vector2(0f, -18f);

        ApplyVisuals();
        spawner = FindFirstObjectByType<FruitSpawner>();
        CreateUI();
    }

    // ─── 환경 오브젝트 시각화 (씬에 미리 배치된 오브젝트에 Sprite를 입힘) ──

    void ApplyVisuals()
    {
        var spr = MakeWhiteSprite();
        Paint("Background",   spr, new Color(0.98f, 0.95f, 0.88f, 1f), -1);
        Paint("Floor",        spr, new Color(0.55f, 0.38f, 0.22f, 1f),  2);
        Paint("LeftWall",     spr, new Color(0.55f, 0.38f, 0.22f, 1f),  2);
        Paint("RightWall",    spr, new Color(0.55f, 0.38f, 0.22f, 1f),  2);
        Paint("GameOverLine", spr, new Color(0.90f, 0.15f, 0.15f, 0.85f), 3);
    }

    static void Paint(string goName, Sprite spr, Color col, int order)
    {
        var obj = GameObject.Find(goName);
        if (obj == null) return;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.color  = col;
        sr.sortingOrder = order;
    }

    static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    // ─── UI 생성 ─────────────────────────────────────────────────────────────

    void CreateUI()
    {
        var cv = new GameObject("Canvas");
        var canvas = cv.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        cv.AddComponent<GraphicRaycaster>();

        // EventSystem (버튼 클릭에 필요)
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 점수 텍스트
        scoreText = MakeText(cv.transform, "ScoreText", "Score: 0", 60,
                             new Color(0.2f, 0.1f, 0f), FontStyle.Bold,
                             new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                             new Vector2(30, -30), new Vector2(500, 90));

        // 게임오버 패널
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(cv.transform, false);
        gameOverPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        var pRt = gameOverPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
        pRt.sizeDelta = Vector2.zero; pRt.anchoredPosition = Vector2.zero;

        MakeText(gameOverPanel.transform, "Title", "GAME OVER", 100,
                 Color.white, FontStyle.Bold,
                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                 new Vector2(0, 80), new Vector2(900, 150));

        finalScoreLabel = MakeText(gameOverPanel.transform, "FinalScore", "Score: 0", 60,
                 new Color(1f, 0.9f, 0.4f), FontStyle.Bold,
                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                 new Vector2(0, -20), new Vector2(700, 90));

        MakeButton(gameOverPanel.transform, "다시 시작", new Vector2(0, -140),
                   new Vector2(420, 100), RestartGame);

        gameOverPanel.SetActive(false);
    }

    // ─── UI 헬퍼 ─────────────────────────────────────────────────────────────

    static Text MakeText(Transform parent, string name, string txt, int size,
                         Color col, FontStyle style,
                         Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                         Vector2 pos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<Text>();
        t.text      = txt;
        t.fontSize  = size;
        t.color     = col;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
        return t;
    }

    static void MakeButton(Transform parent, string label, Vector2 pos,
                           Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject("RestartButton");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = new Color(0.2f, 0.65f, 0.25f);
        obj.AddComponent<Button>().onClick.AddListener(action);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var tObj = new GameObject("Text");
        tObj.transform.SetParent(obj.transform, false);
        var t = tObj.AddComponent<Text>();
        t.text      = label; t.fontSize = 52; t.color = Color.white;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        var tRt = t.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero; tRt.anchoredPosition = Vector2.zero;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void AddScore(int pts)
    {
        score += pts;
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    public void MergeFruits(int newIndex, Vector2 position, int pts)
    {
        if (isGameOver) return;
        AddScore(pts);
        spawner?.SpawnMergedFruit(newIndex, position);
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (spawner != null) spawner.enabled = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreLabel != null)
                finalScoreLabel.text = $"Score: {score}";
        }
    }

    void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public bool IsGameOver => isGameOver;
}
