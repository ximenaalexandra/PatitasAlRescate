using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

// Automatiza el montaje del taller: recorte, Player (rescatista), Mascota, animaciones y HUD.
public static class TallerSetup
{
    const string GirlTex = "Assets/Sprites/Player_Rescatista.png";
    const string DogTexOld = "Assets/Sprites/Player_Dog.png";
    const string DogTex = "Assets/Sprites/Mascota_Dog.png";

    [MenuItem("Taller/Montar rescatista, mascota y HUD")]
    public static void Run()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(DogTexOld) != null)
            Debug.Log("mover: " + AssetDatabase.MoveAsset(DogTexOld, DogTex));

        Slice(GirlTex, "Player_Rescatista_", 150, 85, new[] { 8, 8, 8, 8, 8, 5, 7, 5, 5, 5, 5 });
        Slice(DogTex, "Mascota_Dog_", 64, 48, new[] { 8, 8, 8, 8, 8, 8, 8, 8, 4 });

        AssetDatabase.DeleteAsset("Assets/Animations/Player");
        AssetDatabase.DeleteAsset("Assets/Animations/Mascota");
        AssetDatabase.CreateFolder("Assets/Animations", "Player");
        AssetDatabase.CreateFolder("Assets/Animations", "Mascota");

        var girl = Sprites(GirlTex);
        var dog = Sprites(DogTex);
        AddSortingLayer("Characters");

        // --- Controlador de la rescatista ---
        var pIdle = MakeClip("Assets/Animations/Player/Player_Idle.anim", girl, "Player_Rescatista_", 0, 8, 8);
        var pWalk = MakeClip("Assets/Animations/Player/Player_Walk.anim", girl, "Player_Rescatista_", 8, 8, 10);
        var pRun = MakeClip("Assets/Animations/Player/Player_Run.anim", girl, "Player_Rescatista_", 32, 8, 12);
        var pc = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Player/Player.controller");
        pc.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        pc.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        var sm = pc.layers[0].stateMachine;
        var sIdle = sm.AddState("Player_Idle"); sIdle.motion = pIdle; sm.defaultState = sIdle;
        var sWalk = sm.AddState("Player_Walk"); sWalk.motion = pWalk;
        var sRun = sm.AddState("Player_Run"); sRun.motion = pRun;
        Link(sIdle, sWalk, AnimatorConditionMode.If, "IsWalking");
        Link(sWalk, sIdle, AnimatorConditionMode.IfNot, "IsWalking");
        Link(sWalk, sRun, AnimatorConditionMode.If, "IsRunning");
        Link(sRun, sWalk, AnimatorConditionMode.IfNot, "IsRunning");

        // --- Controlador de la mascota ---
        var mIdle = MakeClip("Assets/Animations/Mascota/Mascota_Idle.anim", dog, "Mascota_Dog_", 0, 6, 8);
        var mWalk = MakeClip("Assets/Animations/Mascota/Mascota_Walk.anim", dog, "Mascota_Dog_", 32, 8, 10);
        var mc = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Mascota/Mascota.controller");
        mc.AddMotion(mIdle);
        mc.AddMotion(mWalk);

        // --- Escena ---
        foreach (var n in new[] { "Player", "Mascota", "HUD" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        var player = MakeCharacter("Player", girl["Player_Rescatista_0"], pc, new Vector3(0, 0, 0), 2);
        player.AddComponent<PlayerMovement>();
        var stats = player.AddComponent<PlayerStats>();
        MakeCharacter("Mascota", dog["Mascota_Dog_0"], mc, new Vector3(-2f, 0, 0), 1);

        var cam = Camera.main;
        if (cam != null) { cam.orthographic = true; cam.orthographicSize = 3f; cam.transform.position = new Vector3(0, 1f, -10); }

        BuildHud(stats);
        FixLight();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Selection.activeGameObject = player;
        Debug.Log("TALLER_OK");
    }

    [MenuItem("Taller/Poner fondo de barrio")]
    public static void Fondo()
    {
        const string tex = "Assets/Sprites/Fondo_Barrio.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var old = GameObject.Find("Fondo");
        if (old != null) Object.DestroyImmediate(old);
        var go = new GameObject("Fondo");
        go.transform.position = new Vector3(0f, 1.3125f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(tex);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -10;
        go.transform.SetSiblingIndex(2);

        var player = GameObject.Find("Player");
        var mascota = GameObject.Find("Mascota");
        player.transform.position = new Vector3(-3.5f, 0f, 0f);
        mascota.transform.position = new Vector3(-5.5f, 0f, 0f);

        var cam = Camera.main;
        cam.orthographicSize = 3f;
        cam.transform.position = new Vector3(-3.5f, 1.3125f, -10f);
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.objetivo = player.transform;

        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        Debug.Log("FONDO_OK");
    }

    [MenuItem("Taller/Reconstruir HUD")]
    public static void RebuildHud()
    {
        var old = GameObject.Find("HUD");
        if (old != null) Object.DestroyImmediate(old);
        var player = GameObject.Find("Player");
        BuildHud(player.GetComponent<PlayerStats>());
        EditorUtility.SetDirty(player.GetComponent<PlayerStats>());
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Debug.Log("HUD_OK");
    }

    static void Link(AnimatorState a, AnimatorState b, AnimatorConditionMode mode, string param)
    {
        var t = a.AddTransition(b);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(mode, 0, param);
    }

    static GameObject MakeCharacter(string name, Sprite sprite, AnimatorController ctrl, Vector3 pos, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = order;
        go.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return go;
    }

    static void BuildHud(PlayerStats stats)
    {
        var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var prt = (RectTransform)panel.transform;
        prt.SetParent(hud.transform, false);
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0, 1);
        prt.anchoredPosition = new Vector2(16, -16);
        prt.sizeDelta = new Vector2(428, 105);
        panel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.20f, 0.70f);

        stats.barraSalud = MakeBar(hud.transform, "BarraSalud", "SALUD", new Color(0.85f, 0.2f, 0.25f), -30);
        stats.barraPoder = MakeBar(hud.transform, "BarraPoder", "PODER", new Color(0.2f, 0.55f, 0.95f), -75);
        stats.barraSalud.anchorMax = new Vector2(stats.salud / 100f, 1f);
        stats.barraPoder.anchorMax = new Vector2(stats.poder / 100f, 1f);
    }

    static RectTransform MakeBar(Transform parent, string name, string label, Color color, float y)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(30, y);
        rt.sizeDelta = new Vector2(400, 32);

        var text = new GameObject("Etiqueta", typeof(RectTransform), typeof(Text));
        var trt = (RectTransform)text.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(0, 1); trt.pivot = new Vector2(0, 0.5f);
        trt.anchoredPosition = Vector2.zero; trt.sizeDelta = new Vector2(100, 0);
        var t = text.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 22; t.fontStyle = FontStyle.Bold; t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        text.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

        var bg = new GameObject("Marco", typeof(RectTransform), typeof(Image));
        var brt = (RectTransform)bg.transform;
        brt.SetParent(rt, false);
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1);
        brt.offsetMin = new Vector2(100, 0); brt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

        var fill = new GameObject("Relleno", typeof(RectTransform), typeof(Image));
        var frt = (RectTransform)fill.transform;
        frt.SetParent(brt, false);
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
        frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
        var area = frt;

        var bar = new GameObject("Valor", typeof(RectTransform), typeof(Image));
        var vrt = (RectTransform)bar.transform;
        vrt.SetParent(area, false);
        vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        bar.GetComponent<Image>().color = color;
        fill.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        return vrt;
    }

    static void FixLight()
    {
        var lightGo = GameObject.Find("Global Light 2D");
        if (lightGo == null) return;
        var so = new SerializedObject(lightGo.GetComponent<UnityEngine.Rendering.Universal.Light2D>());
        var p = so.FindProperty("m_ApplyToSortingLayers");
        p.ClearArray();
        foreach (var sl in SortingLayer.layers)
        {
            p.InsertArrayElementAtIndex(p.arraySize);
            p.GetArrayElementAtIndex(p.arraySize - 1).intValue = sl.id;
        }
        so.ApplyModifiedProperties();
    }

    static Dictionary<string, Sprite> Sprites(string tex)
    {
        return AssetDatabase.LoadAllAssetsAtPath(tex).OfType<Sprite>().ToDictionary(s => s.name);
    }

    static void Slice(string tex, string prefix, int cellW, int cellH, int[] colsPerRow)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        int texH = cellH * colsPerRow.Length, i = 0;
        for (int r = 0; r < colsPerRow.Length; r++)
            for (int c = 0; c < colsPerRow[r]; c++)
                rects.Add(new SpriteRect
                {
                    name = prefix + i++,
                    rect = new Rect(c * cellW, texH - (r + 1) * cellH, cellW, cellH),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate()
                });
        dp.SetSpriteRects(rects.ToArray());
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static AnimationClip MakeClip(string path, Dictionary<string, Sprite> sprites, string prefix, int first, int count, int fps)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[count + 1];
        for (int k = 0; k <= count; k++)
        {
            // el ultimo fotograma repite el primero para un bucle limpio
            int idx = first + (k == count ? 0 : k);
            keys[k] = new ObjectReferenceKeyframe { time = k / (float)fps, value = sprites[prefix + idx] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void AddSortingLayer(string layer)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layer) return;
        layers.InsertArrayElementAtIndex(layers.arraySize);
        var e = layers.GetArrayElementAtIndex(layers.arraySize - 1);
        e.FindPropertyRelative("name").stringValue = layer;
        e.FindPropertyRelative("uniqueID").intValue = 1791652401;
        tm.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
