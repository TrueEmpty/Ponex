using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class OnlineFirebase
{
    public static IEnumerator SignUp(OnlineConfig config, string email, string password, Action<string, string> done)
    {
        string url = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + config.apiKey;
        string body = "{\"email\":" + Quote(email) + ",\"password\":" + Quote(password) + ",\"returnSecureToken\":true}";
        yield return Post(url, body, null, done);
    }

    public static IEnumerator SignInWithGoogle(OnlineConfig config, string idToken, Action<string, string> done)
    {
        string url = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=" + config.apiKey;
        string postBody = "id_token=" + UnityWebRequest.EscapeURL(idToken) + "&providerId=google.com";
        string requestUri = "http://localhost";
        string body = "{\"postBody\":" + Quote(postBody)
            + ",\"requestUri\":" + Quote(requestUri)
            + ",\"returnSecureToken\":true}";
        yield return Post(url, body, null, done);
    }

    public static IEnumerator Refresh(OnlineConfig config, string refreshToken, Action<string, string> done)
    {
        string url = "https://securetoken.googleapis.com/v1/token?key=" + config.apiKey;
        string body = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refreshToken);
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            done?.Invoke(req.downloadHandler.text, null);
        else
            done?.Invoke(null, req.downloadHandler.text);
    }

    public static IEnumerator SignIn(OnlineConfig config, string email, string password, Action<string, string> done)
    {
        string url = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + config.apiKey;
        string body = "{\"email\":" + Quote(email) + ",\"password\":" + Quote(password) + ",\"returnSecureToken\":true}";
        yield return Post(url, body, null, done);
    }

    public static IEnumerator ReadDocument(OnlineConfig config, string token, string path, Action<string, bool> done)
    {
        string url = WithKey(config, DocumentUrl(config, path));
        using UnityWebRequest req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token.Trim());
        yield return req.SendWebRequest();
        bool missing = req.responseCode == 404;
        string text = req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : "";
        done?.Invoke(text, missing || req.result == UnityWebRequest.Result.Success);
    }

    public static IEnumerator WriteUser(OnlineConfig config, string token, string uid, string email, string username, Action<bool, string> done)
    {
        if (string.IsNullOrEmpty(token))
        {
            done?.Invoke(false, "no firebase token");
            yield break;
        }
        token = token.Trim();
        string body = "{\"fields\":{\"email\":{\"stringValue\":" + Quote(email)
            + "},\"username\":{\"stringValue\":" + Quote(username) + "}}}";
        string create = WithKey(config, CollectionUrl(config, "users") + "?documentId=" + UnityWebRequest.EscapeURL(uid));
        int code = 0;
        string text = "";
        bool ok = false;
        yield return SendJson(create, "POST", body, token, (status, response, success) =>
        {
            code = status;
            text = response;
            ok = success;
        });
        if (!ok && (code == 409 || code == 403 || code == 404))
        {
            string patch = WithKey(config, DocumentUrl(config, "users/" + uid)
                + "?updateMask.fieldPaths=email&updateMask.fieldPaths=username");
            yield return SendJson(patch, "PATCH", body, token, (status, response, success) =>
            {
                code = status;
                text = response;
                ok = success;
            });
        }
        done?.Invoke(ok, ok ? "" : text);
    }

    public static IEnumerator ReadObject(OnlineConfig config, string token, string path, Action<string, long, bool> done)
    {
        string url = WithKey(config, DocumentUrl(config, path));
        using UnityWebRequest req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token.Trim());
        yield return req.SendWebRequest();
        bool missing = req.responseCode == 404;
        string text = "";
        if (req.result == UnityWebRequest.Result.Success)
            text = ReadDataField(req.downloadHandler.text);
        done?.Invoke(text, 0, missing || req.result == UnityWebRequest.Result.Success);
    }

    public static IEnumerator WriteObject(OnlineConfig config, string token, string path, string json, bool createOnly, Action<bool, string> done)
    {
        SplitDocument(path, out string parent, out string id);
        string fields = "{\"fields\":{\"data\":{\"stringValue\":" + Quote(json) + "}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(fields);
        string url = WithKey(config, createOnly
            ? CollectionUrl(config, parent) + "?documentId=" + UnityWebRequest.EscapeURL(id)
            : DocumentUrl(config, path) + "?updateMask.fieldPaths=data");

        using UnityWebRequest req = new UnityWebRequest(url, createOnly ? "POST" : "PATCH");
        req.redirectLimit = 0;
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token.Trim());
        yield return req.SendWebRequest();
        if (req.responseCode >= 300 && req.responseCode < 400)
        {
            string location = req.GetResponseHeader("Location");
            if (!string.IsNullOrEmpty(location))
            {
                yield return SendJson(location, createOnly ? "POST" : "PATCH", fields, token, (status, response, success) =>
                {
                    done?.Invoke(success, success ? "" : response);
                });
                yield break;
            }
        }
        if (!createOnly && req.responseCode == 404)
        {
            yield return WriteObject(config, token, path, json, true, done);
            yield break;
        }

        bool ok = req.result == UnityWebRequest.Result.Success;
        string err = req.downloadHandler != null ? req.downloadHandler.text : "";
        if (!ok && string.IsNullOrEmpty(err))
            err = req.error;
        done?.Invoke(ok, ok ? "" : err);
    }

    static string CollectionUrl(OnlineConfig config, string collection)
    {
        return "https://firestore.googleapis.com/v1/projects/" + config.projectId
            + "/databases/(default)/documents/" + collection;
    }

    static string DocumentUrl(OnlineConfig config, string path)
    {
        SplitDocument(path, out string parent, out string id);
        return CollectionUrl(config, parent) + "/" + UnityWebRequest.EscapeURL(id);
    }

    static string WithKey(OnlineConfig config, string url)
    {
        if (config == null || string.IsNullOrEmpty(config.apiKey) || string.IsNullOrEmpty(url) || url.IndexOf("key=", StringComparison.Ordinal) >= 0)
            return url;
        return url + (url.IndexOf('?') >= 0 ? "&" : "?") + "key=" + UnityWebRequest.EscapeURL(config.apiKey);
    }

    static void SplitDocument(string path, out string parent, out string id)
    {
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - 5);
        int slash = string.IsNullOrEmpty(path) ? -1 : path.LastIndexOf('/');
        if (slash < 0)
        {
            parent = path;
            id = "_";
            return;
        }
        parent = path.Substring(0, slash);
        id = path.Substring(slash + 1);
    }

    public static string ReadField(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return "";
        string token = "\"" + key + "\":{\"stringValue\":\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start >= 0)
            return Unquote(json, start + token.Length);
        return ReadString(ReadDataField(json), key);
    }

    static string Unquote(string json, int start)
    {
        StringBuilder value = new StringBuilder();
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '\\' && i + 1 < json.Length)
            {
                char next = json[i + 1];
                if (next == '"' || next == '\\' || next == '/')
                    value.Append(next);
                else if (next == 'n')
                    value.Append('\n');
                else if (next == 'r')
                    value.Append('\r');
                else if (next == 't')
                    value.Append('\t');
                else
                    value.Append(next);
                i++;
                continue;
            }
            if (c == '"')
                break;
            value.Append(c);
        }
        return value.ToString();
    }

    static IEnumerator SendJson(string url, string method, string body, string token, Action<int, string, bool> done)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        using UnityWebRequest req = new UnityWebRequest(url, method);
        req.redirectLimit = 0;
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token.Trim());
        yield return req.SendWebRequest();
        int status = (int)req.responseCode;
        string response = req.downloadHandler != null ? req.downloadHandler.text : "";
        if (status >= 300 && status < 400)
        {
            string location = req.GetResponseHeader("Location");
            if (!string.IsNullOrEmpty(location))
            {
                yield return SendJson(location, method, body, token, done);
                yield break;
            }
        }
        bool ok = req.result == UnityWebRequest.Result.Success;
        if (!ok && string.IsNullOrEmpty(response))
            response = req.error;
        done?.Invoke(status, response, ok);
    }

    static string ReadDataField(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "";
        string token = "\"data\":{\"stringValue\":\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start >= 0)
            return Unquote(json, start + token.Length);
        token = "\"stringValue\":\"";
        start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return "";
        return Unquote(json, start + token.Length);
    }

    static IEnumerator Post(string url, string body, string token, Action<string, string> done)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Firebase " + token);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            done?.Invoke(req.downloadHandler.text, null);
        else
            done?.Invoke(null, req.downloadHandler.text);
    }

    public static string Quote(string value)
    {
        if (value == null)
            return "\"\"";
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static string ReadString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            return "";
        string token = "\"" + key + "\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return "";
        int i = start + token.Length;
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        if (i >= json.Length || json[i] != ':')
            return "";
        i++;
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        if (i >= json.Length || json[i] != '"')
            return "";
        i++;
        int end = json.IndexOf('"', i);
        if (end < 0)
            return "";
        return json.Substring(i, end - i);
    }

    public static IEnumerator ReadLobby(OnlineConfig config, string token, string code, Action<OnlineLobbyData, bool, string> done)
    {
        string raw = "";
        bool missing = false;
        yield return ReadDocument(config, token, "lobbies/" + code, (text, ok) =>
        {
            raw = text;
            missing = string.IsNullOrEmpty(text);
        });
        if (missing)
        {
            done?.Invoke(null, true, "");
            yield break;
        }
        OnlineLobbyData lobby = ParseLobby(raw);
        if (lobby != null && string.IsNullOrEmpty(lobby.code))
            lobby.code = code;
        done?.Invoke(lobby, false, lobby == null ? raw : "");
    }

    public static IEnumerator WriteLobby(OnlineConfig config, string token, OnlineLobbyData lobby, Action<bool, string> done, bool createNew = false)
    {
        if (lobby == null || string.IsNullOrEmpty(lobby.code))
        {
            done?.Invoke(false, "missing lobby");
            yield break;
        }
        string body = LobbyDocument(lobby);
        string patch = WithKey(config, DocumentUrl(config, "lobbies/" + lobby.code) + "?" + LobbyMask());
        string create = WithKey(config, CollectionUrl(config, "lobbies") + "?documentId=" + UnityWebRequest.EscapeURL(lobby.code));
        int status = 0;
        string text = "";
        bool ok = false;
        if (createNew)
        {
            yield return SendJson(create, "POST", body, token, (code, response, success) =>
            {
                status = code;
                text = response;
                ok = success;
            });
            if (!ok && status == 409)
            {
                yield return SendJson(patch, "PATCH", body, token, (code, response, success) =>
                {
                    status = code;
                    text = response;
                    ok = success;
                });
            }
        }
        else
        {
            yield return SendJson(patch, "PATCH", body, token, (code, response, success) =>
            {
                status = code;
                text = response;
                ok = success;
            });
            if (!ok && status == 404)
            {
                yield return SendJson(create, "POST", body, token, (code, response, success) =>
                {
                    status = code;
                    text = response;
                    ok = success;
                });
            }
        }
        done?.Invoke(ok, ok ? "" : text);
    }

    public static IEnumerator DeleteLobby(OnlineConfig config, string token, string code, Action<bool, string> done)
    {
        string url = WithKey(config, DocumentUrl(config, "lobbies/" + code));
        using UnityWebRequest req = UnityWebRequest.Delete(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token.Trim());
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success || req.responseCode == 404;
        string err = req.downloadHandler != null ? req.downloadHandler.text : req.error;
        done?.Invoke(ok, ok ? "" : err);
    }

    public static IEnumerator ListPublicLobbies(OnlineConfig config, string token, Action<List<OnlineLobbyData>, string> done)
    {
        string url = WithKey(config, "https://firestore.googleapis.com/v1/projects/" + config.projectId
            + "/databases/(default)/documents:runQuery");
        string body = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"lobbies\"}],\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"isPublic\"},\"op\":\"EQUAL\",\"value\":{\"booleanValue\":true}}}}}";
        string text = "";
        bool ok = false;
        yield return SendJson(url, "POST", body, token, (status, response, success) =>
        {
            text = response;
            ok = success;
        });
        List<OnlineLobbyData> list = new List<OnlineLobbyData>();
        if (!ok)
        {
            done?.Invoke(list, text);
            yield break;
        }
        int search = 0;
        while (search < text.Length)
        {
            int doc = text.IndexOf("\"document\"", search, StringComparison.Ordinal);
            if (doc < 0)
                break;
            int start = text.IndexOf('{', doc);
            if (start < 0)
                break;
            int end = MatchBrace(text, start);
            if (end < 0)
                break;
            OnlineLobbyData lobby = ParseLobby(text.Substring(start, end - start + 1));
            if (lobby != null && !string.IsNullOrEmpty(lobby.code))
                list.Add(lobby);
            search = end + 1;
        }
        done?.Invoke(list, "");
    }

    static string LobbyMask()
    {
        string[] fields = { "code", "hostUid", "isPublic", "locked", "relayUrl", "relayKey", "revision", "members", "data" };
        StringBuilder mask = new StringBuilder();
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                mask.Append('&');
            mask.Append("updateMask.fieldPaths=");
            mask.Append(fields[i]);
        }
        return mask.ToString();
    }

    static string LobbyDocument(OnlineLobbyData lobby)
    {
        StringBuilder members = new StringBuilder();
        members.Append("{\"arrayValue\":{\"values\":[");
        bool any = false;
        if (lobby.members != null)
        {
            for (int i = 0; i < lobby.members.Count; i++)
            {
                OnlineMember member = lobby.members[i];
                if (member == null)
                    continue;
                if (any)
                    members.Append(',');
                any = true;
                members.Append("{\"mapValue\":{\"fields\":{");
                members.Append("\"uid\":{\"stringValue\":").Append(Quote(member.uid)).Append("},");
                members.Append("\"name\":{\"stringValue\":").Append(Quote(member.name)).Append("},");
                members.Append("\"guest\":{\"booleanValue\":").Append(member.guest ? "true" : "false").Append("},");
                members.Append("\"color\":{\"integerValue\":\"").Append(member.color).Append("\"},");
                members.Append("\"parentUid\":{\"stringValue\":").Append(Quote(member.parentUid)).Append("}");
                members.Append("}}}");
            }
        }
        members.Append("]}}");

        StringBuilder body = new StringBuilder();
        body.Append("{\"fields\":{");
        body.Append("\"code\":{\"stringValue\":").Append(Quote(lobby.code)).Append("},");
        body.Append("\"hostUid\":{\"stringValue\":").Append(Quote(lobby.hostUid)).Append("},");
        body.Append("\"isPublic\":{\"booleanValue\":").Append(lobby.isPublic ? "true" : "false").Append("},");
        body.Append("\"locked\":{\"booleanValue\":").Append(lobby.locked ? "true" : "false").Append("},");
        body.Append("\"relayUrl\":{\"stringValue\":").Append(Quote(lobby.relayUrl)).Append("},");
        body.Append("\"relayKey\":{\"stringValue\":").Append(Quote(lobby.relayKey)).Append("},");
        body.Append("\"revision\":{\"integerValue\":\"").Append(lobby.revision).Append("\"},");
        body.Append("\"members\":").Append(members);
        body.Append("}}");
        return body.ToString();
    }

    static OnlineLobbyData ParseLobby(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        if (json.IndexOf("\"members\":{\"arrayValue\"", StringComparison.Ordinal) >= 0
            || json.IndexOf("\"hostUid\":{\"stringValue\"", StringComparison.Ordinal) >= 0)
        {
            OnlineLobbyData lobby = new OnlineLobbyData();
            lobby.code = ReadField(json, "code");
            if (string.IsNullOrEmpty(lobby.code))
                lobby.code = CodeFromName(json);
            lobby.hostUid = ReadField(json, "hostUid");
            lobby.relayUrl = ReadField(json, "relayUrl");
            lobby.relayKey = ReadField(json, "relayKey");
            lobby.isPublic = ReadBool(json, "isPublic");
            lobby.locked = ReadBool(json, "locked");
            lobby.revision = ReadInt(json, "revision");
            lobby.members = ReadMembers(json);
            return lobby;
        }

        string blob = ReadDataField(json);
        if (string.IsNullOrEmpty(blob) || blob.TrimStart().Length == 0 || blob.TrimStart()[0] != '{')
            return null;
        return JsonUtility.FromJson<OnlineLobbyData>(blob);
    }

    static string CodeFromName(string json)
    {
        string token = "/documents/lobbies/";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return "";
        start += token.Length;
        int end = start;
        while (end < json.Length && json[end] != '"' && json[end] != '/')
            end++;
        return json.Substring(start, end - start);
    }

    static bool ReadBool(string json, string key)
    {
        string token = "\"" + key + "\":{\"booleanValue\":";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return false;
        start += token.Length;
        while (start < json.Length && char.IsWhiteSpace(json[start]))
            start++;
        return start < json.Length && json[start] == 't';
    }

    static int ReadInt(string json, string key)
    {
        string token = "\"" + key + "\":{\"integerValue\":\"";
        int start = json.IndexOf(token, StringComparison.Ordinal);
        if (start < 0)
            return 0;
        start += token.Length;
        int end = start;
        while (end < json.Length && char.IsDigit(json[end]))
            end++;
        if (end == start || !int.TryParse(json.Substring(start, end - start), out int value))
            return 0;
        return value;
    }

    static List<OnlineMember> ReadMembers(string json)
    {
        List<OnlineMember> members = new List<OnlineMember>();
        int membersAt = json.IndexOf("\"members\"", StringComparison.Ordinal);
        if (membersAt < 0)
            return members;
        int values = json.IndexOf("\"values\"", membersAt, StringComparison.Ordinal);
        if (values < 0)
            return members;
        int array = json.IndexOf('[', values);
        if (array < 0)
            return members;
        int i = array + 1;
        while (i < json.Length)
        {
            while (i < json.Length && json[i] != '{' && json[i] != ']')
                i++;
            if (i >= json.Length || json[i] == ']')
                break;
            int end = MatchBrace(json, i);
            if (end < 0)
                break;
            string slice = json.Substring(i, end - i + 1);
            members.Add(new OnlineMember
            {
                uid = ReadField(slice, "uid"),
                name = ReadField(slice, "name"),
                guest = ReadBool(slice, "guest"),
                color = ReadInt(slice, "color"),
                parentUid = ReadField(slice, "parentUid")
            });
            i = end + 1;
        }
        return members;
    }

    static int MatchBrace(string json, int open)
    {
        int depth = 0;
        for (int i = open; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"')
            {
                i++;
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\')
                        i++;
                    i++;
                }
                continue;
            }
            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }
        return -1;
    }
}
