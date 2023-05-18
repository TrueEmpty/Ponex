using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FieldCreator : MonoBehaviour
{
    Database db;
    public FieldInfo fI;

    public Transform fieldHolder;
    public Transform fieldObj;

    public GameObject typeSelector;
    string typeSelected = "";
    string typeShown = "";

    public GameObject partSelector;

    public InputField nameText;
    public Text sizeText;
    public int size = 10;
    public InputField creatorText;

    public Transform contentHolder;
    
    HashSet<char> accepted_ANS = new HashSet<char>(){
    'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z'
    ,'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
    ,'0','1','2','3','4','5','6','7','8','9',' '};

    public Transform selected_Part;
    public float zPos = 0;
    public Vector3 offsetPoint = Vector3.zero;
    public bool lockPoint = false;

    public GameObject parts_options;
    public Text partsNameDisplay;
    public RawImage partsImageDisplay;

    public bool overrideAdminField = false;
    public Text messageGO;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;

        Invoke("RefreshTypes", 1);
    }

    public void AlphanumericAndSpace(InputField input)
    {
        string result = "";
     
        if (input.text.Length > 0)
        {
            char[] cA = input.text.ToCharArray();

            if(cA.Length > 0)
            {
                for(int i = 0; i < cA.Length; i++)
                {
                    if(accepted_ANS.Contains(cA[i]))
                    {
                        result += cA[i];
                    }
                    else
                    {
                        input.caretPosition -= 1;
                    }
                }
            }
        }

        input.text = result;
    }


    public void UpdateSize(Slider slider)
    {
        size = Mathf.RoundToInt(slider.value);
        sizeText.text = "Size (" + size + ")";

        //Change Camera Pos

    }

    public void LoadField(Field fi)
    {
        //Clear old field
        Clear();

        //Add Field parts
        fI.field = fi;
        fI.StartFieldSpawn();
    }

    public void SaveField()
    {
        Field fe = fI.field;

        if(fe.uid == null || fe.uid == "")
        {
            fe.CreateUID();
        }

        Transform fT = fI.transform;

        fe.parts.Clear();
        if(fT.childCount > 0)
        {
            for (int i = 0; i < fT.childCount; i++)
            {
                Transform cT = fT.GetChild(i);

                PartInfo pI = cT.gameObject.GetComponent<PartInfo>();

                if (pI != null)
                {
                    Part pa = pI.part;

                    if (pa != null)
                    {
                        pa.CreateUID();
                        pa.position = cT.localPosition;
                        pa.rotation = cT.localEulerAngles;
                        pa.size = cT.localScale;

                        fe.parts.Add(pa);
                    }
                }
            }
        }

        fe.name = nameText.text.Trim();

        string cTt = creatorText.text.Trim();
        if (cTt == "True Empty" && overrideAdminField)
        {
            fe.arthur = cTt;
        }
        else if(cTt == "True Empty")
        {
            cTt = "Not True Empty";
            fe.arthur = cTt;
        }
        else
        {
            fe.arthur = cTt;
        }

        fe.size = size;

        //Take A Pic

        //Save
        int ind = db.fields.FindIndex(x => x.uid == fe.uid);

        if (ind >= 0 && ind < db.fields.Count)
        {
            db.fields.RemoveAt(ind);
        }

        db.fields.Add(fe);

        StartCoroutine(FinishSave());
    }

    IEnumerator FinishSave()
    {
        WriteMessage("Saving Field");
        yield return new WaitForSeconds(.3f);

        db.StartSaveData();
        yield return new WaitUntil(() => !db.IsSaving());
        WriteMessage("Field Saved",1.5f);

    }

    void RefreshTypes()
    {
        typeShown = "BlaBlaBla";

        fI.field.uid = db.fields[0].uid;
        fI.field.name = db.fields[0].name;
        fI.field.arthur = db.fields[0].arthur;

        nameText.text = fI.field.name;
        creatorText.text = fI.field.arthur;
    }

    // Update is called once per frame
    void Update()
    {
        AddContents();
        SelectingPart();
        UpdatePartsOptions();

        messageGO.gameObject.SetActive(messageGO.text != "" && messageGO.text != null);
    }

    public void UpdatePartsOptions()
    {
        if(selected_Part != null)
        {
            PartInfo pI = selected_Part.GetComponent<PartInfo>();

            parts_options.SetActive(pI != null);

            if (pI != null)
            {
                partsNameDisplay.text = pI.part.name;

                if(pI.part.icon != null)
                {
                    partsImageDisplay.texture = pI.part.icon;
                }
            }
        }
        else
        {
            parts_options.SetActive(false);
        }
    }

    public void SelectingPart()
    {
        if(Input.GetMouseButton(0))
        {
            if(selected_Part == null)
            {
                RaycastHit hit;

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000))
                {
                    PartInfo pI = hit.transform.gameObject.GetComponent<PartInfo>();

                    if (pI != null)
                    {
                        selected_Part = hit.transform;
                        zPos = hit.transform.position.z;
                        Vector3 usePoint = hit.point;
                        usePoint.z = zPos;
                        offsetPoint = usePoint - selected_Part.transform.position;
                    }
                }
            }
            else
            {
                PartMovement();
            }
        }
        else
        {
            selected_Part = null;
        }
    }

    public void PartMovement()
    {
        RaycastHit hit;
        Vector3 sp = selected_Part.position;

        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),out hit,1000, 3))
        {
            sp = hit.point - offsetPoint;
        }

        sp.z = zPos;

        selected_Part.position = sp;

        if(Input.GetMouseButtonDown(1))
        {
            selected_Part = null;
        }
    }

    public void SetType(string type_Name)
    {
        typeSelected = type_Name;
    }

    public void AddContents()
    {
        if (typeSelected != typeShown)
        {
            ClearContents();

            if (typeSelected == "")
            {
                if (db.typeIds.Count > 0)
                {
                    for (int i = 0; i < db.typeIds.Count; i++)
                    {
                        GameObject go = Instantiate(typeSelector, contentHolder) as GameObject;

                        TypeSelector ts = go.GetComponent<TypeSelector>();

                        if (ts != null)
                        {
                            ts.SetupType(this, db.typeIds[i], db.typeIds[i], db.typeIds[i].tcolor);
                        }
                    }
                }
            }
            else if (typeSelected == "Components")
            {
                //Setup Back button
                GameObject go = Instantiate(typeSelector, contentHolder) as GameObject;

                TypeSelector ts = go.GetComponent<TypeSelector>();

                if (ts != null)
                {
                    ts.SetupType(this, "", Color.red, Color.white);
                }

                //Add Script Selectors
            }
            else
            {
                //Setup Back button
                GameObject go = Instantiate(typeSelector, contentHolder) as GameObject;

                TypeSelector ts = go.GetComponent<TypeSelector>();

                if (ts != null)
                {
                    ts.SetupType(this, "", Color.red, Color.white);
                }

                //Add Part Selectors
                List<Part> type_Parts = db.parts.FindAll(x => x.type.ToLower().Trim() == typeSelected.ToLower().Trim());

                TypeIdentifier tI = db.typeIds.Find(x=> x.type.ToLower().Trim() == typeSelected.ToLower().Trim());
                
                if(tI == null)
                {
                    tI = new TypeIdentifier(typeSelected);
                }

                if(type_Parts.Count > 0)
                {
                    for (int i = 0; i < type_Parts.Count; i++)
                    {
                        GameObject po = Instantiate(partSelector, contentHolder) as GameObject;

                        PartSelector ps = po.GetComponent<PartSelector>();

                        if (ps != null)
                        {
                            ps.Setup(this, type_Parts[i], tI, tI.tcolor);
                        }
                    }
                }
            }

            typeShown = typeSelected;
        }
    }

    public void SpawnPart(Part p)
    {
        if(p.prefab != null)
        {
            Vector3 sp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            sp.z = fI.transform.position.z;
            zPos = sp.z;

            Instantiate(p.prefab,sp,Quaternion.identity,fI.transform);
        }
    }

    public void ClearContents()
    {
        if(contentHolder.childCount > 0)
        {
            for(int i = contentHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(contentHolder.GetChild(i).gameObject);
            }
        }
    }

    void Clear()
    {
        fI.DestroyField();
    }

    void WriteMessage(string message,float time = -1)
    {
        messageGO.text = message;

        if (time > 0)
        {
            Invoke("ClearMessage", time);
        }
    }

    void ClearMessage()
    {
        messageGO.text = "";
    }
}
