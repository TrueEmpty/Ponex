using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class OnlineRelay : MonoBehaviour
{
    ClientWebSocket socket;
    CancellationTokenSource cancel;
    readonly Queue<string> incoming = new Queue<string>();
    readonly object gate = new object();
    readonly Dictionary<string, string> lastSent = new Dictionary<string, string>();

    public bool Connected => socket != null && socket.State == WebSocketState.Open;

    public void Connect(string url, string lobby, string key, string uid, bool host)
    {
        Disconnect();
        if (string.IsNullOrEmpty(url))
            return;
        cancel = new CancellationTokenSource();
        socket = new ClientWebSocket();
        string hello = "{\"t\":\"hello\",\"lobby\":" + OnlineFirebase.Quote(lobby)
            + ",\"key\":" + OnlineFirebase.Quote(key)
            + ",\"uid\":" + OnlineFirebase.Quote(uid)
            + ",\"host\":" + (host ? "true" : "false") + "}";
        Task.Run(() => Run(url, hello, cancel.Token));
    }

    public void Disconnect()
    {
        if (cancel != null)
        {
            cancel.Cancel();
            cancel = null;
        }
        if (socket != null)
        {
            try { socket.Abort(); } catch { }
            socket.Dispose();
            socket = null;
        }
        lastSent.Clear();
    }

    public void Send(string json)
    {
        if (!Connected || string.IsNullOrEmpty(json))
            return;
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancel.Token);
        }
        catch { }
    }

    public void SendDelta(string key, string json)
    {
        if (lastSent.TryGetValue(key, out string prev) && prev == json)
            return;
        lastSent[key] = json;
        Send(json);
    }

    public bool TryPop(out string json)
    {
        lock (gate)
        {
            if (incoming.Count == 0)
            {
                json = null;
                return false;
            }
            json = incoming.Dequeue();
            return true;
        }
    }

    async Task Run(string url, string hello, CancellationToken token)
    {
        try
        {
            await socket.ConnectAsync(new Uri(url), token);
            byte[] helloBytes = Encoding.UTF8.GetBytes(hello);
            await socket.SendAsync(new ArraySegment<byte>(helloBytes), WebSocketMessageType.Text, true, token);
            byte[] buffer = new byte[8192];
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                StringBuilder builder = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                lock (gate)
                    incoming.Enqueue(builder.ToString());
            }
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested)
                Debug.LogWarning("Online relay: " + e.Message);
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }
}
