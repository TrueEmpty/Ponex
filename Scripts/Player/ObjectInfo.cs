using UnityEngine;

[System.Serializable]
public class ObjectInfo
{
    public GameObject prefabs;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    public ObjectInfo()
    {

    }

    public ObjectInfo(string fromString)
    {
        FromString(fromString);
    }

    public override string ToString()
    {
        string result = "";

        result += prefabs.name;
        result += "|";

        result += positionOffset.x.ToString();
        result += "|";
        result += positionOffset.y.ToString();
        result += "|";
        result += positionOffset.z.ToString();
        result += "|";

        result += rotationOffset.x.ToString();
        result += "|";
        result += rotationOffset.y.ToString();
        result += "|";
        result += rotationOffset.z.ToString();
        result += "|";

        return result;
    }

    public void FromString(string str)
    {
        int sai = 0;
        string[] sA = str.Split('|');

        if (sA[sai] != null && sA[sai] != "")
        {
            prefabs = (GameObject)Resources.Load("Parts/" + "/" + sA[sai]);
        }
        sai++;

        positionOffset.x = float.Parse(sA[sai]);
        sai++;
        positionOffset.y = float.Parse(sA[sai]);
        sai++;
        positionOffset.z = float.Parse(sA[sai]);
        sai++;

        rotationOffset.x = float.Parse(sA[sai]);
        sai++;
        rotationOffset.y = float.Parse(sA[sai]);
        sai++;
        rotationOffset.z = float.Parse(sA[sai]);
        sai++;
    }
}
