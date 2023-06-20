using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageDisplay : MonoBehaviour
{
    public static MessageDisplay instance;
    public Text message;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        message.gameObject.SetActive(message.text != "" && message.text != null);
    }

    public void Write(string data, float time = -1)
    {
        message.text = data;

        if (time > 0)
        {
            Invoke("Clear", time);
        }
    }

    public void Clear()
    {
        message.text = "";
    }
}
