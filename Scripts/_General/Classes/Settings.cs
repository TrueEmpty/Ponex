using UnityEngine;

[System.Serializable]
public class Settings
{
    public PlayerButtons p1Buttons = new PlayerButtons(KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S);
    public PlayerButtons p2Buttons = new PlayerButtons(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.UpArrow);
    public PlayerButtons p3Buttons = new PlayerButtons(KeyCode.Keypad2, KeyCode.Keypad8, KeyCode.Keypad4, KeyCode.Keypad5);
    public PlayerButtons p4Buttons = new PlayerButtons(KeyCode.M, KeyCode.U, KeyCode.K, KeyCode.J);

    public Settings()
    {

    }


}
