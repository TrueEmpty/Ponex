#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes PlayerSelector click wiring into prefabs/scenes (not runtime AddComponent).
/// Menu: Tools/Ponex/Bake Selector UI
/// Batch: -executeMethod BakeSelectorUI.BakeFromCommandLine
/// </summary>
public static class BakeSelectorUI
{
    const string FieldSelectPrefab = "Assets/Prefabs/UIs/Field Select.prefab";
    const string GameplayScene = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Tools/Ponex/Bake Selector UI")]
    public static void BakeFromMenu()
    {
        BakeAll();
        EditorUtility.DisplayDialog("Bake Selector UI", "Baked Field/Position/Team/Ball selector UI into assets.", "OK");
    }

    public static void BakeFromCommandLine()
    {
        try
        {
            BakeAll();
            Debug.Log("[BakeSelectorUI] Success");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[BakeSelectorUI] Failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void BakeAll()
    {
        BakeFieldSelectPrefab();
        BakeGameplayScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void BakeFieldSelectPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FieldSelectPrefab);
        try
        {
            int n = BakeFieldGrabsUnder(root.transform);
            CreateOrWireFieldPageButtons(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, FieldSelectPrefab);
            Debug.Log($"[BakeSelectorUI] Field Select prefab: {n} FieldGrabs");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BakeGameplayScene()
    {
        if (!System.IO.File.Exists(GameplayScene))
        {
            Debug.LogWarning("[BakeSelectorUI] Missing " + GameplayScene);
            return;
        }

        Scene scene;
        bool openedTemp = false;

        // Prefer already-open Gameplay so we don't stomp the user's open scene
        scene = default;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == GameplayScene)
            {
                scene = s;
                break;
            }
        }

        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Additive);
            openedTemp = true;
        }

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            Transform[] all = go.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == "Field Select" && t.GetComponent<FieldSelect>() != null)
                {
                    BakeFieldGrabsUnder(t);
                    CreateOrWireFieldPageButtons(t);
                    WireConfirmAsConfirmClicked(t);
                }
                else if (t.name == "Position Select" && t.GetComponent<PositionSelect>() != null)
                {
                    BakePositionSelect(t);
                    WireConfirmAsConfirmClicked(t);
                }
                else if (t.name == "Team Select" && t.GetComponent<TeamSelect>() != null)
                {
                    BakeTeamSelect(t);
                    WireConfirmAsConfirmClicked(t);
                }
                else if (t.name == "Ball Select" && t.GetComponent<BallSelect>() != null)
                {
                    BakeBallSelect(t);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedTemp)
            EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[BakeSelectorUI] Gameplay scene baked");
    }

    static int BakeFieldGrabsUnder(Transform root)
    {
        int count = 0;
        List<FieldGrab> grabs = new List<FieldGrab>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (!t.name.ToLower().Contains("field grab"))
                continue;

            EnsureClickable(t.gameObject, 80f);
            FieldGrab fg = t.GetComponent<FieldGrab>();
            if (fg == null)
                fg = t.gameObject.AddComponent<FieldGrab>();
            grabs.Add(fg);
            count++;
        }

        FieldSelect fs = root.GetComponent<FieldSelect>();
        if (fs == null)
            fs = root.GetComponentInChildren<FieldSelect>(true);
        if (fs != null)
        {
            fs.fieldGrabs = grabs;
            EditorUtility.SetDirty(fs);
        }

        return count;
    }

    static void CreateOrWireFieldPageButtons(Transform root)
    {
        Transform holder = FindDeep(root, "PageButtons");
        if (holder == null)
        {
            GameObject go = new GameObject("PageButtons", typeof(RectTransform));
            go.transform.SetParent(root, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(400f, 60f);
            holder = go.transform;

            FieldSelect fs = root.GetComponent<FieldSelect>();
            if (fs != null)
            {
                fs.pageButtonsHolder = holder;
                EditorUtility.SetDirty(fs);
            }
        }

        WireOrCreatePage(holder, "Prev", -1, new Vector2(-120f, 0f));
        WireOrCreatePage(holder, "Next", 1, new Vector2(120f, 0f));
    }

    static void WireOrCreatePage(Transform parent, string name, int dir, Vector2 pos)
    {
        Transform t = FindDeep(parent, name);
        GameObject go;
        if (t == null)
            go = CreateUiButton(parent, name, pos, new Vector2(140f, 50f));
        else
            go = t.gameObject;

        EnsureClickable(go, 100f);
        FieldPageButton btn = go.GetComponent<FieldPageButton>();
        if (btn == null)
            btn = go.AddComponent<FieldPageButton>();
        btn.direction = dir;
        EditorUtility.SetDirty(go);
    }

    static void BakePositionSelect(Transform root)
    {
        PositionSelect ps = root.GetComponent<PositionSelect>();
        Transform zones = FindDeep(root, "Zones");
        if (zones == null)
        {
            GameObject z = new GameObject("Zones", typeof(RectTransform));
            z.transform.SetParent(root, false);
            RectTransform zrt = z.GetComponent<RectTransform>();
            zrt.anchorMin = Vector2.zero;
            zrt.anchorMax = Vector2.one;
            zrt.offsetMin = new Vector2(400f, 150f);
            zrt.offsetMax = new Vector2(-400f, -100f);
            zones = z.transform;
            // Place after Boarder / before Markers if possible
            zones.SetSiblingIndex(Mathf.Min(2, root.childCount - 1));
        }

        Facing[] facings = { Facing.Up, Facing.Down, Facing.Left, Facing.Right };
        Vector3 setPos = ps != null ? ps.setPos : new Vector3(225f, 175f, 25f);

        foreach (Facing f in facings)
        {
            Transform side = FindDeep(zones, f.ToString());
            if (side == null)
            {
                GameObject s = new GameObject(f.ToString(), typeof(RectTransform));
                s.transform.SetParent(zones, false);
                RectTransform srt = s.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(100f, 100f);
                srt.anchoredPosition = Vector2.zero;
                side = s.transform;
            }

            for (int slot = 0; slot < 2; slot++)
            {
                string slotName = f + "_Slot" + slot;
                Transform existing = side.childCount > slot ? side.GetChild(slot) : null;
                GameObject go;
                if (existing != null && (existing.GetComponent<Image>() != null || existing.GetComponent<PositionSlot>() != null))
                {
                    go = existing.gameObject;
                    go.name = slotName;
                }
                else
                {
                    go = CreateSlotObject(side, slotName, f, slot, setPos);
                }

                EnsureClickable(go, 160f);
                PositionSlot psSlot = go.GetComponent<PositionSlot>();
                if (psSlot == null)
                    psSlot = go.AddComponent<PositionSlot>();
                psSlot.facing = f;
                psSlot.slot = slot;
                psSlot.SetupVisual();
                EditorUtility.SetDirty(go);
            }
        }

        if (ps != null)
            EditorUtility.SetDirty(ps);
    }

    static GameObject CreateSlotObject(Transform parent, string name, Facing f, int slot, Vector3 setPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 50f);
        PlaceSlot(rt, f, slot, setPos);

        Image img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);

        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = new Color(0.1f, 0.35f, 0.95f, 1f);
        ol.effectDistance = new Vector2(3f, 3f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text txt = textGo.GetComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.text = f + (slot == 0 ? " A" : " B");
        return go;
    }

    static void PlaceSlot(RectTransform rt, Facing f, int slot, Vector3 setPos)
    {
        Vector2 cP = new Vector2(slot == 0 ? setPos.z : -setPos.z, slot == 0 ? setPos.x : setPos.y);
        switch (f)
        {
            case Facing.Up:
                rt.anchoredPosition = new Vector2(cP.x, -cP.y);
                break;
            case Facing.Down:
                rt.anchoredPosition = new Vector2(cP.x, cP.y);
                break;
            case Facing.Right:
                rt.anchoredPosition = new Vector2(-cP.y, cP.x);
                break;
            case Facing.Left:
                rt.anchoredPosition = new Vector2(cP.y, cP.x);
                break;
        }
    }

    static void BakeTeamSelect(Transform root)
    {
        Transform assignments = FindDeep(root, "Assignments");
        if (assignments == null && root.childCount > 1)
            assignments = root.GetChild(1);

        if (assignments == null)
            return;

        for (int i = 0; i < assignments.childCount; i++)
        {
            GameObject go = assignments.GetChild(i).gameObject;
            EnsureClickable(go, 180f);
            TeamCard card = go.GetComponent<TeamCard>();
            if (card == null)
                card = go.AddComponent<TeamCard>();
            EditorUtility.SetDirty(go);
        }
    }

    static void BakeBallSelect(Transform root)
    {
        BallSelect bs = root.GetComponent<BallSelect>();
        Transform controls = FindDeep(root, "Controls");
        if (controls == null)
        {
            GameObject c = new GameObject("Controls", typeof(RectTransform));
            c.transform.SetParent(root, false);
            RectTransform crt = c.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, -80f);
            crt.sizeDelta = new Vector2(600f, 80f);
            controls = c.transform;
            if (bs != null)
            {
                bs.controlsHolder = controls;
                EditorUtility.SetDirty(bs);
            }
        }

        WireBallStep(FindOrCreateButton(controls, "Prev", new Vector2(-250f, 0f)), -1, false);
        WireBallStep(FindOrCreateButton(controls, "Next", new Vector2(250f, 0f)), 1, false);
        WireBallStep(FindOrCreateButton(controls, "Random", new Vector2(0f, -70f)), 0, true);

        if (bs != null && bs.ballName != null)
        {
            EnsureClickable(bs.ballName.gameObject, 200f);
            BallStepButton step = bs.ballName.GetComponent<BallStepButton>();
            if (step == null)
                step = bs.ballName.gameObject.AddComponent<BallStepButton>();
            step.step = 1;
            step.setRandom = false;
            EditorUtility.SetDirty(bs.ballName.gameObject);
        }
    }

    static GameObject FindOrCreateButton(Transform parent, string name, Vector2 pos)
    {
        Transform t = FindDeep(parent, name);
        if (t != null)
            return t.gameObject;
        return CreateUiButton(parent, name, pos, new Vector2(140f, 50f));
    }

    static void WireBallStep(GameObject go, int step, bool random)
    {
        EnsureClickable(go, 140f);
        BallStepButton btn = go.GetComponent<BallStepButton>();
        if (btn == null)
            btn = go.AddComponent<BallStepButton>();
        btn.step = step;
        btn.setRandom = random;
        EditorUtility.SetDirty(go);
    }

    static void WireConfirmAsConfirmClicked(Transform menuRoot)
    {
        Transform confirm = FindDeep(menuRoot, "Confirm");
        if (confirm == null)
            return;

        EnsureClickable(confirm.gameObject, 170f);

        // Position/Team need per-player ConfirmClicked; keep RunOnClicked for Field/Ball if already wired.
        string menu = menuRoot.name.ToLower();
        bool perPlayer = menu.Contains("position") || menu.Contains("team");
        if (!perPlayer)
            return;

        RunOnClicked roc = confirm.GetComponent<RunOnClicked>();
        if (roc != null)
            Object.DestroyImmediate(roc, true);

        if (confirm.GetComponent<ConfirmClicked>() == null)
            confirm.gameObject.AddComponent<ConfirmClicked>();

        EditorUtility.SetDirty(confirm.gameObject);
    }

    static GameObject CreateUiButton(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Text));
        go.transform.SetParent(parent, false);
        go.tag = "Selection";
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = new Color(0.1f, 0.35f, 0.95f, 1f);
        ol.effectDistance = new Vector2(3f, 3f);

        Text txt = go.GetComponent<Text>();
        txt.text = name;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.raycastTarget = false;
        return go;
    }

    static void EnsureClickable(GameObject go, float fallbackSize)
    {
        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null)
            col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            float w = rt.rect.width;
            float h = rt.rect.height;
            if (w < 1f) w = fallbackSize;
            if (h < 1f) h = fallbackSize;
            col.size = new Vector3(w, h, 1f);
        }
        else
        {
            col.size = new Vector3(fallbackSize, fallbackSize, 1f);
        }
        col.center = Vector3.zero;

        if (go.GetComponent<ButtonInteraction>() == null)
        {
            ButtonInteraction bi = go.AddComponent<ButtonInteraction>();
            bi.fade = false;
            bi.fadeAmount = 0.5f;
            bi.highlightColor = new Color(0f, 0.46f, 1f, 1f);
            bi.pressedColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            bi.deactivatedColor = new Color(0.46f, 0.46f, 0.46f, 0.32f);
            bi.deactivatedColor_outline = new Color(0.15f, 0.15f, 0.15f, 0.53f);
        }

        if (go.GetComponent<Outline>() == null && go.GetComponent<Image>() != null)
        {
            Outline ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.1f, 0.35f, 0.95f, 1f);
            ol.effectDistance = new Vector2(3f, 3f);
        }

        EditorUtility.SetDirty(go);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
#endif
