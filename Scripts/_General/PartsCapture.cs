using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartsCapture : MonoBehaviour
{
    Database db;
    public bool saving = false;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && !saving)
        {
            saving = true;
            StartCoroutine(LoadPartsAndSave());
        }
    }

    IEnumerator LoadPartsAndSave()
    {
        int cC = transform.childCount;

        if(cC > 0)
        {
            for(int i = 0; i < cC; i++)
            {
                PartInfo pi = transform.GetChild(i).GetComponent<PartInfo>();

                if (pi != null)
                {
                    if (pi.part.uid == null || pi.part.uid == "")
                    {
                        pi.part.CreateUID();
                    }
                    yield return null;

                    if(db.parts.Count > 0)
                    {
                        if(!db.parts.Exists(x=> x.name == pi.part.name && x.type == pi.part.type))
                        {
                            db.parts.Add(new Part(pi.part.ToString()));
                        }
                    }
                    else
                    {
                        db.parts.Add(new Part(pi.part.ToString()));
                    }

                    yield return null;
                }
            }

            db.StartSaveData();

            yield return new WaitUntil(() => !db.IsSaving());
        }

        saving = false;
        yield return null;
    }
}
