using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 상태(점수·게임오버) 관리 및 씬 오브젝트 시각화.
/// UI 오브젝트는 씬에 미리 배치하고 SerializeField로 참조.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─── 게임 월드 크기 (Inspector에서 조정 가능) ────────────────────────────
    [SerializeField] private float wallX         = 4.5f;
    [SerializeField] private float floorY        = -9f;
    [SerializeField] private float gameOverLineY = 6.5f;
    [SerializeField] private float spawnerY      = 8.5f;

    public float WallX         => wallX;
    public float FloorY        => floorY;
    public float GameOverLineY => gameOverLineY;
    public float SpawnerY      => spawnerY;

    // ─── 씬 UI 참조 (Inspector에서 연결) ────────────────────────────────────
    [SerializeField] private Text       scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text       finalScoreLabel;

    // ─── 런타임 참조 ─────────────────────────────────────────────────────────
    private FruitSpawner spawner;
    private bool         isGameOver;
    private int          score;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Physics2D.gravity = new Vector2(0f, -18f);

        ApplyLayout();
        ApplyVisuals();
        spawner = FindFirstObjectByType<FruitSpawner>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // ─── 환경 오브젝트 레이아웃 ─────────────────────────────────────────────

    void ApplyLayout()
    {
        float wallThick = 0.5f;
        float wallHalf  = wallThick * 0.5f;

        SetTransform("Background",
            new Vector3(0, (floorY + gameOverLineY) * 0.5f, 0),
            new Vector3(wallX * 2f, gameOverLineY - floorY, 1f));

        SetTransform("Floor",
            new Vector3(0, floorY - wallHalf, 0),
            new Vector3(wallX * 2f + wallThick * 2f, wallThick, 1f));

        SetTransform("LeftWall",
            new Vector3(-(wallX + wallHalf), 0, 0),
            new Vector3(wallThick, 40f, 1f));

        SetTransform("RightWall",
            new Vector3(wallX + wallHalf, 0, 0),
            new Vector3(wallThick, 40f, 1f));

        SetTransform("GameOverLine",
            new Vector3(0, gameOverLineY, 0),
            new Vector3(wallX * 2f, 0.12f, 1f));

        SetTransform("GameOverZone",
            new Vector3(0, gameOverLineY + 1f, 0),
            new Vector3(wallX * 2f, 2f, 1f));

        var sp = GameObject.Find("FruitSpawner");
        if (sp != null) sp.transform.position = new Vector3(0, spawnerY, 0);
    }

    static void SetTransform(string goName, Vector3 pos, Vector3 scale)
    {
        var obj = GameObject.Find(goName);
        if (obj == null) return;
        obj.transform.position   = pos;
        obj.transform.localScale = scale;
    }

    // ─── 환경 오브젝트 시각화 ────────────────────────────────────────────────

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

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public bool IsGameOver => isGameOver;
}
