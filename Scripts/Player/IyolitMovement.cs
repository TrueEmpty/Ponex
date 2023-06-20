using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitMovement : MonoBehaviour
{
    public PlayerGrab pG;
    ButtonManager bm;
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
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(ready)
        {
            if (positions.Count > 0)
            {
                for (int i = positions.Count - 1; i >= 0; i--)
                {
                    if (positions[i] == null)
                    {
                        positions.RemoveAt(i);

                        if(currentPosition == i)
                        {
                            lastPosition = -1;
                        }

                        if(currentPosition >= positions.Count)
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
                        percentDis += pG.player.Speed * Time.deltaTime;
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
                    if (bm.KeyDown(pG.player.keys.right))
                    {
                        currentPosition++;

                        if (currentPosition >= positions.Count)
                        {
                            currentPosition = positions.Count - 1;
                        }
                    }

                    if (bm.KeyDown(pG.player.keys.left))
                    {
                        currentPosition--;

                        if (currentPosition < 0)
                        {
                            currentPosition = 0;
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

            bumpActive = bm.KeyPressed(pG.player.keys.bump);

            if (superCooldownTimer < superCooldown)
            {
                superCooldownTimer += Time.deltaTime;
            }

            pG.player.player.superReadyPercent = superCooldownTimer / superCooldown;

            if(pG.player.player.superReadyPercent < 0)
            {
                pG.player.player.superReadyPercent = 0;
            }
            else if(pG.player.player.superReadyPercent > 1)
            {
                pG.player.player.superReadyPercent = 1;
            }


            if (bm.KeyDown(pG.player.keys.super) && !SuperOn() && pG.player.CanSuper && pG.player.player.superAmount >= pG.player.player.superCost && pG.player.player.superReadyPercent >= 1)
            {
                superOn = superTime;
                superCooldownTimer = 0;
                pG.player.CostSuper(pG.player.player.superCost);
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
