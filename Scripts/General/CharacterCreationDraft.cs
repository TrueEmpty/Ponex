using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Working session for Character Creation. Survives Play Mode stop/start so you can
/// iterate on scripts/objects without clicking Save Character every time.
/// Official roster export still goes through Save Character → Resources/Characters.
/// </summary>
[CreateAssetMenu(fileName = "ActiveDraft", menuName = "Ponex/Character Creation Draft", order = 1)]
public class CharacterCreationDraft : ScriptableObject
{
    public const string DraftFolder = "Assets/Data/CharacterCreation";
    public const string DraftAssetPath = DraftFolder + "/ActiveDraft.asset";
    public const string DraftCharacterPrefabPath = DraftFolder + "/Draft_Character.prefab";
    public const string DraftLifelinePrefabPath = DraftFolder + "/Draft_Lifeline.prefab";
    public bool hasSession;

    public int fieldIndex = -1;

    public string characterName = "New Character";
    public int maxHealth = 10;
    public float movementSpeed = 5f;
    public float pushBack = 1000f;
    public bool ignoreFacing;

    public Skill bump = new Skill();
    public Skill super = new Skill();
    public Skill dash = new Skill();

    public ObjectInfo character = new ObjectInfo();
    public ObjectInfo lifeline = new ObjectInfo();
    /// <summary>Original assigned prefab (materials intact). Preferred over draft bake copies.</summary>
    public GameObject sourceCharacterPrefab;
    public GameObject sourceLifelinePrefab;
    public GameObject playerInfo;
    public bool playerInfoCustom;

    public string superName;
    [TextArea(2, 5)]
    public string superDescription;
    public Color portraitColor = Color.cyan;
    public Texture portrait;
    public Texture icon;

    public Characters ToCharacters()
    {
        Characters c = new Characters();
        c.name = characterName;
        c.maxHealth = maxHealth;
        c.movementSpeed = movementSpeed;
        c.pushBack = pushBack;
        c.ignoreFacing = ignoreFacing;
        c.bump = bump != null ? new Skill(bump) : new Skill();
        c.super = super != null ? new Skill(super) : new Skill();
        c.dash = dash != null ? new Skill(dash) : new Skill();
        c.character = character != null ? new ObjectInfo(character) : new ObjectInfo();
        c.lifeline = lifeline != null ? new ObjectInfo(lifeline) : new ObjectInfo();
        // Prefer source prefabs (real materials) over pink draft bakes.
        // Never use a Field_Info prefab as a character / lifeline.
        if (sourceCharacterPrefab != null && !IsFieldPrefab(sourceCharacterPrefab))
        {
            if (c.character == null)
                c.character = new ObjectInfo();
            c.character.prefabs = sourceCharacterPrefab;
        }
        else if (c.character != null && IsFieldPrefab(c.character.prefabs))
            c.character.prefabs = null;

        if (sourceLifelinePrefab != null && !IsFieldPrefab(sourceLifelinePrefab))
        {
            if (c.lifeline == null)
                c.lifeline = new ObjectInfo();
            c.lifeline.prefabs = sourceLifelinePrefab;
        }
        else if (c.lifeline != null && IsFieldPrefab(c.lifeline.prefabs))
            c.lifeline.prefabs = null;
        c.playerInfo = playerInfoCustom ? playerInfo : null;
        c.superName = superName;
        c.superDescription = superDescription;
        c.portraitColor = portraitColor;
        c.portrait = portrait;
        c.icon = icon;
        c.active = true;
        return c;
    }

