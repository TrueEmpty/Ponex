using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamSelect : MonoBehaviour
{
    public static TeamSelect instance;
    ButtonManager bm;
    Database db;

    List<Portait> teamGrabs = new List<Portait>();
    bool setup = false;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;
        bm = ButtonManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (setup)
        {
            for (int i = 0; i < db.players.Count; i++)
            {

            }
        }
        else
        {
            Setup();
        }
    }

    public void Setup()
    {
        if (!setup)
        {
            //Team Portraits
            Transform por = transform.GetChild(1);

            for (int i = 0; i < por.childCount; i++)
            {
                GameObject container = por.GetChild(i).gameObject;

                Portait p = new Portait();
                p.container = container;
                p.image = container.GetComponent<RawImage>();
                p.playerText = container.transform.GetChild(0).GetComponent<Text>();
                p.infoText = container.transform.GetChild(1).GetComponent<Text>();
                p.ready = container.transform.GetChild(2).gameObject;

                teamGrabs.Add(p);
            }
            setup = true;
        }
    }
}
