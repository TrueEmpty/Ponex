using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(PlayerInput))]
[DefaultExecutionOrder(-200)] // Before PlayerGrab so wasPressedThisFrame / isPressed are fresh
public class ControllerLink : MonoBehaviour
{
    Database db;
    public int index = -1;
    public bool isAIControlled = false;

    PlayerInput playerInput;
    public List<ControllerButtons> buttons = new List<ControllerButtons>();
    public float ready = .5f;

    /// <summary>Hold Select / Backspace / Delete this long to unlink the controller.</summary>
    public float leaveHoldSeconds = 1.25f;
    float leaveHold;
    bool leaveFired;

    /// <summary>0–1 progress while holding Leave (for selector unfill visual).</summary>
    public float LeaveHoldProgress
    {
        get
        {
            if (leaveFired)
                return 1f;
            if (leaveHoldSeconds <= 0.0001f)
                return 0f;
            return Mathf.Clamp01(leaveHold / leaveHoldSeconds);
        }
    }

    public ControllerButtons this[string buttonName]
    {
        get
        {
            return buttons.Find(x=> x.name.Trim().ToLower() == buttonName.Trim().ToLower());
        }
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        db = Database.instance;

        // Character Creation: always route onto the draft (even if this instance was already listed)
        if (CharacterCreationManager.IsActive)
        {
            index = CharacterCreationManager.instance.BindControllerToDraft(this);
            if (index < 0)
                index = db.PlayerAdd(this);
            if (index < 0)
            {
                Destroy(gameObject);
                return;
            }
            AutoFillControllerLayout();
            return;
        }

        if(!db.controllers.Contains(this)) //&& !manager.controllers.Exists(x=> x.playerInput.devices.ToString() == playerInput.devices.ToString()))
        {
            index = db.PlayerAdd(this);

            if(index < 0)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }

        // Auto-fill controller buttons
        AutoFillControllerLayout();
    }

    void AutoFillControllerLayout()
    {
        foreach (var action in playerInput.actions)
        {
            string ajson = JsonUtility.ToJson(action);
            ControllerButtonType type = GetControllerButton(ajson, out string actionName, out float deadZone);

            if(actionName != "")
            {
                if (!buttons.Exists(b => b.name == actionName))
                {
                    buttons.Add(new ControllerButtons(actionName, type)
                    {
                        deadZone = deadZone
                    });
                }
            }
        }
    }

    ControllerButtonType GetControllerButton(string json, out string name, out float deadZone)
    {
        ControllerButtonType result = ControllerButtonType.NotFound;
        deadZone = 0;
        name = "";

        string[] split = json.Split(',');
        foreach (string st in split)
        {
            string s = st.Replace("\"", "").Replace("{", "").Replace("}", "");
            int iFound;
            if (FoundStringEnd(s,"m_Name:", out iFound))
            {
                name = s[iFound..].Replace("\"","").Replace(":","");
            }
            else if(FoundStringEnd(s, "m_Type:", out iFound) && result == ControllerButtonType.NotFound)
            {
                string mtype = s[iFound..].Replace("\"", "").Replace(":", "");

                if(int.TryParse(mtype,out int mT))
                {
                    if(mT == 1)
                    {
                        result = ControllerButtonType.Button;
                    }
                }
            }
            else if(FoundStringEnd(s, "m_ExpectedControlType:", out iFound) && result == ControllerButtonType.NotFound)
            {
                string mECT = s[iFound..].Replace("\"", "").Replace(":", "");

                if (mECT.Contains("Vector2", StringComparison.OrdinalIgnoreCase))
                {
                    result = ControllerButtonType.Vector2;
                }
                else if(mECT.Contains("Analog",StringComparison.OrdinalIgnoreCase))
                {
                    result = ControllerButtonType.SingleAxis;
                }
            }
            else if (FoundStringEnd(s, "m_Processors:StickDeadzone(min=", out iFound))
            {
                string mPro = s[iFound..].Replace("\"", "").Replace(":", "").Replace(")", "");
                if (float.TryParse(mPro, out float mP))
                {
                    deadZone = mP;
                }
            }
        }

        return result;
    }

