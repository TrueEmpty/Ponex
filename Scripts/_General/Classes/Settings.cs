using UnityEngine;

[System.Serializable]
public class Settings
{
    public PlayerButtons p1Buttons = new PlayerButtons(KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S);
    public PlayerButtons p2Buttons = new PlayerButtons(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow);
    public PlayerButtons p3Buttons = new PlayerButtons(KeyCode.Keypad2, KeyCode.Keypad8, KeyCode.Keypad4, KeyCode.Keypad5);
    public PlayerButtons p4Buttons = new PlayerButtons(KeyCode.J, KeyCode.L, KeyCode.I, KeyCode.K);    
    public PlayerButtons p5Buttons = new PlayerButtons(KeyCode.F, KeyCode.H, KeyCode.T, KeyCode.G);   
    public PlayerButtons p6Buttons = new PlayerButtons();   
    public PlayerButtons p7Buttons = new PlayerButtons();
    public PlayerButtons p8Buttons = new PlayerButtons();

    public Settings()
    {

    }


}
