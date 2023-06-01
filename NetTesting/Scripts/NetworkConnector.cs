using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public delegate void RPC_Callback(CallbackVar cbv);
public class NetworkConnector : MonoBehaviour
{
    public static NetworkConnector instance;
    public List<RPC> rpcs = new List<RPC>();
    bool lockGet = false;

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

    private void Update()
    {
        if(rpcs.Count > 0 && !lockGet)
        {
            lockGet = true;

            switch(rpcs[0].requestType)
            {
                case RPC.RequestType.GET:
                    StartCoroutine(GetRPC(rpcs[0]));
                    break;
                case RPC.RequestType.POST:
                    StartCoroutine(PostRPC(rpcs[0]));
                    break;
                case RPC.RequestType.PUT:
                    StartCoroutine(PutRPC(rpcs[0]));
                    break;
                default:
                    StartCoroutine(GetRPC(rpcs[0]));
                    break;
            }
        }
    }

    public void SendRPC(string URL, WWWForm data, RPC_Callback callbackMeth = null, RPC.RequestType reType = RPC.RequestType.GET)
    {
        RPC newRPC = new RPC(reType, URL, data, callbackMeth);

        if (!rpcs.Contains(newRPC))
        {
            rpcs.Add(newRPC);
        }
    }

    IEnumerator GetRPC(RPC rpc)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(rpc.URL))
        {
            yield return request.SendWebRequest();

            SendResult(request, rpc);
        };

        yield return new WaitForEndOfFrame();
    }

    IEnumerator PostRPC(RPC rpc)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(rpc.URL,rpc.data))
        {
            yield return request.SendWebRequest();

            SendResult(request, rpc);
        };

        yield return new WaitForEndOfFrame();
    }

    IEnumerator PutRPC(RPC rpc)
    {
        using (UnityWebRequest request = UnityWebRequest.Put(rpc.URL,""))
        {
            yield return request.SendWebRequest();

            SendResult(request, rpc);
        };

        yield return new WaitForEndOfFrame();
    }

    public void SendResult(UnityWebRequest request, RPC rpc)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            //print(request.downloadHandler.text);
            print("<color=Green>" + request.result + "</color>" + "\n" + request.error + "\n" + request.downloadHandler.text);

            if (rpc.callbackMethod != null)
            {
                rpc.callbackMethod(new CallbackVar(request.downloadHandler.text));
            }
        }
        else
        {
            print("<color=Red>" + request.result + "</color>" + "\n" + request.error + "\n" + request.downloadHandler.text);

            if (rpc.callbackMethod != null)
            {
                rpc.callbackMethod(new CallbackVar(request.downloadHandler.text,true,request.error));
            }
        }

        lockGet = false;
        rpcs.RemoveAt(0);
    }
}

public class RPC
{
    public RequestType requestType = RequestType.GET;
    public string URL;
    public WWWForm data;

    public RPC_Callback callbackMethod;

    public enum RequestType
    {
        POST,
        GET,
        PUT
    }

    public RPC(RequestType reType, string url, WWWForm info, RPC_Callback callbackMeth)
    {
        requestType = reType;
        URL = url;
        data = info;

        callbackMethod = callbackMeth;
    }
}

public class CallbackVar
{
    public string data;
    public bool error = false;
    public string errorData = "";

    public CallbackVar(string Data, bool Error = false, string ErrorData = "")
    {
        data = Data;
        error = Error;
        errorData = ErrorData;
    }
}