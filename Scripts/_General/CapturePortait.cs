using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CapturePortait : MonoBehaviour
{
    Database db;
    Camera c;
    public GameObject controls;
    public GameObject powerLevel;
    public GameObject holder;

    public int resWidth = 2550;
    public int resHeight = 3300;

    private bool takeHiResShot = false;

    public int character = 0;
    public int part = 0;
    public int field = 0;
    
    bool controlShowing = false;

    public Text chIcName;
    public Text chPorName;

    public Showing showing = Showing.Characters;
    public Text showText;

    public Vector4 iconSize = Vector4.one;
    float xOffset = 1f;

    Vector3 max = Vector3.zero; //Max (Health,Movement Speed, Bump Speed)
    
    [Space()]
    public Text healthAmount;
    public RectTransform healthRT;

    public Text movementAmount;
    public RectTransform movementRT;

    public Text bumpAmount;
    public RectTransform bumpRT;

    public Text abilityName;
    public Text abilityDescription;

    public enum Showing
    {
        Characters,
        Parts,
        Fields
    }

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        c = Camera.main;

        resWidth = Screen.width;
        resHeight = Screen.height;

        float xLow = Mathf.FloorToInt(resWidth * .2318026f);
        float xHigh = Mathf.FloorToInt(resWidth * .7681974f);

        float yLow = Mathf.FloorToInt(resHeight * .2886751f);
        float yHigh = Mathf.FloorToInt(resHeight * 1);

        iconSize = new Vector4(xLow,yLow,xHigh-xLow,yHigh-yLow);

        controls.SetActive(controlShowing);

        //Get Maxes
        if (db.characters.Count > 0)
        {
            for(int i = 0; i < db.characters.Count; i++)
            {
                if(db.characters[i].MaxHealth > max.x)
                {
                    max.x = db.characters[i].MaxHealth;
                }

                if (db.characters[i].Speed > max.y)
                {
                    max.y = db.characters[i].Speed;
                }

                if (db.characters[i].Bump > max.z)
                {
                    max.z = db.characters[i].Bump;
                }
            }
        }

        UpdateField();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            if (!takeHiResShot)
            {
                takeHiResShot = true;
                StartCoroutine(TakePhotos());
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            controlShowing = !controlShowing;
            controls.SetActive(controlShowing);            
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeShowing();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            NextCharacter();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            PrevCharacter();
        }
    }

    public void ChangeShowing()
    {
        switch(showing)
        {
            case Showing.Characters:
                showing = Showing.Parts;
                break;
            case Showing.Parts:
                showing = Showing.Fields;
                break;
            case Showing.Fields:
                showing = Showing.Characters;
                break;
        }

        UpdateShowText();
    }

    void UpdateShowText()
    {
        switch (showing)
        {
            case Showing.Characters:
                showText.text = "Showing Characters" + "\n" + "(C)";
                break;
            case Showing.Parts:
                showText.text = "Showing Parts" + "\n" + "(C)";
                break;
            case Showing.Fields:
                showText.text = "Showing Fields" + "\n" + "(C)";
                break;
        }

        UpdateField();
    }

    IEnumerator TakePhotos()
    {
        switch(showing)
        {
            case Showing.Characters:
                if (character >= 0 && character < db.characters.Count)
                {
                    Stats ch = db.characters[character];

                    if (ch.Name != null && ch.Name != "")
                    {
                        if (takeHiResShot)
                        {
                            //Setting up Icon
                            if (controls != null)
                            {
                                controlShowing = controls.activeInHierarchy;
                                controls.SetActive(false);
                                chIcName.gameObject.SetActive(true);
                                chPorName.gameObject.SetActive(false);
                                powerLevel.SetActive(false);
                            }

                            yield return new WaitForEndOfFrame();
                            //Shooting Icon
                            RenderTexture rt = new RenderTexture(resWidth, resHeight, 32);
                            c.targetTexture = rt;
                            Texture2D screenShot = new Texture2D((int)iconSize.z, (int)iconSize.w, TextureFormat.RGB24, false);
                            c.Render();
                            RenderTexture.active = rt;
                            screenShot.ReadPixels(new Rect((int)iconSize.x, (int)iconSize.y, (int)iconSize.z, (int)iconSize.w), 0, 0);
                            c.targetTexture = null;
                            RenderTexture.active = null; // JC: added to avoid errors
                            Destroy(rt);
                            byte[] bytes = screenShot.EncodeToPNG();
                            string filename = ScreenShotName("Icons", ch.Name, 1, "Characters");
                            System.IO.File.WriteAllBytes(filename, bytes);
                            Debug.Log(string.Format("Took icon screenshot to: {0}", filename));

                            //Setting up Portrait
                            yield return new WaitForEndOfFrame();
                            chIcName.gameObject.SetActive(false);
                            chPorName.gameObject.SetActive(true);
                            powerLevel.SetActive(true);

                            if (holder.transform.childCount > 0)
                            {
                                for (int i = 0; i < holder.transform.childCount; i++)
                                {
                                    Transform tr = holder.transform.GetChild(i).transform;

                                    tr.position += (Vector3.right * xOffset);
                                }
                            }

                            yield return new WaitForEndOfFrame();
                            //Shooting Portrait
                            RenderTexture por_rt = new RenderTexture(resWidth, resHeight, 32);
                            c.targetTexture = por_rt;
                            Texture2D por_screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
                            c.Render();
                            RenderTexture.active = por_rt;
                            por_screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
                            c.targetTexture = null;
                            RenderTexture.active = null; // JC: added to avoid errors
                            Destroy(por_rt);
                            byte[] por_bytes = por_screenShot.EncodeToPNG();
                            string por_filename = ScreenShotName("Portraits", ch.Name, 1, "Characters");
                            System.IO.File.WriteAllBytes(por_filename, por_bytes);
                            Debug.Log(string.Format("Took Portrait screenshot to: {0}", por_filename));
                            yield return new WaitForEndOfFrame();

                            //Undo All
                            if (holder.transform.childCount > 0)
                            {
                                for (int i = 0; i < holder.transform.childCount; i++)
                                {
                                    Transform tr = holder.transform.GetChild(i).transform;

                                    tr.position -= (Vector3.right * xOffset);
                                }
                            }

                            chIcName.gameObject.SetActive(true);
                            chPorName.gameObject.SetActive(false);
                            powerLevel.SetActive(false);

                            if (controlShowing)
                            {
                                controls.SetActive(true);
                            }
                        }
                    }
                }
                break;
            case Showing.Parts:
                break;
            case Showing.Fields:
                break;
        }

        takeHiResShot = false;
        yield return new WaitForEndOfFrame();
    }


    public static string ScreenShotName(string type, string nameOfch, int number = 1, string classification = "Characters")
    {
        return string.Format("{0}/Resources/{1}/{4}/{2}_{3}.png",
                             Application.dataPath,
                             type, nameOfch, number, classification);
    }

    public void ClearHolder()
    {
        //Remove Old Field Items
        if (holder.transform.childCount > 0)
        {
            for (int i = holder.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(holder.transform.GetChild(i).gameObject);
            }
        }

        chIcName.text = "";
        chPorName.text = "";
    }

    public void UpdateField()
    {
        ClearHolder();

        switch(showing)
        {
            case Showing.Characters:
                if (character >= 0 && character < db.characters.Count)
                {
                    Stats ch = db.characters[character];
                    Transform hT = holder.transform;
                    Vector3 hP = holder.transform.position;

                    //Add New Field Items
                    if (ch.character != null)
                    {
                        ObjectInfo oI = ch.character;

                        if (oI.prefabs != null)
                        {
                            Vector3 naturalOffset = new Vector3(-.89f, -14.35f, -10.38f);
                            Vector3 useRotation = naturalOffset + oI.rotationOffset;

                            GameObject go = Instantiate(oI.prefabs, hP, Quaternion.Euler(useRotation));
                            go.transform.parent = hT;
                            go.transform.localScale *= .25f;

                            PlayerInfo pI = go.GetComponent<PlayerInfo>();

                            if(pI != null)
                            {
                                pI.player.selection = true;
                            }
                        }
                    }

                    chIcName.text = ch.Name;
                    chPorName.text = "";
                    char[] cA = ch.Name.ToCharArray();
                    if (cA.Length > 0)
                    {
                        for (int i = 0; i < cA.Length; i++)
                        {
                            chPorName.text += cA[i];

                            if (i < cA.Length - 1)
                            {
                                chPorName.text += "\n";
                            }
                        }
                    }

                    healthAmount.text = ch.MaxHealth.ToString();
                    healthRT.sizeDelta = new Vector2((250 * (ch.MaxHealth / max.x)), healthRT.sizeDelta.y);

                    movementAmount.text = ch.Speed.ToString();
                    movementRT.sizeDelta = new Vector2((250 * (ch.Speed / max.y)), movementRT.sizeDelta.y);

                    bumpAmount.text = ch.Bump.ToString();
                    bumpRT.sizeDelta = new Vector2((250 * (ch.Bump / max.z)), bumpRT.sizeDelta.y);

                    abilityName.text = ch.superName;
                    abilityDescription.text = ch.superDescription;

                    c.backgroundColor = ch.portraitColor;
                }
                break;
            case Showing.Parts:
                if (part >= 0 && part < db.parts.Count)
                {
                    Transform hT = holder.transform;
                    Vector3 hP = holder.transform.position;

                    //Add New Field Items
                    Part oI = db.parts[part];

                    if (oI.prefab != null)
                    {
                        Vector3 naturalOffset = new Vector3(-.89f, -14.55f, -10.38f);
                        Vector3 useRotation = naturalOffset + Vector3.zero;

                        Instantiate(oI.prefab, hP, Quaternion.Euler(useRotation), hT);
                    }

                    chIcName.text = oI.name;
                    chPorName.text = "";
                    char[] cA = oI.name.ToCharArray();
                    if (cA.Length > 0)
                    {
                        for (int i = 0; i < cA.Length; i++)
                        {
                            chPorName.text += cA[i];

                            if (i < cA.Length - 1)
                            {
                                chPorName.text += "\n";
                            }
                        }
                    }

                    c.backgroundColor = GetTypeColor(oI.type);
                }
                break;
            case Showing.Fields:
                if (field >= 0 && field < db.fields.Count)
                {
                    Transform hT = holder.transform;
                    Vector3 hP = holder.transform.position;

                    //Add New Field Items
                    Field oI = db.fields[field];

                    if (oI.parts.Count > 0)
                    {
                        //Spawn Field Parts
                        /*Vector3 naturalOffset = new Vector3(-.89f, -14.55f, -10.38f);
                        Vector3 useRotation = naturalOffset + Vector3.zero;

                        Instantiate(oI.prefab, hP, Quaternion.Euler(useRotation), hT);*/
                    }

                    chIcName.text = oI.name;
                    chPorName.text = "";
                    char[] cA = oI.name.ToCharArray();
                    if (cA.Length > 0)
                    {
                        for (int i = 0; i < cA.Length; i++)
                        {
                            chPorName.text += cA[i];

                            if (i < cA.Length - 1)
                            {
                                chPorName.text += "\n";
                            }
                        }
                    }

                    c.backgroundColor = GetTypeColor("");
                }
                break;
        }
    }

    public Color GetTypeColor(string type)
    {
        Color result = Color.clear;

        switch(type)
        {
            case "":
                result = Color.clear;
                break;
            default:
                result = Color.clear;
                break;
        }

        return result;
    }

    public void TakeHiResShot()
    {
        if (!takeHiResShot)
        {
            takeHiResShot = true;
            StartCoroutine(TakePhotos());
        }
    }

    public void NextCharacter()
    {
        switch(showing)
        {
            case Showing.Characters:
                if (db.characters.Count > 1)
                {
                    character++;

                    if (character >= db.characters.Count)
                    {
                        character = 0;
                    }

                    UpdateField();
                }
                break;
            case Showing.Parts:
                if (db.parts.Count > 1)
                {
                    part++;

                    if (part >= db.parts.Count)
                    {
                        part = 0;
                    }

                    UpdateField();
                }
                break;
            case Showing.Fields:
                if (db.fields.Count > 1)
                {
                    field++;

                    if (field >= db.fields.Count)
                    {
                        field = 0;
                    }

                    UpdateField();
                }
                break;
        }
    }

    public void PrevCharacter()
    {
        switch (showing)
        {
            case Showing.Characters:
                if (db.characters.Count > 1)
                {
                    character--;

                    if (character < 0)
                    {
                        character = db.characters.Count - 1;
                    }

                    UpdateField();
                }
                break;
            case Showing.Parts:
                if (db.parts.Count > 1)
                {
                    part--;

                    if (part < 0)
                    {
                        part = db.parts.Count - 1;
                    }

                    UpdateField();
                }
                break;
            case Showing.Fields:
                if (db.fields.Count > 1)
                {
                    field--;

                    if (field < 0)
                    {
                        field = db.fields.Count - 1;
                    }

                    UpdateField();
                }
                break;
        }
    }
}
