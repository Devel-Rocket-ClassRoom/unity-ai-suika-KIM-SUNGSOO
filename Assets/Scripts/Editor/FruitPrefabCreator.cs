using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Suika > 과일 프리팹 생성
/// Cherry(0) ~ Watermelon(10) 프리팹을 Assets/Prefabs/Fruits/ 에 만들고
/// 씬의 FruitSpawner 에 자동 연결합니다.
/// </summary>
public static class FruitPrefabCreator
{
    const string PrefabFolder = "Assets/Prefabs/Fruits";
    const string SpritePath   = "Assets/Prefabs/Fruits/FruitCircle.png";

    [MenuItem("Tools/Suika/과일 프리팹 생성")]
    static void CreateAll()
    {
        EnsureFolders();
        var circle = GetOrCreateCircleSprite();

        var prefabs = new GameObject[Fruit.Names.Length];

        for (int i = 0; i < Fruit.Names.Length; i++)
        {
            string path = $"{PrefabFolder}/{Fruit.Names[i]}.prefab";

            var go = new GameObject(Fruit.Names[i]);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = circle;
            sr.color        = Fruit.Colors[i];
            sr.sortingOrder = 1;

            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();

            var fruit        = go.AddComponent<Fruit>();
            fruit.fruitIndex = i;

            prefabs[i] = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[FruitPrefabCreator] ✓ {Fruit.Names[i]}  →  {path}");
        }

        WireToSpawner(prefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FruitPrefabCreator] 완료! Assets/Prefabs/Fruits/ 를 확인하세요.");
    }

    // ─── 씬의 FruitSpawner 에 자동 연결 ────────────────────────────────────

    static void WireToSpawner(GameObject[] prefabs)
    {
        var spawner = Object.FindFirstObjectByType<FruitSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[FruitPrefabCreator] 씬에 FruitSpawner 가 없어 자동 연결을 건너뜁니다.");
            return;
        }

        var so   = new SerializedObject(spawner);
        var prop = so.FindProperty("fruitPrefabs");
        prop.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log("[FruitPrefabCreator] FruitSpawner 에 프리팹 자동 연결 완료.");
    }

    // ─── 폴더 생성 ──────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Fruits");
    }

    // ─── 임시 흰색 원형 스프라이트 (실제 이미지로 교체 가능) ────────────────

    static Sprite GetOrCreateCircleSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null) return existing;

        const int size = 128;
        float c = size * 0.5f, rad = c - 1f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px  = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rad - d + 0.5f));
        }
        tex.SetPixels(px);
        tex.Apply();

        string abs = Path.Combine(Application.dataPath,
                                  SpritePath.Substring("Assets/".Length));
        File.WriteAllBytes(abs, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType        = TextureImporterType.Sprite;
        importer.spriteImportMode   = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode         = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
