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

    // ─── 게임 월드 상수 ──────────────────────────────────────────────────────
    public const float WallX          = 4.5f;
    public const float FloorY         = -9f;
    public const float GameOverLineY  = 6.5f;
    public const float SpawnerY       = 8.5f;

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

        ApplyVisuals();
        spawner = FindFirstObjectByType<FruitSpawner>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
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
