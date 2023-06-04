using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerButtons
{
    [Tooltip("This should be the absolute Left not adjusted for rotation")]
    public List<KeyCode> left = new List<KeyCode>();
    [Tooltip("This should be the absolute Right not adjusted for rotation")]
    public List<KeyCode> right = new List<KeyCode>();
    [Tooltip("This should be the absolute Up not adjusted for rotation")]
    public List<KeyCode> bump = new List<KeyCode>();
    [Tooltip("This should be the absolute Down not adjusted for rotation")]
    public List<KeyCode> super = new List<KeyCode>();

    public string dirShown = "Up";
    public string dirDesc = "Up";

    public float leftTime = -1;
    public float rightTime = -1;
    public float bumpTime = -1;
    public float superTime = -1;

    public bool cpuControlled = false;

    public PlayerButtons()
    {

    }

    public PlayerButtons(PlayerButtons pb)
    {
        left = pb.left;
        right = pb.right;
        bump = pb.bump;
        super = pb.super;

        dirShown = pb.dirShown;
        dirDesc = pb.dirDesc;
    }

    public PlayerButtons(KeyCode Left, KeyCode Right, KeyCode Bump, KeyCode Super)
    {
        left.Add(Left);
        right.Add(Right);
        bump.Add(Bump);
        super.Add(Super);
    }

    public PlayerButtons RotateKeysBasedOnObjRotation(Vector3 rotation)
    {
        PlayerButtons result = new PlayerButtons();

        //Tempset
        List<KeyCode> tleft = left;
        List<KeyCode> tright = right;
        List<KeyCode> tbump = bump;
        List<KeyCode> tsuper = super;

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

    public void ButtonPressUpdate()
    {
        if(!cpuControlled)
        {
            bool pressed = false;

            if (left.Count > 0)
            {
                pressed = false;
                for(int i = 0; i < left.Count; i++)
                {
                    if(Input.GetKey(left[i]))
                    {
                        pressed = true;
                        break;
                    }
                }

                if(pressed)
                {
                    if(leftTime < 0)
                    {
                        leftTime = 0;
                    }
                    else
                    {
                        leftTime += Time.deltaTime;
                    }
                }
                else
                {
                    if (leftTime <= 0)
                    {
                        leftTime = -2;
                    }
                    else
                    {
                        leftTime = -1;
                    }
                }
            }

            if (right.Count > 0)
            {
                pressed = false;
                for (int i = 0; i < right.Count; i++)
                {
                    if (Input.GetKey(right[i]))
                    {
                        pressed = true;
                        break;
                    }
                }

                if (pressed)
                {
                    if (rightTime < 0)
                    {
                        rightTime = 0;
                    }
                    else
                    {
                        rightTime += Time.deltaTime;
                    }
                }
                else
                {
                    if (rightTime <= 0)
                    {
                        rightTime = -2;
                    }
                    else
                    {
                        rightTime = -1;
                    }
                }
            }

            if (bump.Count > 0)
            {
                pressed = false;
                for (int i = 0; i < bump.Count; i++)
                {
                    if (Input.GetKey(bump[i]))
                    {
                        pressed = true;
                        break;
                    }
                }

                if (pressed)
                {
                    if (bumpTime < 0)
                    {
                        bumpTime = 0;
                    }
                    else
                    {
                        bumpTime += Time.deltaTime;
                    }
                }
                else
                {
                    if (bumpTime <= 0)
                    {
                        bumpTime = -2;
                    }
                    else
                    {
                        bumpTime = -1;
                    }
                }
            }

            if (super.Count > 0)
            {
                pressed = false;
                for (int i = 0; i < super.Count; i++)
                {
                    if (Input.GetKey(super[i]))
                    {
                        pressed = true;
                        break;
                    }
                }

                if (pressed)
                {
                    if (superTime < 0)
                    {
                        superTime = 0;
                    }
                    else
                    {
                        superTime += Time.deltaTime;
                    }
                }
                else
                {
                    if (superTime <= 0)
                    {
                        superTime = -2;
                    }
                    else
                    {
                        superTime = -1;
                    }
                }
            }
        }
    }

    public bool KeyPressedDown(Direction direction)
    {
        bool result = false;

        switch(direction)
        {
            case Direction.Left:
                result = (leftTime == 0);
                break;
            case Direction.Right:
                result = (rightTime == 0);
                break;
            case Direction.Bump:
                result = (bumpTime == 0);
                break;
            case Direction.Super:
                result = (superTime == 0);
                break;
        }

        return result;
    }

    public bool KeyPressedUp(Direction direction)
    {
        bool result = false;

        switch (direction)
        {
            case Direction.Left:
                result = (leftTime == -2);
                break;
            case Direction.Right:
                result = (rightTime == -2);
                break;
            case Direction.Bump:
                result = (bumpTime == -2);
                break;
            case Direction.Super:
                result = (superTime == -2);
                break;
        }

        return result;
    }

    public bool KeyPressed(Direction direction)
    {
        bool result = false;

        switch (direction)
        {
            case Direction.Left:
                result = (leftTime >= 0);
                break;
            case Direction.Right:
                result = (rightTime >= 0);
                break;
            case Direction.Bump:
                result = (bumpTime >= 0);
                break;
            case Direction.Super:
                result = (superTime >= 0);
                break;
        }

        return result;
    }
}

public enum Direction
{
    Left,
    Right,
    Bump,
    Super
}
