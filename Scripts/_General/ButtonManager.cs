using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager instance;

    public static List<ButtonCapture> buttonCaptures = new List<ButtonCapture>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        System.Array values = System.Enum.GetValues(typeof(KeyCode));
        foreach (KeyCode code in values)
        {
            buttonCaptures.Add(new ButtonCapture("Key " + code,code));
        }

        var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];

        SerializedObject obj = new SerializedObject(inputManager);

        SerializedProperty axisArray = obj.FindProperty("m_Axes");

        if (axisArray.arraySize > 0)
        {
            for (int i = 0; i < axisArray.arraySize; ++i)
            {
                var axis = axisArray.GetArrayElementAtIndex(i);

                var name = axis.FindPropertyRelative("m_Name").stringValue;

                buttonCaptures.Add(new ButtonCapture("+ " + name.ToString(),name.ToString(),true));
                buttonCaptures.Add(new ButtonCapture("- " + name.ToString(), name.ToString(),false));
            }
        }     
    }

    private void Update()
    {
        if(buttonCaptures.Count > 0)
        {
            for(int i = 0; i < buttonCaptures.Count; i++)
            {
                ButtonCapture bc = buttonCaptures[i];
                bool pressed = false;

                if (bc.Pressed())
                {
                    if (bc.state < 0)
                    {
                        bc.state = 0;
                    }
                    else
                    {
                        bc.state += Time.deltaTime;
                    }
                }
                else
                {
                    if (bc.state >= 0)
                    {
                        bc.state = -2;
                    }
                    else
                    {
                        bc.state = -1;
                    }
                }
            }
        }
    }

    public static ButtonCapture GetButton(KeyCode keycode)
    {
        return buttonCaptures.Find(x => (KeyCode)x.Value == keycode);
    }

    public static ButtonCapture GetButton(string axis,bool positive = true)
    {
        return buttonCaptures.Find(x => (string)x.Value == axis && x.axisPositive == positive);
    }

    public static ButtonCapture GetButton(string title)
    {
        return buttonCaptures.Find(x => x.name == title);
    }

    public static List<ButtonCapture> AllKeysPressed()
    {
        return buttonCaptures.FindAll(x => x.state >= 0);
    }

    public static bool KeyPressed(string name)
    {
        bool result = false;
        ButtonCapture bC = GetButton(name);

        if(bC != null)
        {
            result = bC.state >= 0;
        }

        return result;
    }

    public static bool KeyDown(string name)
    {
        bool result = false;
        ButtonCapture bC = GetButton(name);

        if(bC != null)
        {
            result = bC.state == 0;
        }

        return result;
    }

    public static bool KeyUp(string name)
    {
        bool result = false;
        ButtonCapture bC = GetButton(name);

        if(bC != null)
        {
            result = bC.state == -2;
        }

        return result;
    }

    public static bool KeyPressed(List<string> names, bool allPressed  = false)
    {
        bool result = false;
        
        if(names.Count > 0)
        {
            result = allPressed;
            
            for(int i = 0; i < names.Count; i++)
            {
                if(KeyPressed(names[i]))
                {
                    if(!allPressed)
                    {
                        return true;
                    }
                }
                else
                {
                    if(allPressed)
                    {
                        return false;
                    }
                }
            }
        }

        return result;
    }

    public static bool KeyDown(List<string> names, bool allPressed = false)
    {
        bool result = false;

        if (names.Count > 0)
        {
            result = allPressed;

            for (int i = 0; i < names.Count; i++)
            {
                if (KeyDown(names[i]))
                {
                    if (!allPressed)
                    {
                        return true;
                    }
                }
                else
                {
                    if (allPressed)
                    {
                        return false;
                    }
                }
            }
        }

        return result;
    }

    public static bool KeyUp(List<string> names, bool allPressed = false)
    {
        bool result = false;

        if (names.Count > 0)
        {
            result = allPressed;

            for (int i = 0; i < names.Count; i++)
            {
                if (KeyUp(names[i]))
                {
                    if (!allPressed)
                    {
                        return true;
                    }
                }
                else
                {
                    if (allPressed)
                    {
                        return false;
                    }
                }
            }
        }
        return result;
    }
}

[System.Serializable]
public class ButtonCapture
{
    public string name = "";
    public string axis = "";
    public bool axisPositive = true;
    public KeyCode keycode = KeyCode.None;
    public ButtonType type = ButtonType.KeyCode;
    public float state = -1;

    public ButtonCapture(string title,string value,bool positive = true)
    {
        name = title;
        axis = value;
        axisPositive = positive;
        type = ButtonType.Axis;
    }

    public ButtonCapture(string title, KeyCode value)
    {
        name = title;
        keycode = value;
        this.type = ButtonType.KeyCode;
    }

    public object Value
    {
        set
        {
            if(value.GetType() == typeof(KeyCode))
            {
                keycode = (KeyCode)value;
                type = ButtonType.KeyCode;
            }
            else if(value.GetType() == typeof(KeyCode))
            {
                axis = (string)value;
                type = ButtonType.Axis;
            }
            else
            {
                axis = value.ToString();
                type = ButtonType.Axis;
            }
        }
        get
        {
            switch(type)
            {
                case ButtonType.KeyCode:
                    return keycode;
                case ButtonType.Axis:
                    return axis;
                default:
                    return axis;
            }
        }
    }

    public bool Pressed()
    {
        switch (type)
        {
            case ButtonType.KeyCode:
                return Input.GetKey(keycode);
            case ButtonType.Axis:
                return axisPositive ? Input.GetAxis(axis) > .01f : Input.GetAxis(axis) < -.01f;
            default:
                return axisPositive ? Input.GetAxis(axis) > .01f : Input.GetAxis(axis) < -.01f;
        }
    }

    public string StringOut()
    {
        string result = "";
        result += type == ButtonType.KeyCode ? "KeyCode: " : "Axis: ";
        result += type == ButtonType.KeyCode ? keycode : (axisPositive ? "+" : "-") + axis;
        return result;
    }
}

public enum ButtonType
{
    Axis,
    KeyCode
}
