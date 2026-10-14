using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class OnlineGoogle
{
    public static IEnumerator GetIdToken(string clientId, string clientSecret, Action<string, string> done)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            done?.Invoke(null, "missing client");
            yield break;
        }

        string verifier = RandomKey(32);
        string challenge;
        using (SHA256 sha = SHA256.Create())
            challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));

        string code = null;
        string error = null;
        bool finished = false;
        TcpListener listener = null;
        string redirect = "http://127.0.0.1/";
        string url = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            redirect = "http://127.0.0.1:" + port + "/";
            url = "https://accounts.google.com/o/oauth2/v2/auth"
                + "?client_id=" + UnityWebRequest.EscapeURL(clientId)
                + "&redirect_uri=" + UnityWebRequest.EscapeURL(redirect)
                + "&response_type=code"
                + "&scope=" + UnityWebRequest.EscapeURL("openid email profile")
                + "&code_challenge=" + challenge
                + "&code_challenge_method=S256"
                + "&prompt=select_account";
            Task.Run(() =>
            {
                try
                {
                    using TcpClient client = listener.AcceptTcpClient();
                    using NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = 5000;
                    string request = ReadHttp(stream);
                    code = QueryValue(request, "code");
                    error = QueryValue(request, "error");
                    byte[] page = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nConnection: close\r\n\r\n<html><body><p>You can close this window and return to Ponex.</p></body></html>");
                    stream.Write(page, 0, page.Length);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                finally
                {
                    finished = true;
                }
            });
        }
        catch (Exception e)
        {
            done?.Invoke(null, e.Message);
            yield break;
        }

        Application.OpenURL(url);
        float wait = 0f;
        while (!finished && wait < 120f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (listener != null)
            listener.Stop();

        if (string.IsNullOrEmpty(code))
        {
            done?.Invoke(null, string.IsNullOrEmpty(error) ? "Google sign-in was cancelled." : error);
            yield break;
        }

        string tokenBody = "code=" + UnityWebRequest.EscapeURL(code)
            + "&client_id=" + UnityWebRequest.EscapeURL(clientId)
            + "&redirect_uri=" + UnityWebRequest.EscapeURL(redirect)
            + "&grant_type=authorization_code"
            + "&code_verifier=" + UnityWebRequest.EscapeURL(verifier);
        if (!string.IsNullOrEmpty(clientSecret))
            tokenBody += "&client_secret=" + UnityWebRequest.EscapeURL(clientSecret);
        byte[] bytes = Encoding.UTF8.GetBytes(tokenBody);
        using UnityWebRequest req = new UnityWebRequest("https://oauth2.googleapis.com/token", "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            done?.Invoke(null, GoogleError(body, clientSecret));
            yield break;
        }

        string idToken = OnlineFirebase.ReadString(req.downloadHandler.text, "id_token");
        done?.Invoke(string.IsNullOrEmpty(idToken) ? null : idToken, string.IsNullOrEmpty(idToken) ? "Google did not return a sign-in token." : null);
    }

    static string GoogleError(string body, string clientSecret)
    {
        string detail = ReadDescription(body);
        if (string.IsNullOrEmpty(detail))
            detail = "Google did not return a sign-in token.";
        if (string.IsNullOrEmpty(clientSecret) && detail.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0)
            detail += " Paste that OAuth client's secret into googleClientSecret in OnlineConfig.json.";
        return detail;
    }

    static string ReadDescription(string body)
    {
        if (string.IsNullOrEmpty(body))
            return "";
        string described = Quoted(body, "error_description");
        if (!string.IsNullOrEmpty(described))
            return described;
        return Quoted(body, "error");
    }

    static string Quoted(string json, string key)
    {
        string token = "\"" + key + "\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return "";
        int colon = json.IndexOf(':', start + token.Length);
        if (colon < 0)
            return "";
        int quote = json.IndexOf('"', colon + 1);
        if (quote < 0)
            return "";
        int end = json.IndexOf('"', quote + 1);
        if (end < 0)
            return "";
        return json.Substring(quote + 1, end - quote - 1);
    }

    public static string AccountEmail(string idToken)
    {
        return JwtString(idToken, "email");
    }

    public static string DisplayName(string idToken)
    {
        string name = JwtString(idToken, "name");
        if (string.IsNullOrEmpty(name))
        {
            string mail = JwtString(idToken, "email");
            int at = string.IsNullOrEmpty(mail) ? -1 : mail.IndexOf('@');
            name = at > 0 ? mail.Substring(0, at) : mail;
        }
        if (string.IsNullOrEmpty(name))
            return "";
        StringBuilder clean = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '_')
                clean.Append(c);
        }
        return clean.ToString().Trim();
    }

    static string JwtString(string jwt, string key)
    {
        if (string.IsNullOrEmpty(jwt))
            return "";
        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
            return "";
        try
        {
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            return OnlineFirebase.ReadString(json, key);
        }
        catch (Exception)
        {
            return "";
        }
    }

    static string ReadHttp(NetworkStream stream)
    {
        byte[] buffer = new byte[8192];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer, total, buffer.Length - total);
            if (read <= 0)
                break;
            total += read;
            if (Encoding.ASCII.GetString(buffer, 0, total).Contains("\r\n\r\n"))
                break;
        }
        return Encoding.ASCII.GetString(buffer, 0, total);
    }

    static string QueryValue(string request, string key)
    {
        if (string.IsNullOrEmpty(request))
            return null;
        int lineEnd = request.IndexOf('\r');
        string line = lineEnd > 0 ? request.Substring(0, lineEnd) : request;
        int q = line.IndexOf('?');
        if (q < 0)
            return null;
        int space = line.IndexOf(' ', q);
        string query = space > q ? line.Substring(q + 1, space - q - 1) : line.Substring(q + 1);
        string[] pairs = query.Split('&');
        string prefix = key + "=";
        for (int i = 0; i < pairs.Length; i++)
        {
            if (pairs[i].StartsWith(prefix, StringComparison.Ordinal))
                return Uri.UnescapeDataString(pairs[i].Substring(prefix.Length));
        }
        return null;
    }

    static string RandomKey(int bytes)
    {
        byte[] data = new byte[bytes];
        RandomNumberGenerator.Fill(data);
        return Base64Url(data);
    }

    static string Base64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