    public void CaptureFromPlayer(Player p, int selectedField, bool infoDirty)
    {
        if (p == null)
            return;

        hasSession = true;
        fieldIndex = selectedField;
        characterName = p.name;
        maxHealth = p.maxHealth;
        movementSpeed = p.movementSpeed;
        pushBack = p.pushBack;
        ignoreFacing = p.ignoreFacing;
        bump = p.bump != null ? new Skill(p.bump) : new Skill();
        super = p.super != null ? new Skill(p.super) : new Skill();
        dash = p.dash != null ? new Skill(p.dash) : new Skill();
        character = p.character != null ? new ObjectInfo(p.character) : new ObjectInfo();
        lifeline = p.lifeline != null ? new ObjectInfo(p.lifeline) : new ObjectInfo();
        playerInfoCustom = infoDirty;
        playerInfo = infoDirty ? p.playerInfo : null;
        superName = p.superName;
        superDescription = p.superDescription;
        portraitColor = p.portraitColor;
        portrait = p.portrait;
        icon = p.icon;
    }

    public static bool IsFieldPrefab(GameObject prefab)
    {
        return prefab != null && prefab.GetComponentInChildren<Field_Info>(true) != null;
    }

#if UNITY_EDITOR
    public static CharacterCreationDraft LoadOrCreate()
    {
        EnsureFolder(DraftFolder);
        CharacterCreationDraft draft = AssetDatabase.LoadAssetAtPath<CharacterCreationDraft>(DraftAssetPath);
        if (draft == null)
        {
            draft = CreateInstance<CharacterCreationDraft>();
            AssetDatabase.CreateAsset(draft, DraftAssetPath);
            AssetDatabase.SaveAssets();
        }
        RepairMissingMaterialRefs(draft);
        return draft;
    }

