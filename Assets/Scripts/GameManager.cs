using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const float WallX        = 4.5f;
    public const float FloorY       = -9f;
    public const float GameOverLineY = 6.5f;
    public const float SpawnerY     = 8.5f;
    public const float WallThickness = 0.5f;

    private FruitSpawner spawner;
    private bool isGameOver;
    private int score;

    private Text scoreText;
    private GameObject gameOverPanel;
    private Sprite boxSprite;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Physics2D.gravity = new Vector2(0f, -18f);
        boxSprite = MakeBoxSprite();

        CreateBackground();
        CreateWalls();
        CreateGameOverZone();
        CreateSpawner();
        CreateUI();
    }

    // ─── World setup ───────────────────────────────────────────────────────

    static Sprite MakeBoxSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void CreateBackground()
    {
        float h = GameOverLineY - FloorY;
        float cy = FloorY + h * 0.5f;
        MakeQuad("Background", new Vector2(0, cy), new Vector2(WallX * 2f, h),
                 new Color(0.98f, 0.95f, 0.88f), -1, false);
    }

    void CreateWalls()
    {
        float wallH = SpawnerY - FloorY + WallThickness;
        float midY = (FloorY + SpawnerY) * 0.5f;

        // Floor
        MakeQuad("Floor",
            new Vector2(0, FloorY - WallThickness * 0.5f),
            new Vector2(WallX * 2f + WallThickness * 2f, WallThickness),
            new Color(0.55f, 0.38f, 0.22f), 2, true);

        // Left wall
        MakeQuad("LeftWall",
            new Vector2(-WallX - WallThickness * 0.5f, midY),
            new Vector2(WallThickness, wallH),
            new Color(0.55f, 0.38f, 0.22f), 2, true);

        // Right wall
        MakeQuad("RightWall",
            new Vector2(WallX + WallThickness * 0.5f, midY),
            new Vector2(WallThickness, wallH),
            new Color(0.55f, 0.38f, 0.22f), 2, true);
    }

    void CreateGameOverZone()
    {
        // Invisible trigger zone above game-over line
        float zoneH = SpawnerY - GameOverLineY;
        float zoneY = GameOverLineY + zoneH * 0.5f;

        var zone = new GameObject("GameOverZone");
        zone.transform.position = new Vector3(0, zoneY, 0);
        zone.transform.localScale = new Vector3(WallX * 2f, zoneH, 1f);
        var col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;
        zone.AddComponent<GameOverChecker>();

        // Visual red line at the boundary
        MakeQuad("GameOverLine",
            new Vector2(0, GameOverLineY),
            new Vector2(WallX * 2f, 0.12f),
            new Color(0.9f, 0.15f, 0.15f, 0.85f), 3, false);
    }

    void CreateSpawner()
    {
        var obj = new GameObject("FruitSpawner");
        obj.transform.position = new Vector3(0, SpawnerY, 0);
        spawner = obj.AddComponent<FruitSpawner>();
    }

    // ─── UI ────────────────────────────────────────────────────────────────

    void CreateUI()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Score text
        var scoreObj = MakeUIText(canvasObj.transform, "ScoreText", "Score: 0", 60,
                                  new Color(0.2f, 0.1f, 0f), FontStyle.Bold);
        scoreText = scoreObj.GetComponent<Text>();
        var sRt = scoreObj.GetComponent<RectTransform>();
        sRt.anchorMin = sRt.anchorMax = new Vector2(0, 1);
        sRt.pivot = new Vector2(0, 1);
        sRt.anchoredPosition = new Vector2(30, -30);
        sRt.sizeDelta = new Vector2(500, 90);

        // Game-over overlay
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObj.transform, false);
        var panelImg = gameOverPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.75f);
        var pRt = gameOverPanel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
        pRt.sizeDelta = Vector2.zero; pRt.anchoredPosition = Vector2.zero;

        AddCenteredLabel(gameOverPanel.transform, "GAME OVER", 100, Color.white,
                         new Vector2(0, 80), new Vector2(900, 160));
        AddScoreLabel(gameOverPanel.transform);
        AddButton(gameOverPanel.transform, "다시 시작", new Vector2(0, -120),
                  new Vector2(420, 100), RestartGame);

        gameOverPanel.SetActive(false);
    }

    Text MakeUIText(Transform parent, string name, string txt, int size, Color col, FontStyle style)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<Text>();
        t.text = txt;
        t.fontSize = size;
        t.color = col;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    void AddCenteredLabel(Transform parent, string txt, int size, Color col,
                          Vector2 offset, Vector2 sizeDelta)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<Text>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = sizeDelta;
    }

    private Text finalScoreLabel;

    void AddScoreLabel(Transform parent)
    {
        var obj = new GameObject("FinalScore");
        obj.transform.SetParent(parent, false);
        finalScoreLabel = obj.AddComponent<Text>();
        finalScoreLabel.text = "Score: 0";
        finalScoreLabel.fontSize = 60;
        finalScoreLabel.color = new Color(1f, 0.9f, 0.4f);
        finalScoreLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        finalScoreLabel.fontStyle = FontStyle.Bold;
        finalScoreLabel.alignment = TextAnchor.MiddleCenter;
        var rt = finalScoreLabel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -30);
        rt.sizeDelta = new Vector2(700, 90);
    }

    void AddButton(Transform parent, string label, Vector2 offset, Vector2 size,
                   UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject("Button");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = new Color(0.2f, 0.65f, 0.25f);
        var btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(action);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset; rt.sizeDelta = size;

        var tObj = new GameObject("Text");
        tObj.transform.SetParent(obj.transform, false);
        var t = tObj.AddComponent<Text>();
        t.text = label; t.fontSize = 52; t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        var tRt = t.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero; tRt.anchoredPosition = Vector2.zero;
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    void MakeQuad(string name, Vector2 pos, Vector2 size, Color col, int order, bool addCollider)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = boxSprite;
        sr.color = col;
        sr.sortingOrder = order;
        if (addCollider)
        {
            var c = obj.AddComponent<BoxCollider2D>();
            c.size = Vector2.one;
        }
    }

    // ─── Public API ─────────────────────────────────────────────────────────

    public void AddScore(int points)
    {
        score += points;
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void MergeFruits(int newIndex, Vector2 position, int points)
    {
        if (isGameOver) return;
        AddScore(points);
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
