using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteOnMenuLoss : MonoBehaviour
{
    public MenuControl mC;

    [SerializeField]
    public List<string> menus = new List<string>();
    public bool deleteInsteadOnMenuFound = false;

    public bool NonOverlay = false;

    public float DelayTillActive = 3;
    public float activationTime = -1;

    // Start is called before the first frame update
    void Start()
    {
        if(mC == null)
        {
            mC = GameObject.FindGameObjectWithTag("Menu").GetComponent<MenuControl>();
        }

        activationTime = Time.time + DelayTillActive;
    }

    // Update is called once per frame
    void Update()
    {
        if(activationTime > 0 && Time.time > activationTime)
        {
            Action();
        }
    }

    void Action()
    {
        if (deleteInsteadOnMenuFound)
        {
            if (menus.Contains(mC.GetOpenMenu(NonOverlay).title))
            {
                GameObject.Destroy(gameObject);
            }
        }
        else //Delete when menu is not found
        {
            if (menus.Contains(mC.GetOpenMenu(NonOverlay).title) == false)
            {
                GameObject.Destroy(gameObject);
            }
        }
    }
}
