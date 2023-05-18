using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatSpawn : MonoBehaviour
{
    PlayerGrab pG;

    public float cooldown = 2;
    float timeTillReset = 0;

    int spawnedAmount = 0;
    public int maxSpawns = -1;

    public GameObject spawnObj;
    public Vector3 spawnPosOffset = Vector3.zero;
    public Vector3 spawnPosRangeMin = Vector3.zero;
    public Vector3 spawnPosRangeMax = Vector3.zero;

    public Vector3 spawnRotOffset = Vector3.zero;
    public Vector3 spawnRotRangeMin = Vector3.zero;
    public Vector3 spawnRotRangeMax = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        pG = GetComponent<PlayerGrab>();
    }

    // Update is called once per frame
    void Update()
    {
        int mS = maxSpawns;

        if(maxSpawns < 0)
        {
            mS = spawnedAmount;
        }

        if (timeTillReset >= cooldown && spawnedAmount <= mS)
        {
            Vector3 spawnPos = transform.position;
            spawnPos += transform.up * spawnPosOffset.y * Random.Range(spawnPosRangeMin.y, spawnPosRangeMax.y);
            spawnPos += transform.right * spawnPosOffset.x * Random.Range(spawnPosRangeMin.x, spawnPosRangeMax.x);
            spawnPos += transform.forward * spawnPosOffset.z * Random.Range(spawnPosRangeMin.z, spawnPosRangeMax.z);

            Vector3 spawnRot = transform.rotation.eulerAngles;
            spawnRot.y += spawnRotOffset.y + Random.Range(spawnRotRangeMin.y, spawnRotRangeMax.y);
            spawnRot.x += spawnRotOffset.x + Random.Range(spawnRotRangeMin.x, spawnRotRangeMax.x);
            spawnRot.z += spawnRotOffset.z + Random.Range(spawnRotRangeMin.z, spawnRotRangeMax.z);

            GameObject go = Instantiate(spawnObj, spawnPos, Quaternion.Euler(spawnRot)) as GameObject;

            PlayerGrab npg = go.GetComponent<PlayerGrab>();

            if (npg != null)
            {
                npg.player = pG.player;
            }

            timeTillReset = 0;
            spawnedAmount++;
        }

        timeTillReset += Time.deltaTime;
    }
}
