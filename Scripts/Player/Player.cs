using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    [SerializeField]
    public Stats player = new Stats();

    public string nickName = "";
    public int team = 0;
    public int position = 1;

    public PlayerButtons keys = new PlayerButtons();

    [SerializeField]
    public List<PlayerConstraints> canMove = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canBump = new List<PlayerConstraints>();
    [SerializeField]
    public List<PlayerConstraints> canSuper = new List<PlayerConstraints>();

    public bool selection = false;
}
