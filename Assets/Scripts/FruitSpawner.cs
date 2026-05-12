using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 과일 떨어뜨리기·머지 과일 생성을 담당.
/// 씬에 배치된 FruitSpawner GameObject의 Transform 위치(SpawnerY)를 기준으로 동작.
/// </summary>
public class FruitSpawner : MonoBehaviour
{
    // 스폰 풀: Cherry(0)·Strawberry(1)·Grape(2)·Dewberry(3)·Orange(4) 만 맨 위에서 등장
    private static readonly int[] SpawnPool = { 0, 0, 1, 1, 2, 2, 3, 4 };

    private int   currentIndex;
    private int   nextIndex;
    private Fruit heldFruit;

    private bool  canDrop   = true;
    private float cooldown  = 0.6f;
    private float coolTimer = 0f;

    private GameObject dropLine;
    private Image      nextPreviewImage;
    private static Sprite sharedPreviewSprite;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    void Start()
    {
        CreateDropLine();
        CreateNextPreview();
        nextIndex = RandomIndex();
        PrepareNextFruit();
    }

    // ─── 매 프레임 ───────────────────────────────────────────────────────────

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

        // 마우스 X 위치로 과일 이동
        if (heldFruit != null)
        {
            float mx    = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            float r     = Fruit.Radii[currentIndex];
            float clamp = Mathf.Clamp(mx, -GameManager.WallX + r, GameManager.WallX - r);
            heldFruit.transform.position = new Vector3(clamp, transform.position.y, 0);
            UpdateDropLine(clamp);
        }

        // 클릭으로 낙하
        if (Input.GetMouseButtonDown(0) && heldFruit != null)
            DropHeld();
    }

    // ─── 스폰 ────────────────────────────────────────────────────────────────

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
        if (dropLine != null) dropLine.SetActive(false);
    }

    public void SpawnMergedFruit(int index, Vector2 position)
        => SpawnFruit(index, position, kinematic: false);

    static Fruit SpawnFruit(int index, Vector2 pos, bool kinematic)
    {
        var obj   = new GameObject(Fruit.Names[index]);
        obj.transform.position = pos;
        var fruit = obj.AddComponent<Fruit>();
        fruit.Initialize(index);
        if (!kinematic) fruit.Drop();
        return fruit;
    }

    static int RandomIndex() => SpawnPool[Random.Range(0, SpawnPool.Length)];

    // ─── 낙하 가이드 라인 ────────────────────────────────────────────────────

    void CreateDropLine()
    {
        dropLine = new GameObject("DropLine");
        var sr   = dropLine.AddComponent<SpriteRenderer>();
        var tex  = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.35f));
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        sr.sortingOrder = 4;
        dropLine.SetActive(false);
    }

    void UpdateDropLine(float x)
    {
        if (dropLine == null) return;
        dropLine.SetActive(true);
        float h  = transform.position.y - GameManager.FloorY;
        float cy = GameManager.FloorY + h * 0.5f;
        dropLine.transform.position   = new Vector3(x, cy, 0);
        dropLine.transform.localScale = new Vector3(0.06f, h, 1f);
    }

    // ─── 다음 과일 미리보기 (Canvas UI 패널) ─────────────────────────────────

    void CreateNextPreview()
    {
        sharedPreviewSprite = BuildCircleSprite();

        // Canvas
        var cv = new GameObject("NextPreviewCanvas");
        var canvas = cv.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        cv.AddComponent<GraphicRaycaster>();

        // 패널 배경 (갈색 박스)
        var panel = new GameObject("NextPanel");
        panel.transform.SetParent(cv.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.55f, 0.38f, 0.22f, 0.95f);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-30f, -30f);
        panelRt.sizeDelta        = new Vector2(210f, 260f);

        // "NEXT" 라벨
        var lObj = new GameObject("NextLabel");
        lObj.transform.SetParent(panel.transform, false);
        var lTxt = lObj.AddComponent<Text>();
        lTxt.text      = "NEXT";
        lTxt.fontSize  = 48;
        lTxt.color     = Color.white;
        lTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lTxt.fontStyle = FontStyle.Bold;
        lTxt.alignment = TextAnchor.MiddleCenter;
        var lRt = lTxt.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 1f); lRt.anchorMax = new Vector2(1f, 1f);
        lRt.pivot     = new Vector2(0.5f, 1f);
        lRt.anchoredPosition = new Vector2(0f, -8f);
        lRt.sizeDelta        = new Vector2(0f, 65f);

        // 과일 아이콘
        var iObj = new GameObject("NextFruitIcon");
        iObj.transform.SetParent(panel.transform, false);
        nextPreviewImage = iObj.AddComponent<Image>();
        nextPreviewImage.sprite = sharedPreviewSprite;
        var iRt = iObj.GetComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = new Vector2(0f, -30f);
        iRt.sizeDelta        = new Vector2(140f, 140f);
    }

    void UpdateNextPreview()
    {
        if (nextPreviewImage == null) return;
        float r     = Fruit.Radii[nextIndex];
        float scale = Mathf.Lerp(0.35f, 1f, (r - Fruit.Radii[0]) / (Fruit.Radii[Fruit.Radii.Length - 1] - Fruit.Radii[0]));
        nextPreviewImage.color    = Fruit.Colors[nextIndex];
        nextPreviewImage.rectTransform.sizeDelta = Vector2.one * 140f * scale;
    }

    static Sprite BuildCircleSprite()
    {
        const int size = 64;
        float c = size * 0.5f, rad = c - 1f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px  = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(rad - d + 0.5f));
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ─── 비활성화 처리 ───────────────────────────────────────────────────────

    void OnDisable()
    {
        if (heldFruit != null) { Destroy(heldFruit.gameObject); heldFruit = null; }
        if (dropLine   != null) dropLine.SetActive(false);
    }
}
