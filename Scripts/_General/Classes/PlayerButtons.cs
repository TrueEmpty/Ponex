using UnityEngine;

[System.Serializable]
public class PlayerButtons
{
    [Tooltip("This should be the absolute Left not adjusted for rotation")]
    public KeyCode left = KeyCode.A;
    [Tooltip("This should be the absolute Right not adjusted for rotation")]
    public KeyCode right = KeyCode.D;
    [Tooltip("This should be the absolute Up not adjusted for rotation")]
    public KeyCode bump = KeyCode.W;
    [Tooltip("This should be the absolute Down not adjusted for rotation")]
    public KeyCode super = KeyCode.S;

    public string dirShown = "Up";
    public string dirDesc = "Up";

    public PlayerButtons()
    {

    }

    public PlayerButtons(KeyCode Left, KeyCode Right, KeyCode Bump, KeyCode Super)
    {
        left = Left;
        right = Right;
        bump = Bump;
        super = Super;
    }

    public PlayerButtons RotateKeysBasedOnObjRotation(Vector3 rotation)
    {
        PlayerButtons result = new PlayerButtons();

        //Tempset
        KeyCode tleft = left;
        KeyCode tright = right;
        KeyCode tbump = bump;
        KeyCode tsuper = super;

        float absZrot = Mathf.Abs(rotation.z);

        if(absZrot < 45)//Pointed Up P1 main
        {
            result.left = tleft;
            result.right = tright;
            result.bump = tbump;
            result.super = tsuper;
            result.dirShown = "U";
            result.dirDesc = "Pointed Up P1 main";
        }
        else if(absZrot < 90+45)//Pointed Left P1 main
        {
            result.left = tsuper;
            result.right = tbump;
            result.bump = tleft;
            result.super = tright;
            result.dirShown = "L";
            result.dirDesc = "Pointed Left P4 main";
        }
        else if (absZrot < 180 + 45) //Pointed Right P2 main
        {
            result.left = tleft;
            result.right = tright;
            result.bump = tsuper;
            result.super = tbump;
            result.dirShown = "R";
            result.dirDesc = "Pointed Right P2 main";
        }
        else//Pointed Down P3 main
        {
            result.left = tsuper;
            result.right = tbump;
            result.bump = tright;
            result.super = tleft;
            result.dirShown = "D";
            result.dirDesc = "Pointed Down P3 main";
        }

        return result;
    }
}
