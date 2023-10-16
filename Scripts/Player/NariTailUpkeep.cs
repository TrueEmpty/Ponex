using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NariTailUpkeep : MonoBehaviour
{
    PlayerGrab pG;
    public Transform head;
    public List<GameObject> tails = new List<GameObject>();
    public GameObject tail;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        //Remove all null tails
        tails.RemoveAll(x => x == null);

        //Check if tails count are lower than health
        int cH = pG.player.currentHealth;

        if (tails.Count < cH)
        {
            GameObject go = Instantiate(tail, transform.position, Quaternion.identity);

            tails.Add(go);
            int hI = tails.Count;

            float scale = (.38f - (.02f * hI));

            if(scale <= 0)
            {
                scale = .1f;
            }

            go.transform.localScale = Vector3.one * scale;

            PlayerGrab tPG = go.GetComponent<PlayerGrab>();
            tPG.playerIndex = pG.playerIndex;

            NariTail nT = go.GetComponent<NariTail>();
            nT.healthIndex = hI;
            nT.nTU = this;

            OffsetFollow oF = go.GetComponent<OffsetFollow>();
            
            float ungap = 4 - ((.5f * (hI - 1)));
            if (ungap < 1.5)
            {
                ungap = 1.5f;
            }

            if (hI == 1)
            {
                oF.parent = head;
                oF.unGap = ungap;
            }
            else
            {
                oF.parent = tails[hI - 2].transform;
                oF.unGap = ungap;
            }
        }

        if (pG.player.super.amount >= pG.player.super.cost)
        {
            pG.player.maxHealth++;
            pG.player.currentHealth++;
            pG.player.super.amount = 0;
        }
    }
}
