using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    // Only cherry … orange (index 0-4) can appear at the top
    private static readonly int[] SpawnPool = { 0, 0, 1, 1, 2, 2, 3, 4 };

    private int   currentIndex;
    private int   nextIndex;
    private Fruit heldFruit;

    private bool  canDrop    = true;
    private float cooldown   = 0.6f;
    private float coolTimer  = 0f;

    private GameObject dropLine;
    private SpriteRenderer nextPreview;

    void Start()
    {
        CreateDropLine();
        CreateNextPreview();
        nextIndex = RandomIndex();
        PrepareNextFruit();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        if (!canDrop)
        {
            coolTimer += Time.deltaTime;
            if (coolTimer >= cooldown)
            {
                coolTimer = 0f;
                canDrop   = true;
                PrepareNextFruit();
            }
            return;
        }

        if (heldFruit != null)
        {
            float mouseX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            float r      = Fruit.Radii[currentIndex];
            float clampX = Mathf.Clamp(mouseX, -GameManager.WallX + r, GameManager.WallX - r);
            heldFruit.transform.position = new Vector3(clampX, transform.position.y, 0);
            UpdateDropLine(clampX);
        }

        if (Input.GetMouseButtonDown(0) && heldFruit != null)
            DropHeld();
    }

    // ─── Spawn helpers ───────────────────────────────────────────────────────

    void PrepareNextFruit()
    {
        currentIndex = nextIndex;
        nextIndex    = RandomIndex();
        UpdateNextPreview();
        heldFruit = SpawnFruit(currentIndex, transform.position, kinematic: true);
    }

    void DropHeld()
    {
        heldFruit.Drop();
        heldFruit = null;
        canDrop   = false;
        dropLine.SetActive(false);
    }

    public void SpawnMergedFruit(int index, Vector2 position)
    {
        var f = SpawnFruit(index, position, kinematic: false);
        f.Drop();
    }

    static Fruit SpawnFruit(int index, Vector2 pos, bool kinematic)
    {
        var obj = new GameObject(Fruit.Names[index]);
        obj.transform.position = pos;
        var fruit = obj.AddComponent<Fruit>();
        fruit.Initialize(index);
        if (!kinematic) fruit.Drop();
        return fruit;
    }

    static int RandomIndex() => SpawnPool[Random.Range(0, SpawnPool.Length)];

    // ─── Visuals ────────────────────────────────────────────────────────────

    void CreateDropLine()
    {
        dropLine = new GameObject("DropLine");
        var sr = dropLine.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.35f));
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        sr.sortingOrder = 4;
    }

    void UpdateDropLine(float x)
    {
        if (dropLine == null) return;
        dropLine.SetActive(true);
        float lineH = transform.position.y - GameManager.FloorY;
        float cy    = GameManager.FloorY + lineH * 0.5f;
        dropLine.transform.position   = new Vector3(x, cy, 0);
        dropLine.transform.localScale = new Vector3(0.06f, lineH, 1f);
    }

    void CreateNextPreview()
    {
        // Tiny preview in the top-right area (world space, rendered behind UI)
        var obj = new GameObject("NextFruitPreview");
        nextPreview = obj.AddComponent<SpriteRenderer>();
        nextPreview.sortingOrder = 5;
        obj.transform.position = new Vector3(GameManager.WallX + 2.2f, GameManager.GameOverLineY + 1.5f, 0);
    }

    void UpdateNextPreview()
    {
        if (nextPreview == null) return;
        // Reuse Fruit.GetCircleSprite via a temporary fruit (cheap: static method via reflection)
        // Simpler: just tint the preview sprite
        float r = Fruit.Radii[nextIndex];
        nextPreview.sprite = GetSharedCircleSprite();
        nextPreview.color  = Fruit.Colors[nextIndex];
        nextPreview.transform.localScale = Vector3.one * (r * 2f * 0.6f); // 60% size for preview
    }

    // Access the shared circle sprite without instantiating a full Fruit
    static Sprite GetSharedCircleSprite()
    {
        const int size = 128;
        float center = size * 0.5f;
        float radius = center - 1f;

        // Reuse cached sprite if available via a dummy Fruit-like approach
        // Create a simple circle sprite (same as Fruit.GetCircleSprite)
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist  = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
            float alpha = Mathf.Clamp01(radius - dist + 0.5f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDisable()
    {
        if (heldFruit != null)
        {
            Destroy(heldFruit.gameObject);
            heldFruit = null;
        }
        if (dropLine != null) dropLine.SetActive(false);
    }
}
