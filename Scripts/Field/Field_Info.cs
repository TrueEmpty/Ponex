using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field_Info : MonoBehaviour
{
    public static Field_Info instance;

    public Field field;
    Database db;
    MenuManager mM;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        mM = MenuManager.instance;
        Preload();
        LoadField();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Preload()
    {
        int fs = field.size;

        //Move to correct spot
        Vector3 pos = transform.position;
        pos.z = fs + 10;
        transform.position = pos;

        //Add Out of Bounds
        for (int i = 0; i < 4; i++)
        {
            GameObject oob = Instantiate(db.outofBounds);
            oob.transform.parent = transform;

            Vector3 oobLoc = new Vector3(0, ((fs+10) / 2) + 1, 0);
            Vector3 oobScale = new Vector3((fs + 10) + 3, 1, 1);

            switch (i)
            {
                case 1://Right
                    oobLoc = new Vector3(((fs + 10) / 2) + 1, 0, 0);
                    oobScale = new Vector3(1, (fs + 10) + 3, 1);
                    break;
                case 2://Down
                    oobLoc = new Vector3(0, -(((fs + 10) / 2) + 1), 0);
                    oobScale = new Vector3((fs + 10) + 3, 1, 1);
                    break;
                case 3://Left
                    oobLoc = new Vector3(-(((fs + 10) / 2) + 1), 0, 0);
                    oobScale = new Vector3(1, (fs + 10) + 3, 1);
                    break;
            }

            oob.transform.localPosition = oobLoc;
            oob.transform.localScale = oobScale;
        }

        //Add Background
        GameObject background = Instantiate(db.background);
        background.transform.parent = transform;
        background.transform.localPosition = new Vector3(0, 0, .5f);

        Renderer bRen = background.GetComponent<Renderer>();

        if (bRen != null)
        {
            bRen.material.color = field.backgroundColor;
            bRen.material.SetFloat("_Metallic", field.backgroundMatallic);
            bRen.material.SetFloat("_Smoothness", field.backgroundSmoothness);

            if (field.backgroundMaterial != null)
            {
                bRen.material.shaderKeywords = new string[1] { "_NORMALMAP" };
                bRen.material.SetTexture("_NORMALMAP", field.backgroundMaterial);
            }
        }
    }

    public void LoadField()
    {
        //Add all Parts
        if (field.parts.Count > 0)
        {
            for(int i = 0; i < field.parts.Count; i++)
            {
                Part p = new Part(field.parts[i]);

                if (p.prefab != null && p.spawned == null)
                {
                    //Create Part
                    GameObject go = Instantiate(p.prefab);
                    go.transform.parent = transform;

                    //Set Spawned Go's
                    field.parts[i].spawned = go;
                    p.spawned = go;

                    //Set Scale
                    go.transform.localScale = p.size;
                    go.transform.localPosition = p.position;
                    go.transform.localEulerAngles = p.rotation;

                    //Add Material
                    Renderer pRen = go.GetComponent<Renderer>();

                    if (pRen != null)
                    {
                        pRen.material.color = p.material_Color;
                        pRen.material.SetFloat("_Metallic", p.material_Matallic);
                        pRen.material.SetFloat("_Smoothness", p.material_Smoothness);

                        if(p.material != null)
                        {
                            pRen.material.shaderKeywords = new string[1] { "_NORMALMAP"};
                            pRen.material.SetTexture("_NORMALMAP", p.material);
                        }
                    }

                    //Add Scripts
                    if (mM.GetOpenMenu(true).title.ToLower() != "createMode" && p.type.ToLower().Trim() == "spawn")
                    {
                        go.SetActive(false);
                    }

                    PartInfo pI = go.GetComponent<PartInfo>();

                    if (pI != null)
                    {
                        pI.part = p;
                    }                  
                }
            }
        }
    }

    public void SaveField()
    {
        field.parts.Clear();

        if(transform.childCount > 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                PartInfo pI = transform.GetChild(i).GetComponent<PartInfo>();

                if(pI != null)
                {
                    Part p = new(pI.part);

                    p.position = pI.transform.localPosition;
                    p.rotation = pI.transform.localRotation.eulerAngles;
                    p.size = pI.transform.localScale;

                    p.spawned = null;
                    field.parts.Add(p);
                }
            }
        }
    }
}
