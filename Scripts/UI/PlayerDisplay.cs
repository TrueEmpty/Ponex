using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplay : MonoBehaviour
{
    public RawImage background;
    public Text playerName;
    public Text characterName;
    public Text health;
    public Image bump;
    public Image super;
    public Outline superOutline;
    bool superlastState = false;
    bool runSuperEffect = false;
    float superEffectPecent = 0;
    int superEffectDir = 1;
    public Image dash;

    public PlayerInfo player;
    Database db;

    public Color superNotReady = Color.red;
    public Color superReady = Color.yellow;

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(player != null)
        {
            Stats pS = player.player.player;

            int shownColor = 0;

            if(pS.currentHealth > 0 && player.player.team >= 0 && player.player.team < db.teamColors.Count)
            {
                shownColor = player.player.team;
            }

            background.color = db.teamColors[shownColor];

            if(pS.computer)
            {
                playerName.text = "CPU";
            }
            else
            {
                if(player.player.nickName == "" || player.player.nickName == null)
                {
                    playerName.text = "P" + player.player.position;
                }
                else
                {
                    playerName.text = player.player.nickName;
                }
            }

            characterName.text = pS.name;
            health.text = pS.currentHealth + "/" + pS.maxHealth;

            bump.fillAmount = pS.bumpReadyPercent;

            float sP = pS.superAmount / pS.superMax;

            super.fillAmount = sP;

            //Change Super's Color based on Ready
            Color superColor = superReady;
            bool currentSuperState = false;

            if(pS.superCost <= pS.superAmount && pS.superReadyPercent >= 1)
            {
                superColor = superReady;
                currentSuperState = true;
            }
            else if(pS.superReadyPercent < 1)
            {
                superColor = superNotReady;
            }
            else
            {
                superColor = Color.Lerp(superNotReady, superReady, (pS.superAmount / pS.superCost)/1.5f);
            }

            super.color = superColor;

            superOutline.enabled = currentSuperState;

            if(!superlastState && currentSuperState)
            {
                runSuperEffect = true;
            }

            superlastState = currentSuperState;

            if(!currentSuperState)
            {
                runSuperEffect = false;
                superOutline.effectDistance = new Vector2(1,-1);
                superEffectPecent = 0;
                superEffectDir = 1;
            }

            if(runSuperEffect)
            {
                superEffectPecent += superEffectDir * 300 * Time.deltaTime;

                if(superEffectDir == 1)
                {
                    if(superEffectPecent > 100)
                    {
                        superEffectPecent = 100;
                        superEffectDir = -1;
                    }
                }
                else
                {
                    if (superEffectPecent < 0)
                    {
                        runSuperEffect = false;
                        superOutline.effectDistance = new Vector2(1, -1);
                        superEffectPecent = 0;
                        superEffectDir = 1;
                    }
                }

                superOutline.effectDistance = new Vector2(Mathf.Lerp(1,30, superEffectPecent/100), -1);
            }

            dash.fillAmount = pS.dashReadyPercent;
        }
        else
        {
        }
    }
}
