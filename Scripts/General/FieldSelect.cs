using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldSelect : MonoBehaviour
{
    public static FieldSelect instance;
    Database db;

    public List<FieldGrab> fieldGrabs = new List<FieldGrab>();

    public Transform grabHolder;
    public GameObject fieldGrab_pf;

    public int perPage = 45;
    int page;
    int maxpage;

    public Text pageDisplay;

    public RawImage portrait;
    public Text portraitName;
    public Text portraitInfo;

    public Transform pageButtonsHolder;

    int lastShownField = -999;

    private void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Start()
    {
        db = Database.instance;
        ResolveRefs();
    }

    void OnEnable()
    {
        if (db == null)
            db = Database.instance;
        ResolveRefs();
        SpawnGrabs();
        UpdateSelectedPortrait();
    }

    void OnDisable()
    {
        ClearGrabs(false);
    }

    void Update()
    {
        if (db == null)
            db = Database.instance;

        if (lastShownField != db.selectedField)
            UpdateSelectedPortrait();
    }

    void ResolveRefs()
    {
        if (grabHolder == null)
        {
            Transform t = FindChildNamed(transform, "Field Select");
            if (t != null && t != transform)
                grabHolder = t;
            else if (transform.childCount > 1)
                grabHolder = transform.GetChild(1);
        }

        if (pageButtonsHolder == null)
            pageButtonsHolder = FindChildNamed(transform, "PageButtons");

        if (pageDisplay == null && pageButtonsHolder != null)
        {
            Transform pageNum = FindChildNamed(pageButtonsHolder, "Page Number");
            if (pageNum == null)
                pageNum = FindChildNamed(pageButtonsHolder, "PageDisplay");
            if (pageNum != null)
                pageDisplay = pageNum.GetComponent<Text>();
        }

        if (portrait == null)
        {
            Transform t = FindChildNamed(transform, "Portrait");
            if (t != null)
                portrait = t.GetComponent<RawImage>();
        }

        if (portrait != null)
        {
            if (portraitName == null)
            {
                Transform n = FindChildNamed(portrait.transform, "Name");
                if (n != null)
                    portraitName = n.GetComponent<Text>();
            }

            if (portraitInfo == null)
            {
                Transform info = FindChildNamed(portrait.transform, "Info");
                if (info != null)
                    portraitInfo = info.GetComponent<Text>();
            }
        }
    }

    Transform FindChildNamed(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindChildNamed(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    public void PageChange(int d)
    {
        page += d;
        if (page > maxpage) page = maxpage;
        if (page < 0) page = 0;
        SpawnGrabs();
    }

    void ClearGrabs(bool immediate)
    {
        fieldGrabs.Clear();

        if (grabHolder == null)
            return;

        for (int i = grabHolder.childCount - 1; i >= 0; i--)
        {
            GameObject go = grabHolder.GetChild(i).gameObject;
            if (immediate)
                DestroyImmediate(go);
            else
                Destroy(go);
        }
    }

    void SpawnGrabs()
    {
        if (db == null || db.fields == null || fieldGrab_pf == null || grabHolder == null)
        {
            if (fieldGrab_pf == null)
                Debug.LogWarning("FieldSelect: assign fieldGrab_pf (Field Grab prefab).");
            return;
        }

        ClearGrabs(true);

        int total = db.fields.Count;
        int countPerPage = Mathf.Max(1, perPage);
        maxpage = Mathf.Max(0, Mathf.FloorToInt((total - 1) / (float)countPerPage));
        if (page > maxpage) page = maxpage;
        if (page < 0) page = 0;

        int start = page * countPerPage;
        int end = Mathf.Min(start + countPerPage, total);

        for (int i = start; i < end; i++)
        {
            Field f = db.fields[i];
            if (f == null) continue;

            GameObject go = Instantiate(fieldGrab_pf, grabHolder);
            go.name = "Field Grab";
            FieldGrab fg = go.GetComponent<FieldGrab>();
            if (fg == null)
                continue;
            fg.Bind(f, i);
            fieldGrabs.Add(fg);
        }

        if (pageDisplay != null)
            pageDisplay.text = "Page " + (page + 1) + " of " + (maxpage + 1);

        UpdateSelectedPortrait();
    }

    public void RefreshAllSelectionLooks()
    {
        for (int i = 0; i < fieldGrabs.Count; i++)
        {
            if (fieldGrabs[i] != null)
                fieldGrabs[i].RefreshSelectedLook();
        }
    }

    public void UpdateSelectedPortrait()
    {
        if (db == null)
            db = Database.instance;
        if (db == null || db.fields == null)
            return;

        if (portrait == null)
            ResolveRefs();

        lastShownField = db.selectedField;

        if (db.selectedField < 0 || db.selectedField >= db.fields.Count)
        {
            if (portrait != null)
                portrait.color = new Color(1f, 1f, 1f, 0.35f);
            if (portraitName != null)
                portraitName.text = "Random";
            if (portraitInfo != null)
                portraitInfo.text = "";
            return;
        }

        Field f = db.fields[db.selectedField];
        if (f == null)
            return;

        if (portrait != null)
        {
            if (f.portrait != null)
                portrait.texture = f.portrait;
            portrait.color = Color.white;
        }

        if (portraitName != null)
        {
            portraitName.text = string.IsNullOrEmpty(f.name) ? "Field" : f.name;
            portraitName.color = Color.white;
        }

        if (portraitInfo != null)
        {
            string author = string.IsNullOrEmpty(f.arthur) ? "Unknown" : f.arthur;
            portraitInfo.text = "Size: " + f.size + "    Aurthur: " + author;
            portraitInfo.color = new Color(1f, 0.93f, 0f, 1f);
        }
    }
}
