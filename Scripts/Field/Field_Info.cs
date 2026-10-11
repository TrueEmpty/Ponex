using System.Collections.Generic;
using UnityEngine;

public class Field_Info : MonoBehaviour
{
    public static Field_Info instance;

    public Field field;
    Database db;
    MenuManager mM;

    void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Start()
    {
        db = Database.instance;
        mM = MenuManager.instance;
        Preload();
        LoadField();
    }

    void Preload()
    {
        int fs = field != null ? field.size : 0;
        float play = fs + 10;
        if (db != null && db.FieldPlaySize > 0.01f)
            play = db.FieldPlaySize;

        for (int i = 0; i < 4; i++)
        {
            GameObject oob = Instantiate(db.outofBounds);
            oob.transform.parent = transform;

            Vector3 oobLoc = new Vector3(0, (play / 2f) + 1f, 0);
            Vector3 oobScale = new Vector3(play + 3f, 1f, 1f);

            switch (i)
            {
                case 1:
                    oobLoc = new Vector3((play / 2f) + 1f, 0f, 0f);
                    oobScale = new Vector3(1f, play + 3f, 1f);
                    break;
                case 2:
                    oobLoc = new Vector3(0f, -((play / 2f) + 1f), 0f);
                    oobScale = new Vector3(play + 3f, 1f, 1f);
                    break;
                case 3:
                    oobLoc = new Vector3(-((play / 2f) + 1f), 0f, 0f);
                    oobScale = new Vector3(1f, play + 3f, 1f);
                    break;
            }

            oob.transform.localPosition = oobLoc;
            oob.transform.localScale = oobScale;
        }

        GameObject background = Instantiate(db.background);
        background.name = "Background";
        background.transform.parent = transform;
        background.transform.localPosition = new Vector3(0, 0, .5f);

        Renderer bRen = background.GetComponent<Renderer>();
        if (bRen != null && field != null)
        {
            // Fully clear bg (Deep Harbor uses water terrain instead)
            if (field.backgroundColor.a < 0.05f)
            {
                bRen.enabled = false;
                background.SetActive(false);
            }
            else
            {
                Material authoredGround = db != null ? db.GroundMaterialFor(field.name) : null;
                if (authoredGround != null)
                {
                    bRen.sharedMaterial = authoredGround;
                }
                else
                {
                    FieldTextureFactory.ApplyAlbedo(
                        bRen,
                        field.backgroundMaterial,
                        field.backgroundColor,
                        field.backgroundMatallic,
                        field.backgroundSmoothness);
                    if (field.backgroundMaterial != null)
                        bRen.material.mainTextureScale = field.tilling;
                }
            }
        }
    }

    public void LoadField()
    {
        if (field == null || field.parts == null || field.parts.Count == 0)
            return;

        GameSettings.EnsureLoaded();
        string menuTitle = "";
        if (mM != null)
        {
            MenuClass open = mM.GetOpenMenu(true);
            if (open != null && open.title != null)
                menuTitle = open.title.ToLowerInvariant();
        }

        for (int i = 0; i < field.parts.Count; i++)
        {
            Part p = new Part(field.parts[i]);
            if (p.prefab == null || p.spawned != null)
                continue;

            bool isHazard = p.isHazard
                || (p.type != null && (p.type.Equals("Hazard", System.StringComparison.OrdinalIgnoreCase)
                    || p.type.Equals("Hazards", System.StringComparison.OrdinalIgnoreCase)));

            // Skip hazard templates entirely when hazards are off
            if (isHazard && !GameSettings.HazardsEnabled && menuTitle != "createmode")
                continue;

            GameObject go = Instantiate(p.prefab);
            go.transform.parent = transform;

            field.parts[i].spawned = go;
            // Keep flags on the live PartInfo copy
            p.isHazard = field.parts[i].isHazard || isHazard;
            p.tangible = field.parts[i].tangible;
            p.spawnRange = field.parts[i].spawnRange;
            p.spawned = go;

            go.transform.localScale = p.size;
            go.transform.localPosition = p.position;
            go.transform.localEulerAngles = p.rotation;

            Renderer pRen = go.GetComponent<Renderer>();
            if (pRen != null)
                FieldTextureFactory.ApplyAlbedo(pRen, p.material, p.material_Color, p.material_Matallic, p.material_Smoothness);

            string typeLower = p.type != null ? p.type.ToLowerInvariant().Trim() : "";
            if (menuTitle != "createmode" && typeLower == "spawn")
                go.SetActive(false);

            // Hazards stay dormant until FieldHazardSpawner places them
            if (isHazard)
                go.SetActive(false);

            PartInfo pI = go.GetComponent<PartInfo>();
            if (pI == null)
                pI = go.AddComponent<PartInfo>();
            pI.part = p;

            // Sync authored flags onto field.parts[i] for the spawner
            field.parts[i].isHazard = p.isHazard;
            field.parts[i].tangible = p.tangible;
            field.parts[i].spawnRange = p.spawnRange;

            AttachEffectBehaviours(go, p);
        }
    }

    void AttachEffectBehaviours(GameObject go, Part p)
    {
        if (go == null || p == null)
            return;
        string n = p.name != null ? p.name.ToLowerInvariant() : "";
        if (n.Contains("water") || n.Contains("tide") || n.Contains("ocean"))
        {
            if (go.GetComponent<AnimatedWater>() == null)
                go.AddComponent<AnimatedWater>();
        }
        else if (n.Contains("paver") || n.Contains("courtyard") || n.Contains("nuoryn"))
        {
            if (go.GetComponent<CourtyardPavers>() == null)
                go.AddComponent<CourtyardPavers>();
        }
    }

    public void SaveField()
    {
        field.parts.Clear();

        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                PartInfo pI = transform.GetChild(i).GetComponent<PartInfo>();
                if (pI == null || pI.part == null)
                    continue;

                Part p = new Part(pI.part);
                p.position = pI.transform.localPosition;
                p.rotation = pI.transform.localRotation.eulerAngles;
                p.size = pI.transform.localScale;
                p.spawned = null;
                field.parts.Add(p);
            }
        }
    }
}
