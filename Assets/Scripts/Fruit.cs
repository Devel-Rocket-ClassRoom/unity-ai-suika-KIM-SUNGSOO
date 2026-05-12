using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    public int fruitIndex;
    public bool isDropped;
    public bool isMerging;

    // ─── Static fruit data (index 0 = cherry … 10 = watermelon) ────────────

    public static readonly float[] Radii =
        { 0.30f, 0.45f, 0.55f, 0.65f, 0.80f, 0.95f, 1.10f, 1.25f, 1.45f, 1.65f, 1.95f };

    public static readonly Color[] Colors =
    {
        new Color(0.85f, 0.10f, 0.10f),   // 0  Cherry
        new Color(1.00f, 0.40f, 0.50f),   // 1  Strawberry
        new Color(0.50f, 0.20f, 0.80f),   // 2  Grape
        new Color(0.25f, 0.05f, 0.45f),   // 3  Dewberry
        new Color(1.00f, 0.55f, 0.05f),   // 4  Orange
        new Color(0.75f, 0.10f, 0.10f),   // 5  Apple
        new Color(0.75f, 0.90f, 0.35f),   // 6  Pear
        new Color(1.00f, 0.75f, 0.60f),   // 7  Peach
        new Color(0.95f, 0.85f, 0.10f),   // 8  Pineapple
        new Color(0.30f, 0.75f, 0.25f),   // 9  Melon
        new Color(0.15f, 0.50f, 0.10f),   // 10 Watermelon
    };

    public static readonly int[] Scores =
        { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66 };

    public static readonly string[] Names =
    {
        "Cherry", "Strawberry", "Grape", "Dewberry", "Orange",
        "Apple", "Pear", "Peach", "Pineapple", "Melon", "Watermelon"
    };

    // ─── Components ─────────────────────────────────────────────────────────

    private Rigidbody2D   rb;
    private CircleCollider2D cc;
    private SpriteRenderer   sr;

    private static Sprite sharedCircleSprite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    // ─── Init / Drop ────────────────────────────────────────────────────────

    public void Initialize(int index)
    {
        fruitIndex = index;
        float r = Radii[index];

        // Scale object so local-space radius 0.5 == world-space r
        transform.localScale = Vector3.one * (r * 2f);
        cc.radius = 0.5f;

        sr.sprite = GetCircleSprite();
        sr.color  = Colors[index];
        sr.sortingOrder = 1;

        rb.isKinematic   = true;
        rb.gravityScale  = 1f;
        rb.mass          = r * r * 2f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 1.0f;
    }

    public void Drop()
    {
        rb.isKinematic = false;
        isDropped      = true;
    }

    // ─── Merge logic ────────────────────────────────────────────────────────

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!isDropped || isMerging) return;

        var other = col.gameObject.GetComponent<Fruit>();
        if (other == null || other.isMerging || !other.isDropped) return;
        if (other.fruitIndex != fruitIndex || fruitIndex >= 10) return;

        // Only the fruit with the lower instance ID handles the merge (avoids double-call)
        if (gameObject.GetInstanceID() > other.gameObject.GetInstanceID()) return;

        isMerging       = true;
        other.isMerging = true;

        var mid    = ((Vector2)transform.position + (Vector2)other.transform.position) * 0.5f;
        int points = Scores[fruitIndex] * 2;

        GameManager.Instance.MergeFruits(fruitIndex + 1, mid, points);

        Destroy(other.gameObject);
        Destroy(gameObject);
    }

    // ─── Circle sprite ──────────────────────────────────────────────────────

    static Sprite GetCircleSprite()
    {
        if (sharedCircleSprite != null) return sharedCircleSprite;

        const int size = 128;
        float center = size * 0.5f;
        float radius = center - 1f;

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

        sharedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                                           new Vector2(0.5f, 0.5f), size);
        sharedCircleSprite.name = "CircleSprite";
        return sharedCircleSprite;
    }
}