    /// <summary>
    /// If draft points at a bake with null materials, retarget to source / known good prefabs.
    /// </summary>
    public static void RepairMissingMaterialRefs(CharacterCreationDraft draft)
    {
        if (draft == null)
            return;

        bool dirty = false;

        if (IsFieldPrefab(draft.sourceCharacterPrefab)
            || (draft.sourceCharacterPrefab == null && draft.character != null))
        {
            GameObject better = FindBestSourcePrefab(draft.characterName, isLifeline: false);
            if (better != null && better != draft.sourceCharacterPrefab)
            {
                draft.sourceCharacterPrefab = better;
                dirty = true;
            }
            else if (IsFieldPrefab(draft.sourceCharacterPrefab))
            {
                draft.sourceCharacterPrefab = null;
                dirty = true;
            }
        }

        if (IsFieldPrefab(draft.sourceLifelinePrefab)
            || (draft.sourceLifelinePrefab == null && draft.lifeline != null))
        {
            GameObject better = FindBestSourcePrefab(draft.characterName, isLifeline: true);
            if (better != null && better != draft.sourceLifelinePrefab)
            {
                draft.sourceLifelinePrefab = better;
                dirty = true;
            }
            else if (IsFieldPrefab(draft.sourceLifelinePrefab))
            {
                draft.sourceLifelinePrefab = null;
                dirty = true;
            }
        }

        if (draft.character != null
            && (PrefabHasMissingMaterials(draft.character.prefabs) || IsFieldPrefab(draft.character.prefabs)))
        {
            if (draft.sourceCharacterPrefab != null && !IsFieldPrefab(draft.sourceCharacterPrefab))
            {
                draft.character.prefabs = draft.sourceCharacterPrefab;
                dirty = true;
            }
            else if (IsFieldPrefab(draft.character.prefabs))
            {
                draft.character.prefabs = null;
                dirty = true;
            }
        }

        if (draft.lifeline != null
            && (PrefabHasMissingMaterials(draft.lifeline.prefabs) || IsFieldPrefab(draft.lifeline.prefabs)))
        {
            if (draft.sourceLifelinePrefab != null && !IsFieldPrefab(draft.sourceLifelinePrefab))
            {
                draft.lifeline.prefabs = draft.sourceLifelinePrefab;
                dirty = true;
            }
            else if (IsFieldPrefab(draft.lifeline.prefabs))
            {
                draft.lifeline.prefabs = null;
                dirty = true;
            }
        }

        if (dirty)
        {
            EditorUtility.SetDirty(draft);
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterCreation] Repaired draft prefab refs (materials / rejected field bake).");
        }
    }

    static GameObject FindBestSourcePrefab(string characterName, bool isLifeline)
    {
        string safe = string.IsNullOrWhiteSpace(characterName) ? "" : characterName.Trim();
        string[] candidates;
        if (isLifeline)
        {
            candidates = new[]
            {
                "Assets/_Temp Prefabs/Lifeline/" + safe + ".prefab",
                "Assets/Prefabs/_Lifeline/" + safe + ".prefab",
                "Assets/Prefabs/Lifeline/" + safe + ".prefab",
            };
        }
        else
        {
            candidates = new[]
            {
                "Assets/_Temp Prefabs/Character/" + safe + ".prefab",
                "Assets/Prefabs/_Characters/" + safe + ".prefab",
                "Assets/Prefabs/Characters/" + safe + ".prefab",
            };
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(candidates[i]);
            if (go != null && !PrefabHasMissingMaterials(go) && !IsFieldPrefab(go))
                return go;
        }
        return null;
    }

    public static bool PrefabHasMissingMaterials(GameObject prefab)
    {
        if (prefab == null)
            return true;
        Renderer[] rends = prefab.GetComponentsInChildren<Renderer>(true);
        int checkedMats = 0;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null || rends[i] is ParticleSystemRenderer)
                continue;
            Material[] mats = rends[i].sharedMaterials;
            if (mats == null)
                continue;
            for (int m = 0; m < mats.Length; m++)
            {
                checkedMats++;
                if (mats[m] == null)
                    return true;
            }
        }
        return checkedMats == 0 && rends.Length > 0;
    }

    public static GameObject SaveDraftPrefab(GameObject liveRoot, string path)
    {
        if (liveRoot == null)
            return null;
        EnsureFolder(DraftFolder);

        // Never bake a live PlayerSkin-tinted instance with broken mats.
        // Clone, restore sharedMaterials from the prefab source when possible, then save.
        GameObject clone = Object.Instantiate(liveRoot);
        clone.name = liveRoot.name;
        RestoreSharedMaterialsFromSource(liveRoot, clone);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(clone, path);
        Object.DestroyImmediate(clone);

        if (PrefabHasMissingMaterials(saved))
        {
            Debug.LogWarning("[CharacterCreation] Draft bake still missing materials — keep using source prefab instead.");
            return null;
        }
        return saved;
    }

    static void RestoreSharedMaterialsFromSource(GameObject liveRoot, GameObject cloneRoot)
    {
        Renderer[] liveRends = liveRoot.GetComponentsInChildren<Renderer>(true);
        Renderer[] cloneRends = cloneRoot.GetComponentsInChildren<Renderer>(true);
        int n = Mathf.Min(liveRends.Length, cloneRends.Length);
        for (int i = 0; i < n; i++)
        {
            Renderer live = liveRends[i];
            Renderer clone = cloneRends[i];
            if (live == null || clone == null)
                continue;

            clone.SetPropertyBlock(null);

            Renderer srcRen = null;
            GameObject srcGo = PrefabUtility.GetCorrespondingObjectFromSource(live.gameObject);
            if (srcGo != null)
                srcRen = srcGo.GetComponent<Renderer>();

            if (srcRen != null && srcRen.sharedMaterials != null)
            {
                clone.sharedMaterials = srcRen.sharedMaterials;
                continue;
            }

            // Fall back: only keep materials that are real assets
            Material[] mats = live.sharedMaterials;
            if (mats == null)
                continue;
            Material[] cleaned = new Material[mats.Length];
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat != null && AssetDatabase.Contains(mat))
                    cleaned[m] = mat;
                else if (mat != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(mat);
                    if (!string.IsNullOrEmpty(assetPath))
                        cleaned[m] = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                }
            }
            clone.sharedMaterials = cleaned;
        }
    }

    static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;
        string[] parts = assetPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
#endif
}
