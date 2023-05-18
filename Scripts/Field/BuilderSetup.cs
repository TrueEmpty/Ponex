using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuilderSetup : MonoBehaviour
{
    Database db;
    public MenuControl mC;

    int screen = 0;
    public GameObject buildType;
    public GameObject fieldSelect;
    public GameObject fieldCreator;
    public Transform createField;

    public int fieldSelected = -1;
    public Text fieldName;
    public Text fieldArthur;

    public GameObject createFieldSelector;
    public Transform createFieldContent;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        buildType.SetActive(screen == 0);
        fieldSelect.SetActive(screen == 1);
    }

    public void Back()
    {
        if (screen == 0)
        {
            mC.BackMenu();
        }
        else
        {
            screen--;
        }
    }

    public void LoadField(bool blankTemplate = false)
    {
        if(blankTemplate)
        {
            //Spawn Field Creator with nothing added
            CreateStartup();
        }
        else
        {
            //Go to a screen to load up all the avaliable fields
            SpawnFieldContent();
            screen = 1;
        }
    }

    void SpawnFieldContent()
    {
        //Clear old content
        if(createFieldContent.childCount > 0)
        {
            for(int i = createFieldContent.childCount - 1; i >= 0; i--)
            {
                Destroy(createFieldContent.GetChild(i).gameObject);
            }
        }

        //Add in content
        if(db.fields.Count > 0)
        {
            for(int i = 0; i < db.fields.Count; i++)
            {
                Field fi = db.fields[i];

                GameObject go = Instantiate(createFieldSelector, createFieldContent) as GameObject;
                CreateFieldSelector cfS = go.GetComponent<CreateFieldSelector>();

                if (cfS != null)
                {
                    cfS.Setup(this,i,fi.name,fi.arthur,fi.icon);
                }
            }
        }
    }

    public void SelectField(int fS,string name,string arthur)
    {
        fieldSelected = fS;
        fieldName.text = "Name: " + name; 
        fieldArthur.text = "Creator: " + arthur; 
    }

    void CreateStartup(bool copyOfField = false)
    {
        mC.OpenMenu("None");

        GameObject go = Instantiate(fieldCreator, createField) as GameObject;

        FieldCreator fC = go.GetComponent<FieldCreator>();

        if(fC != null)
        {
            Field fe = null;

            if(fieldSelected >= 0 && fieldSelected < db.fields.Count)
            {
                fe = new Field(db.fields[fieldSelected]);
            }

            if(fe != null && copyOfField)
            {
                fe.CreateUID();
            }

            fC.LoadField(fe);
        }
    }
}
