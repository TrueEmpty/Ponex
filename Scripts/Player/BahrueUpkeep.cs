using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BahrueUpkeep : MonoBehaviour
{
    PlayerGrab pG;
    ButtonManager bm;

    [SerializeField]
    List<FollowPlayer> bahrueObjs = new List<FollowPlayer>();
    Stats p;
    int lastHealth = -1;

    public GameObject bahrueObj;
    public GameObject bahrueBump;

    [SerializeField]
    List<Vector2Int> amountPerRow= new List<Vector2Int>();

    public Vector3 offsetAmounts = Vector3.zero;
    public Transform holder;

    public Transform token;
    public Renderer ring;
    public Renderer emblem;

    public float bumpCooldown = 2;
    float timeTillReset = 0;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!pG.player.player.selection)
        {
            p = pG.player.player.player;

            if (p.currentHealth > 0)
            {
                if(lastHealth != p.currentHealth)
                {
                    UpdateBahrue();
                }

                ActivateSuper();
                ActivateBump();

                pG.player.player.player.movementSpeed = 1.5f * (50 / (float)pG.player.player.player.currentHealth);
            }
        }
    }

    void UpdateBahrue()
    {
        //Remove Extras
        if(bahrueObjs.Count > p.currentHealth)
        {
            for(int i = bahrueObjs.Count - 1; i >= p.currentHealth; i--)
            {
                //Run Animation at position

                //Remove
                Destroy(bahrueObjs[i].gameObject);
                bahrueObjs.RemoveAt(i);
            }
        }

        //Add Missing
        if (bahrueObjs.Count < p.currentHealth)
        {
            for (int i = bahrueObjs.Count; i < p.currentHealth; i++)
            {
                //Run Animation at position

                //Add
                GameObject go = Instantiate(bahrueObj);

                go.transform.parent = holder;
                go.transform.position = holder.position + transform.up * -2.5f;

                PlayerGrab cpg = go.GetComponent<PlayerGrab>();
                FollowPlayer fp = go.GetComponent<FollowPlayer>();

                if (cpg != null)
                {
                    cpg.player = pG.player;
                }

                if (fp != null)
                {
                    fp.offset = new Vector3(0, -5,0);
                    bahrueObjs.Add(fp);
                }
            }
        }

        //Update offsets
        if(bahrueObjs.Count > 0)
        {
            amountPerRow = AmountPerRow();
            for (int i = 0; i < bahrueObjs.Count; i++)
            {
                Vector3 getPosOff = GetPosition(i);

                if(getPosOff != bahrueObjs[i].offset)
                {
                    bahrueObjs[i].LerpOffset(getPosOff,5.5f);
                }
            }
        }

        lastHealth = p.currentHealth;
    }

    void ActivateSuper()
    {
        Vector3 pP = Vector3.zero;
        pP.y = GetTopPoint() + 1;
        token.localPosition = pP;

        if (bm.KeyPressed(pG.player.player.keys.super))
        {
            pG.player.AddConstraint(gameObject, -1, PlayerConstraint.Move);
            pG.player.AddConstraint(gameObject, -1, PlayerConstraint.Bump);

            float superGainSpeed = .75f * (50/ (float)pG.player.player.player.currentHealth);
            pG.player.player.player.superAmount += ((Time.deltaTime * superGainSpeed)/100);

            if(pG.player.player.player.superAmount >= pG.player.player.player.superCost)
            {
                pG.player.player.player.maxHealth ++;
                pG.player.Heal(1);
                pG.player.player.player.superAmount = 0;
                //Play Shine Animation/Sound/Particle
            }
        }
        else
        {
            pG.player.RemoveConstraint(gameObject, PlayerConstraint.Move);
            pG.player.RemoveConstraint(gameObject, PlayerConstraint.Bump);
            pG.player.player.player.superAmount = 0;
        }

        ring.gameObject.SetActive(pG.player.player.player.superAmount != 0);
        emblem.gameObject.SetActive(pG.player.player.player.superAmount != 0);

        Color rC = ring.material.color;
        Color eC = emblem.material.color;

        rC.a = pG.player.player.player.superAmount;
        eC.a = pG.player.player.player.superAmount;

        ring.material.color = rC;
        emblem.material.color = eC;

        pG.player.player.player.maxHealth = pG.player.player.player.currentHealth;
    }

    void ActivateBump()
    {
        float bP = timeTillReset / bumpCooldown;

        if (bP > 1)
        {
            bP = 1;
        }
        else if (bP < 0)
        {
            bP = 0;
        }

        pG.player.player.player.bumpReadyPercent = bP;

        if (bm.KeyDown(pG.player.player.keys.bump))
        {
            if (timeTillReset >= bumpCooldown && pG.player.CanBump)
            {
                if(bahrueObjs.Count > 1)
                {
                    if(amountPerRow.Count > 1)
                    {
                        int cILR = amountPerRow[^1].x;

                        if(bahrueObjs.Count > cILR)
                        {
                            for (int i = 0; i < cILR; i++)
                            {
                                GameObject bo = bahrueObjs[^1].gameObject;

                                Vector3 spawnPos = bo.transform.position;
                                GameObject go = Instantiate(bahrueBump, spawnPos, transform.rotation) as GameObject;

                                PlayerGrab pg = go.GetComponent<PlayerGrab>();

                                if (pg != null)
                                {
                                    pg.player = pG.player;
                                }

                                pG.player.player.player.currentHealth --;
                                pG.player.player.player.maxHealth = pG.player.player.player.currentHealth;
                                Destroy(bo);
                                bahrueObjs.RemoveAt(bahrueObjs.Count - 1);
                            }

                            timeTillReset = 0;
                        }
                    }
                }
            }
        }

        timeTillReset += Time.deltaTime;
    }

    Vector3 GetPosition(int index)
    {
        Vector3 result = Vector3.zero;
        int totalCount = 0;
        int row = -1;
        int col = -1;

        if(amountPerRow.Count > 0)
        {
            for(int i = 0; i < amountPerRow.Count; i++)
            {
                Vector2Int apr = amountPerRow[i];
                if (index < totalCount + apr.x)
                {
                    row = i;
                    col = totalCount - index;
                    break;
                }

                totalCount += apr.x;
            }

            if(row >= 0 && row < amountPerRow.Count)
            {
                Vector2Int apr = amountPerRow[row];
                float maxDis = (apr.x * offsetAmounts.x) * .5f;
                result.x = (maxDis + (col * offsetAmounts.x)) - (offsetAmounts.x/2);
                result.y = row * offsetAmounts.y;
            }
        }

        return result;
    }

    List<Vector2Int> AmountPerRow()
    {
        List<Vector2Int> result = new List<Vector2Int>();
        //How Many in that row,Position Until Up = > 2

        if(bahrueObjs.Count > 0)
        {
            result.Add(new Vector2Int(0,0));

            for(int i = 0; i < bahrueObjs.Count; i++)
            {
                for(int r = 0; r < result.Count; r++)
                {
                    Vector2Int v2I = result[r];
                    if (v2I.y < 2)
                    {
                        v2I.x++;
                        v2I.y++;
                        result[r] = v2I;
                        break;
                    }
                    else
                    {
                        if (result.Count <= r+1)
                        {
                            for (int n = r; n >= 0; n--)
                            {
                                Vector2Int n2I = result[n];
                                n2I.y = 1;
                                result[n] = n2I;
                            }

                            result.Add(new Vector2Int(1, 1));
                            break;
                        }
                    }
                }
            }
        }
        
        return result;
    }

    float GetTopPoint()
    {
        return amountPerRow.Count * offsetAmounts.y;
    }
}
