using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerGrab))]
public class IyolitLifelineCreate : MonoBehaviour
{
    public GameObject candle;
    PlayerGrab pG;
    IyolitMovement iM;
    int candlesToSpawn = 4;

    List<IyolitCandleMelt> candles = new List<IyolitCandleMelt>();

    bool ready = false;
    string wallTag = "Wall";
    public LayerMask wallLayer;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
        iM = pG.player.GetComponent<IyolitMovement>();
        SpawnCandles();
    }

    // Update is called once per frame
    void Update()
    {
        if (ready)
        {
            //Update Current Health
            float totalCurHp = 0;

            if (candles.Count > 0)
            {
                for (int i = candles.Count - 1; i >= 0; i--)
                {
                    if (candles[i] == null)
                    {
                        candles.RemoveAt(i);
                    }
                    else
                    {
                        totalCurHp += candles[i].curHealth;
                    }
                }
            }

            pG.player.player.currentHealth = Mathf.CeilToInt(totalCurHp);
        }
    }

    void SpawnCandles()
    {
        float healthPerCandle = pG.player.player.maxHealth / candlesToSpawn;

        Vector3 pos1 = Vector3.zero;
        Vector3 pos2 = Vector3.zero;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + (transform.up * 1), transform.right * -1, out hit, 1000, wallLayer))
        {
            if (wallTag.ToLower().Trim() == hit.transform.tag.ToLower().Trim())
            {
                pos1 = hit.point;
            }
        }

        if (Physics.Raycast(transform.position + (transform.up * 1), transform.right, out hit,1000,wallLayer))
        {
            if (wallTag.ToLower().Trim() == hit.transform.tag.ToLower().Trim())
            {
                pos2 = hit.point;
            }
        }

        float total_Distance = Vector3.Distance(pos1, pos2) - 10;
        float segment_Distance = total_Distance / (candlesToSpawn-1);

        Vector3 pos = transform.position;
        pos += transform.up * 2.5f;
        pos += transform.right * -1 * (total_Distance / 2);

        for (int i = 0; i < candlesToSpawn; i++)
        {
            GameObject go = Instantiate(candle, pos + (transform.right * (segment_Distance * i)),Quaternion.identity);

            IyolitCandleMelt iCM = go.GetComponent<IyolitCandleMelt>();

            if(iCM != null)
            {
                iCM.iM = iM;
                iCM.maxHealth = healthPerCandle;
                iCM.curHealth = healthPerCandle;
            }

            PlayerGrab iPG = go.GetComponent<PlayerGrab>();

            if(iPG != null)
            {
                iPG.player = pG.player;
            }

            candles.Add(iCM);
            iM.AddPosition(go.transform.GetChild(0).GetChild(0),i == candlesToSpawn - 1);
        }

        ready = true;
    }
}