    bool FoundStringEnd(string input, string find, out int indexFound, bool caseSensitive = false)
    {
        string useInput = input;
        string usefind = find;

        if (!caseSensitive)
        {
            useInput = input.ToLower();
            usefind = find.ToLower();
        }

        int fI = useInput.IndexOf(usefind);

        indexFound = (fI >= 0) ? fI + find.Length : -1;

        return (indexFound >= 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (isAIControlled) return; // Skip input read if AI controlled

        if (ready > 0)
        {
            ready -= Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        else
        {
            foreach (ControllerButtons button in buttons)
            {
                switch (button.buttonType)
                {
                    case ControllerButtonType.Vector2:
                        Vector2 output = playerInput.actions[button.name].ReadValue<Vector2>();
                        Vector2 absolute = Vector2.zero;
                        Vector2Int flatValue = Vector2Int.zero;

                        if (Mathf.Abs(output.x) > Mathf.Abs(button.deadZone))
                        {
                            if (output.x > 0)
                            {
                                flatValue.x = 1;
                            }
                            else
                            {
                                flatValue.x = -1;
                            }

                            absolute.x = output.x;
                        }

                        if (Mathf.Abs(output.y) > Mathf.Abs(button.deadZone))
                        {
                            if (output.y > 0)
                            {
                                flatValue.y = 1;
                            }
                            else
                            {
                                flatValue.y = -1;
                            }

                            absolute.y = output.y;
                        }

                        bool state = (Mathf.Abs(flatValue.x) + Mathf.Abs(flatValue.y) > 0);

                        //Run pulse
                        float pR = button.pulseRate;
                        if (state)
                        {
                            if (pR > 0)
                            {
                                if (button.pulse)
                                {
                                    button.pulse = false;
                                    button.nextPulse = pR;
                                }
                                else
                                {
                                    button.nextPulse -= Time.timeScale < 0.01f ? Time.unscaledDeltaTime : Time.deltaTime;

                                    if (button.nextPulse <= 0)
                                    {
                                        button.pulse = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            button.pulse = false;
                            button.nextPulse = 0;
                        }

                        button.wasPressedThisFrame = (state && !button.isPressed);
                        button.wasReleasedThisFrame = (!state && button.isPressed);
                        button.trueValue = output;
                        button.value = absolute;
                        button.flatValue = flatValue;
                        button.isPressed = state;
                        break;
                    case ControllerButtonType.Button:
                        bool stateBu = playerInput.actions[button.name].IsPressed();
                        button.wasPressedThisFrame = (stateBu && !button.isPressed);
                        button.wasReleasedThisFrame = (!stateBu && button.isPressed);
                        button.triggered = playerInput.actions[button.name].triggered;
                        button.isPressed = stateBu;
                        break;
                    case ControllerButtonType.SingleAxis:
                        float outputSA = playerInput.actions[button.name].ReadValue<float>();
                        float absoluteSA = 0;
                        int flatValueSA = 0;

                        if (Mathf.Abs(outputSA) > Mathf.Abs(button.deadZone))
                        {
                            if (outputSA > 0)
                            {
                                flatValueSA = 1;
                            }
                            else
                            {
                                flatValueSA = -1;
                            }

                            absoluteSA = outputSA;
                        }

                        bool stateSA = (Mathf.Abs(flatValueSA) > 0);

                        button.wasPressedThisFrame = (stateSA && !button.isPressed);
                        button.wasReleasedThisFrame = (!stateSA && button.isPressed);
                        button.trueValue = new Vector2(outputSA,0);
                        button.value = new Vector2(absoluteSA,0);
                        button.flatValue = new Vector2Int(flatValueSA,0);
                        button.isPressed = stateSA;
                        break;
                }
            }

            UpdateLeaveHold();
        }
    }

    /// <summary>
    /// Hold Leave (Select / Backspace / Delete) to drop this controller.
    /// Main Menu: removes the player. Character Select: removes if above minPlayers, else CPU.
    /// Past Character Select / in match: converts the slot to CPU.
    /// </summary>
    void UpdateLeaveHold()
    {
        if (db == null)
            db = Database.instance;
        if (db == null || index < 0)
            return;

        ControllerButtons leave = this["Leave"];
        bool held = leave != null && leave.isPressed;

        if (!held)
        {
            leaveHold = 0f;
            leaveFired = false;
            return;
        }

        if (leaveFired)
            return;

        leaveHold += Time.unscaledDeltaTime;
        if (leaveHold < leaveHoldSeconds)
            return;

        leaveFired = true;
        leaveHold = 0f;
        db.DisconnectPlayer(index);
    }
}

[System.Serializable]
public class ControllerButtons
{
    public string name;
    public ControllerButtonType buttonType = ControllerButtonType.Vector2;
    public float deadZone = 0;

    //[HideInInspector]
    public Vector2 trueValue = Vector2.zero;
    //[HideInInspector]
    public Vector2 value = Vector2.zero;
    //[HideInInspector]
    public Vector2Int flatValue = Vector2Int.zero;
    //[HideInInspector]
    public bool pulse = false; //Will pulse on and off for value grabs with a delay
    //[HideInInspector]
    public float pulseRate = .5f; //Rate of pulse
    //[HideInInspector]
    public float nextPulse = .5f; //next time pulse will fire
    //[HideInInspector]
    public bool isPressed = false;
    //[HideInInspector]
    public bool triggered = false;
    //[HideInInspector]
    public bool wasPressedThisFrame = false;
    //[HideInInspector]
    public bool wasReleasedThisFrame = false;

    public ControllerButtons()
    {
    }

    public ControllerButtons(string name, ControllerButtonType buttonType)
    {
        this.name = name;
        this.buttonType = buttonType;
    }
}

public enum ControllerButtonType
{
    Vector2,
    Button,
    SingleAxis,
    NotFound
}
