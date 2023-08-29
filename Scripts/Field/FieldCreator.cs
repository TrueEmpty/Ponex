using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FieldCreator : MonoBehaviour
{
    Database db;
    MessageDisplay mD;

    public FieldInfo fI;

    public Transform fieldHolder;
    public GameObject fieldObj;

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

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        mD = MessageDisplay.instance;

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

        //Change Field Pos
        if (fI != null)
        {
            Vector3 pos = Vector3.zero;
            pos.z = size;
            fI.transform.localPosition = pos;
        }

        int fCC = fI.transform.childCount;
        if(fCC > 0)
        {
            float sizeOut = Mathf.Lerp(1, 3.325f, ((float)(size - 10) / 90));

            for (int i = 0; i < fCC; i++)
            {
                PartInfo pI = fI.transform.GetChild(i).gameObject.GetComponent<PartInfo>();

                if(pI != null)
                {
                    if(pI.part.scaling)
                    {
                        Vector3 scale = pI.part.size;
                        if (pI.part.type == "Border")
                        {
                            //scale.x = pI.part.size.x * sizeOut;
                            scale.y = pI.part.size.y * sizeOut;
                            scale.z = pI.part.size.z * sizeOut;
                        }
                        else
                        {
                            scale.x = pI.part.size.x * sizeOut;
                            scale.y = pI.part.size.y * sizeOut;
                            //scale.z = pI.part.size.z * sizeOut;
                        }
                        pI.transform.localScale = scale;
                    }

                    if(!pI.part.canMove)
                    {
                        Vector3 np = pI.transform.localPosition;

                        np.x = pI.part.position.x * sizeOut;
                        np.y = pI.part.position.y * sizeOut;
                        np.z = pI.part.position.z * sizeOut;

                        pI.transform.localPosition = np;
                    }
                }
            }
        }
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

        if(fI != null)
        {
            fI.field.uid = db.fields[0].uid;
            fI.field.name = db.fields[0].name;
            fI.field.arthur = db.fields[0].arthur;

            nameText.text = fI.field.name;
            creatorText.text = fI.field.arthur;
        }
    }

    // Update is called once per frame
    void Update()
    {
        AddContents();
        SelectingPart();
        UpdatePartsOptions();
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
        }

        if(selected_Part != null)
        {
            PartMovement();
        }
    }

    public void PartMovement()
    {
        PartInfo pI = selected_Part.transform.gameObject.GetComponent<PartInfo>();

        if(pI.part.canMove)
        {
            RaycastHit hit;
            Vector3 sp = selected_Part.position;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 3))
            {
                sp = hit.point - offsetPoint;
            }

            sp.z = zPos;

            selected_Part.position = sp;
        }

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
            float sizeOut = Mathf.Lerp(1, 3.325f, ((float)(size - 10) / 90));
            Vector3 sp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            sp.z = fI.transform.localPosition.z;
            zPos = sp.z;

            if(p.setPos)
            {
                sp = p.position;
                sp.x *= sizeOut;
                sp.y *= sizeOut;
            }

            GameObject sP = Instantiate(p.prefab);

            sP.transform.parent = fI.transform;
            sP.transform.localPosition = sp;
            sP.transform.localEulerAngles = p.rotation;

            if(p.scaling)
            {
                sP.transform.localScale = p.size * (size / 10);
            }
            else
            {
                sP.transform.localScale = p.size;
            }

            sP.GetComponent<PartInfo>().part = p;
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
        if(fI != null)
        {
            fI.DestroyField();
        }

        //Add Field
        GameObject sF = Instantiate(fieldObj);
        sF.transform.parent = fieldHolder;

        fI = sF.GetComponent<FieldInfo>();

        //Change Field Pos
        if (fI != null)
        {
            Vector3 pos = Vector3.zero;
            pos.z = size;
            fI.transform.localPosition = pos;
        }

        fI.transform.localScale = Vector3.one;
    }

    void WriteMessage(string message,float time = -1)
    {
        mD.Write(message,time);
    }
}
