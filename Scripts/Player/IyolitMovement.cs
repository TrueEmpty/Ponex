using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitMovement : MonoBehaviour
{
    Database db;
    public PlayerGrab pG;
    public List<Transform> positions = new List<Transform>();
    public int currentPosition = 0;
    public int lastPosition = 0;
    public float maxheight = 3;
    public Vector3 startPos = Vector3.zero;
    public bool moving = false;
    public float percentDis = 0;
    public float percentFC = 0;
    bool ready = false;
    public float superOn = 0;
    public float superTime = 3;
    float superCooldown = 5;
    float superCooldownTimer = 0;
    public bool bumpActive = false;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        db = Database.instance;
        SetupFlameVisuals();
    }

    void SetupFlameVisuals()
    {
        // Root = ember/aura body particles; child flame blob sits on the wick
        IyolitFlameFx.Ensure(gameObject, IyolitFlameFx.Style.BodyAura);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
                continue;
            if (child.GetComponent<ParticleSystem>() != null)
                IyolitFlameFx.Ensure(child.gameObject, IyolitFlameFx.Style.BodyCore);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(db.gameStart && pG.player.currentHealth > 0)
        {
            if (ready)
            {
                if (positions.Count > 0)
                {
                    for (int i = positions.Count - 1; i >= 0; i--)
                    {
                        if (positions[i] == null)
                        {
                            positions.RemoveAt(i);

                            if (currentPosition == i)
                            {
                                lastPosition = -1;
                            }

                            if (currentPosition >= positions.Count)
                            {
                                currentPosition = positions.Count - 1;
                            }
                        }
                    }
                }

                if (positions.Count > 0)
                {
                    if (currentPosition != lastPosition)
                    {
                        if (!moving)
                        {
                            moving = true;
                            startPos = transform.position;
                            percentDis = 0;
                        }

                        if (moving)
                        {
                            // Half speed between candles
                            percentDis += pG.player.movementSpeed * 0.5f * Time.deltaTime;
                            percentFC = 1 - (Mathf.Abs(.5f - percentDis) * 2);

                            if (percentDis < 1)
                            {
                                transform.position = Vector3.Lerp(lastPosition < 0 ? startPos : positions[lastPosition].position, positions[currentPosition].position + (positions[currentPosition].up * (maxheight * percentFC)), percentDis);
                            }
                            else
                            {
                                transform.position = positions[currentPosition].position;
                                lastPosition = currentPosition;
                                percentDis = 0;
                                moving = false;
                            }
                        }
                    }

                    if (currentPosition == lastPosition && !moving)
                    {
                        // Lane inputs are reversed vs candle index order — swap so stick matches on-screen move
                        if (pG.inp.tf_right)
                        {
                            currentPosition--;

                            if (currentPosition < 0)
                            {
                                currentPosition = 0;
                            }
                        }

                        if (pG.inp.tf_left)
                        {
                            currentPosition++;

                            if (currentPosition >= positions.Count)
                            {
                                currentPosition = positions.Count - 1;
                            }
                        }

                        //Keep it on the Candle
                        transform.position = positions[currentPosition].position;
                    }
                }
                else
                {
                    //Destroy the player
                    gameObject.SetActive(false);
                }

                if (superOn > 0)
                {
                    superOn -= Time.deltaTime;
                }

                bumpActive = pG.inp.bump;

                if (superCooldownTimer < superCooldown)
                {
                    superCooldownTimer += Time.deltaTime;
                }

                pG.player.super.readyPercent = superCooldownTimer / superCooldown;

                if (pG.player.super.readyPercent < 0)
                {
                    pG.player.super.readyPercent = 0;
                }
                else if (pG.player.super.readyPercent > 1)
                {
                    pG.player.super.readyPercent = 1;
                }


                if (pG.inp.tf_super && !SuperOn() && pG.player.CanSuper && pG.player.super.amount >= pG.player.super.cost && pG.player.super.readyPercent >= 1)
                {
                    superOn = superTime;
                    superCooldownTimer = 0;
                    pG.player.RecordUltUsed();
                    //pG.player.CostSuper(pG.player.player.player.superCost);
                }
            }
        }
    }

    public bool SuperOn()
    {
        return superOn > 0;
    }

    public void AddPosition(Transform trans,bool lastOne = false)
    {
        //Add Position Ordered by position
        positions.Add(trans);

        if(lastOne)
        {
            //Update current Position which will be the middle
            currentPosition = Mathf.RoundToInt(positions.Count / 2) - 1;

            if (currentPosition < 0)
            {
                currentPosition = 0;
            }

            if (currentPosition >= positions.Count)
            {
                currentPosition = 0;
            }

            ready = true;
        }
    }

    public Transform CurrentCandle()
    {
        Transform result = null;

        if(!moving)
        {
            if(currentPosition >= 0 && currentPosition < positions.Count)
            {
                result = positions[currentPosition].parent.parent;
            }
        }

        return result;
    }
}
