using Unity.VisualScripting;
using UnityEngine;

public class PlayerGrab : MonoBehaviour
{
    Database db;
    public int playerIndex = -1;

    public Inputs inp = new Inputs();

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    void Update()
    {
        UpdateInputs();
    }

    void UpdateInputs()
    {
        Player p = player;

        if (p != null)
        {
            ControllerLink cL = p.cLink;

            if (cL != null)
            {
                //Get Last Frames
                bool lf_r = inp.right;
                bool lf_l = inp.left;
                bool lf_u = inp.up;
                bool lf_d = inp.down;

                //Get true directions
                bool r = cL["Move"].value.x > 0.3f || cL["Crouch"].isPressed;
                bool l = cL["Move"].value.x < -0.3f || cL["Attack"].isPressed;
                bool u = (cL["Move"].value.y > 0.3f);
                bool u2 = cL["Jump"].isPressed;
                bool d = cL["Move"].value.y < -0.3f;
                bool d2 = cL["Interact"].isPressed;


                if (p.ignoreFacing)
                {
                    inp.up = u;
                    inp.down = d;
                    inp.right = r;
                    inp.left = l;
                }
                else
                {
                    switch (p.facing)
                    {
                        case Facing.Up:
                            inp.up = u || u2;
                            inp.down = d || d2;
                            inp.right = r;
                            inp.left = l;
                            break;
                        case Facing.Down:
                            inp.up = d || u2;
                            inp.down = u || d2;
                            inp.right = r;
                            inp.left = l;
                            break;
                        case Facing.Left:
                            inp.up = r || u2;
                            inp.down = l || d2;
                            inp.right = d;
                            inp.left = u;
                            break;
                        case Facing.Right:
                            inp.up = l || u2;
                            inp.down = r || d2;
                            inp.right = u;
                            inp.left = d;
                            break;
                    }
                }

                //Set Pressed this frames
                inp.tf_right = (!lf_r && inp.right);
                inp.tf_left = (!lf_l && inp.left);
                inp.tf_up = (!lf_u && inp.up);
                inp.tf_down = (!lf_d && inp.down);
            }
        }
    }

    public Player player
    {
        get 
        {
            Player result = null;

            if(playerIndex >= 0 && playerIndex < db.players.Count)
            {
                result = db.players[playerIndex];
            }

            return result;
        }
    }

    public bool IsLinked()
    {
        return  playerIndex >= 0 && playerIndex < db.players.Count;
    }
}

public class Inputs
{
    public bool right = false;
    public bool left = false;
    public bool up = false;
    public bool down = false;
    public bool bump => up;
    public bool super => down;

    //Activated this frame
    public bool tf_right = false;
    public bool tf_left = false;
    public bool tf_up = false;
    public bool tf_down = false;
    public bool tf_bump => tf_up;
    public bool tf_super => tf_down;

}