using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class OnlineMember
{
    public string uid;
    public string name;
    public bool guest;
    public int color;
    public string parentUid;
}

[Serializable]
public class OnlineLobbyData
{
    public string code;
    public string hostUid;
    public bool isPublic;
    public bool locked;
    public string relayUrl;
    public string relayKey;
    public int revision;
    public List<OnlineMember> members = new List<OnlineMember>();
}

public class OnlineMode : MonoBehaviour
{
    public static OnlineMode instance;

    public static bool OverlayOpen { get; private set; }
    public static Transform OverlayRoot { get; private set; }
    public static bool LoggedIn => instance != null && instance.loggedIn;
    public static bool InLobby => instance != null && instance.lobby != null && !string.IsNullOrEmpty(instance.lobby.code);
    public static bool IsHost => InLobby && instance.lobby.hostUid == instance.uid;
    public static int PlayerCap => InLobby ? 32 : 8;
    public static bool InputsPaused { get; private set; }

    const string PrefEmail = "ponex.online.email";
    const string PrefPass = "ponex.online.pass";
    const string PrefRefresh = "ponex.online.refresh";
    const string PrefRemember = "ponex.online.remember";
    const string PrefAuto = "ponex.online.auto";
    const string PrefName = "ponex.online.name";

    OnlineConfig config;
    OnlineRelay relay;
    OnlineLobbyData lobby;
    string uid;
    string idToken;
    string username;
    string email;
    bool loggedIn;
    bool busy;
    bool triedAuto;
    float nextPoll;
    bool remember = true;
    bool autoSign = false;
    bool themed;
    Color backgroundColor;
    Image background;
    readonly Dictionary<string, Color> buttonColors = new Dictionary<string, Color>();
    readonly Dictionary<string, Color> outlineColors = new Dictionary<string, Color>();
    readonly Dictionary<int, string> poseCache = new Dictionary<int, string>();
    readonly Dictionary<string, Vector2> selectorCache = new Dictionary<string, Vector2>();
    float nextStateSend;

    GameObject loginRoot;
    bool signupTab;
    InputField emailField;
    InputField passField;
    InputField nameField;
    Text emailError;
    Text nameError;
    Text formError;
    bool googleNaming;
    string googleRefresh;
    OnlineClick googleButton;
    OnlineClick autoToggle;
    OnlineClick rememberToggle;

    GameObject lobbyRoot;
    InputField joinField;
    Text codeText;
    OnlineClick joinButton;
    OnlineClick findButton;
    OnlineClick createButton;
    OnlineClick privateButton;
    OnlineClick publicButton;
    OnlineClick leaveButton;
    Text nameLabel;
    Text saveNotice;
    readonly List<GameObject> rosterRows = new List<GameObject>();
    GameObject rosterRoot;
    RectTransform rosterViewport;
    RectTransform rosterContent;
    Text rosterHeaderLabel;
    ScrollRect rosterScroll;
    bool rosterOpen = true;
    float rosterShown = 1f;
    const float RosterRowHeight = 36f;
    const float RosterWidth = 188f;
    const float RosterHeader = 44f;
    GameObject playerCard;
    string cardUid;

    struct ThemeBackup
    {
        public Image image;
        public Outline outline;
        public Color imageColor;
        public Color outlineColor;
        public Text label;
        public Color labelColor;
        public string labelText;
    }

    readonly List<ThemeBackup> themeBackups = new List<ThemeBackup>();

    public static void Ensure(Database db)
    {
        if (db == null)
            return;
        if (db.GetComponent<OnlineMode>() == null)
            db.gameObject.AddComponent<OnlineMode>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        config = OnlineConfig.Load();
        relay = gameObject.AddComponent<OnlineRelay>();
        remember = PlayerPrefs.GetInt(PrefRemember, 1) == 1;
        autoSign = PlayerPrefs.GetInt(PrefAuto, 0) == 1;
        email = PlayerPrefs.GetString(PrefEmail, "");
        username = PlayerPrefs.GetString(PrefName, "");
    }

    void Start()
    {
        HookOnlineButton();
        if (autoSign && remember && PlayerPrefs.HasKey(PrefRefresh))
            StartCoroutine(RefreshLogin());
        else if (autoSign && remember && !string.IsNullOrEmpty(email) && PlayerPrefs.HasKey(PrefPass))
            StartCoroutine(Login(email, PlayerPrefs.GetString(PrefPass, ""), true));
        triedAuto = true;
    }

    void Update()
    {
        DropLostGuests();
        if (loggedIn && MenuManager.instance != null && MenuManager.instance.IsMenuVisible("Main Menu"))
        {
            if (InLobby && Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + 3f;
                StartCoroutine(RefreshLobby());
            }
            if (InLobby && lobby.locked)
                StartCoroutine(SetLocked(false));
        }
        else if (InLobby && IsHost && !lobby.locked)
        {
            StartCoroutine(SetLocked(true));
        }

        PumpRelay();
        if (IsHost && Database.instance != null && Database.instance.gameStart)
            SendState();
        else if (!IsHost && loggedIn && !InputsPaused)
            SendLocalInput();

        RefreshLobbyButtons();
        RefreshRoster();
    }

    void HookOnlineButton()
    {
        GameObject button = GameObject.Find("Online");
        if (button == null || button.GetComponent<OnlineClick>() != null)
            return;
        OnlineClick click = button.AddComponent<OnlineClick>();
        click.clicked = _ => OnOnlineButton();
    }

    void OnOnlineButton()
    {
        if (OverlayOpen)
            return;
        if (loggedIn)
        {
            StartCoroutine(GoOffline());
            return;
        }
        ShowLogin();
    }

    public void ShowLogin()
    {
        if (loginRoot != null)
        {
            loginRoot.SetActive(true);
            OverlayOpen = true;
            OverlayRoot = loginRoot.transform;
            FillRemembered();
            return;
        }

        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        RectTransform root = OnlineWidgets.Panel(canvas.transform, "Online Login", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 640f), new Color(0.05f, 0.07f, 0.12f, 0.94f));
        loginRoot = root.gameObject;
        OnlineWidgets.CatchClicks(root.gameObject);
        Canvas overlay = loginRoot.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 2000;
        OverlayRoot = root;
        OverlayOpen = true;

        OnlineWidgets.Label(root, "Ponex Online", 36, Color.white).rectTransform.anchoredPosition = new Vector2(0f, 270f);
        OnlineWidgets.Button(root, "Log In", new Vector2(-120f, 210f), new Vector2(200f, 52f), OnlineWidgets.Cyan, _ => ShowTab(false));
        OnlineWidgets.Button(root, "Sign Up", new Vector2(120f, 210f), new Vector2(200f, 52f), OnlineWidgets.Cyan, _ => ShowTab(true));

        emailField = OnlineWidgets.Field(root, "Email", "Email", new Vector2(-80f, 130f), new Vector2(480f, 58f), false);
        emailError = OnlineWidgets.Label(root, "", 18, OnlineWidgets.Red);
        emailError.rectTransform.anchoredPosition = new Vector2(-80f, 90f);
        nameField = OnlineWidgets.Field(root, "Username", "Display Name", new Vector2(-80f, 40f), new Vector2(480f, 58f), false);
        nameError = OnlineWidgets.Label(root, "", 18, OnlineWidgets.Red);
        nameError.rectTransform.anchoredPosition = new Vector2(-80f, 0f);
        passField = OnlineWidgets.Field(root, "Password", "Password", new Vector2(-80f, -50f), new Vector2(480f, 58f), true);
        googleButton = OnlineWidgets.Button(root, "Google", new Vector2(310f, 40f), new Vector2(150f, 238f), Color.white, _ => StartCoroutine(GoogleSignIn()));

        autoToggle = OnlineWidgets.Button(root, "Auto Sign In: Off", new Vector2(0f, -130f), new Vector2(520f, 52f), OnlineWidgets.Cyan, _ => ToggleAuto());
        rememberToggle = OnlineWidgets.Button(root, "Remember Login: On", new Vector2(0f, -190f), new Vector2(520f, 52f), OnlineWidgets.Cyan, _ => ToggleRemember());
        OnlineWidgets.Button(root, "Continue", new Vector2(-130f, -260f), new Vector2(240f, 58f), OnlineWidgets.Green, _ => Submit());
        OnlineWidgets.Button(root, "Cancel", new Vector2(150f, -260f), new Vector2(200f, 58f), OnlineWidgets.Red, _ => CloseLogin());
        formError = OnlineWidgets.Label(root, "", 18, OnlineWidgets.Red);
        formError.rectTransform.anchoredPosition = new Vector2(0f, -310f);

        ShowTab(false);
        FillRemembered();
        RefreshToggles();
    }

    void ShowTab(bool signup)
    {
        signupTab = signup;
        googleNaming = false;
        if (nameField != null)
            nameField.transform.gameObject.SetActive(signup);
        if (emailField != null)
            emailField.transform.gameObject.SetActive(true);
        if (passField != null)
            passField.transform.gameObject.SetActive(true);
        if (nameError != null && !signup)
            nameError.text = "";
        if (emailError != null)
            emailError.text = "";
        if (formError != null)
            formError.text = "";
    }

    void FillRemembered()
    {
        if (!remember)
            return;
        if (emailField != null)
            emailField.text = PlayerPrefs.GetString(PrefEmail, "");
        if (passField != null)
            passField.text = PlayerPrefs.GetString(PrefPass, "");
    }

    void ToggleAuto()
    {
        autoSign = !autoSign;
        PlayerPrefs.SetInt(PrefAuto, autoSign ? 1 : 0);
        PlayerPrefs.Save();
        RefreshToggles();
    }

    void ToggleRemember()
    {
        remember = !remember;
        PlayerPrefs.SetInt(PrefRemember, remember ? 1 : 0);
        if (!remember)
            PlayerPrefs.DeleteKey(PrefPass);
        PlayerPrefs.Save();
        RefreshToggles();
    }

    void RefreshToggles()
    {
        SetToggle(autoToggle, "Auto Sign In: " + (autoSign ? "On" : "Off"));
        SetToggle(rememberToggle, "Remember Login: " + (remember ? "On" : "Off"));
    }

    static void SetToggle(OnlineClick button, string label)
    {
        if (button == null)
            return;
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
            text.text = label;
    }

    void Submit()
    {
        if (busy)
            return;
        string mail = emailField != null ? emailField.text.Trim() : "";
        string pass = passField != null ? passField.text : "";
        string display = nameField != null ? nameField.text.Trim() : "";
        if (emailError != null) emailError.text = "";
        if (nameError != null) nameError.text = "";
        if (formError != null) formError.text = "";
        if (!config.HasFirebase)
        {
            formError.text = "Add your Firebase API key in Resources/OnlineConfig.json";
            return;
        }
        if (googleNaming)
        {
            StartCoroutine(ClaimGoogleUsername(nameField != null ? nameField.text.Trim() : ""));
            return;
        }
        if (signupTab)
            StartCoroutine(SignUp(mail, display, pass));
        else
            StartCoroutine(Login(mail, pass, false));
    }

    IEnumerator SignUp(string mail, string display, string pass)
    {
        busy = true;
        googleRefresh = "";
        bool emailBad = string.IsNullOrEmpty(mail) || !mail.Contains("@");
        bool nameBad = display.Length < 3;
        if (emailBad && emailError != null)
            emailError.text = "Enter an email address.";
        if (nameBad && nameError != null)
            nameError.text = "Display name must be at least 3 characters.";
        if (emailBad || nameBad || pass.Length < 6)
        {
            if (pass.Length < 6 && formError != null)
                formError.text = "Password must be at least 6 characters.";
            busy = false;
            yield break;
        }

        string namePath = "usernames/" + Sanitize(display) + ".json";
        bool nameFree = false;
        yield return OnlineFirebase.ReadObject(config, null, namePath, (text, gen, ok) => nameFree = string.IsNullOrEmpty(text));
        if (!nameFree)
        {
            nameError.text = "That display name is already taken.";
            busy = false;
            yield break;
        }

        string authJson = null;
        string authError = null;
        yield return OnlineFirebase.SignUp(config, mail, pass, (ok, err) => { authJson = ok; authError = err; });
        if (string.IsNullOrEmpty(authJson))
        {
            if (authError != null && authError.IndexOf("EMAIL_EXISTS", StringComparison.OrdinalIgnoreCase) >= 0)
                emailError.text = "That email is already registered.";
            else if (formError != null)
                formError.text = "Could not create the account.";
            busy = false;
            yield break;
        }

        uid = OnlineFirebase.ReadString(authJson, "localId");
        idToken = OnlineFirebase.ReadString(authJson, "idToken");
        bool claimed = false;
        string claim = "{\"uid\":" + OnlineFirebase.Quote(uid) + "}";
        yield return OnlineFirebase.WriteObject(config, idToken, namePath, claim, true, (ok, err) => claimed = ok);
        if (!claimed)
        {
            nameError.text = "That display name is already taken.";
            busy = false;
            yield break;
        }

        username = display;
        email = mail;
        bool saved = false;
        string saveError = null;
        yield return EnsureUserDocument((ok, err) => { saved = ok; saveError = err; });
        NoteSave(saved ? "" : saveError);
        FinishLogin(mail, pass);
        busy = false;
        CloseLogin();
        ApplyTheme();
    }

    IEnumerator Login(string mail, string pass, bool silent)
    {
        busy = true;
        googleRefresh = "";
        if (!config.HasFirebase)
        {
            if (!silent && formError != null)
                formError.text = "Add your Firebase API key in Resources/OnlineConfig.json";
            busy = false;
            yield break;
        }
        string authJson = null;
        string authError = null;
        yield return OnlineFirebase.SignIn(config, mail, pass, (ok, err) => { authJson = ok; authError = err; });
        if (string.IsNullOrEmpty(authJson))
        {
            if (!silent && formError != null)
                formError.text = "Email or password was not accepted.";
            busy = false;
            yield break;
        }
        uid = OnlineFirebase.ReadString(authJson, "localId");
        idToken = OnlineFirebase.ReadString(authJson, "idToken");
        email = mail;
        yield return LoadUserDocument();
        if (string.IsNullOrEmpty(username))
            username = PlayerPrefs.GetString(PrefName, "");
        if (string.IsNullOrEmpty(username))
            username = NameFromEmail(mail);
        bool saved = false;
        string saveError = null;
        yield return EnsureUserDocument((ok, err) => { saved = ok; saveError = err; });
        NoteSave(saved ? "" : saveError);
        FinishLogin(mail, pass);
        busy = false;
        CloseLogin();
        ApplyTheme();
    }

    IEnumerator GoogleSignIn()
    {
        if (busy)
            yield break;
        if (emailError != null) emailError.text = "";
        if (nameError != null) nameError.text = "";
        if (formError != null) formError.text = "";
        if (!config.HasGoogle)
        {
            if (formError != null)
                formError.text = "Add the Google desktop client id in OnlineConfig.json.";
            yield break;
        }

        busy = true;
        string googleId = null;
        string googleError = null;
        yield return OnlineGoogle.GetIdToken(config.googleClientId, config.googleClientSecret, (token, err) => { googleId = token; googleError = err; });
        if (string.IsNullOrEmpty(googleId))
        {
            if (formError != null)
                formError.text = string.IsNullOrEmpty(googleError) ? "Google sign-in did not finish." : googleError;
            busy = false;
            yield break;
        }

        string authJson = null;
        string authError = null;
        yield return OnlineFirebase.SignInWithGoogle(config, googleId, (ok, err) => { authJson = ok; authError = err; });
        if (string.IsNullOrEmpty(authJson))
        {
            if (formError != null)
                formError.text = ShortError(authError, "Firebase did not accept that Google account.");
            busy = false;
            yield break;
        }

        uid = OnlineFirebase.ReadString(authJson, "localId");
        idToken = OnlineFirebase.ReadString(authJson, "idToken");
        googleRefresh = OnlineFirebase.ReadString(authJson, "refreshToken");
        email = OnlineFirebase.ReadString(authJson, "email");
        if (string.IsNullOrEmpty(email))
            email = OnlineGoogle.AccountEmail(googleId);
        yield return LoadUserDocument();
        if (!string.IsNullOrEmpty(username))
        {
            bool saved = false;
            string saveError = null;
            yield return EnsureUserDocument((ok, err) => { saved = ok; saveError = err; });
            NoteSave(saved ? "" : saveError);
            CompleteGoogle();
            yield break;
        }

        string suggested = OnlineGoogle.DisplayName(googleId);
        if (suggested.Length >= 3)
        {
            bool claimed = false;
            string namePath = "usernames/" + Sanitize(suggested) + ".json";
            string claim = "{\"uid\":" + OnlineFirebase.Quote(uid) + "}";
            yield return OnlineFirebase.WriteObject(config, idToken, namePath, claim, true, (ok, err) => claimed = ok);
            if (claimed)
            {
                username = suggested;
                bool saved = false;
                string saveError = null;
                yield return EnsureUserDocument((ok, err) => { saved = ok; saveError = err; });
                NoteSave(saved ? "" : saveError);
                CompleteGoogle();
                yield break;
            }
        }

        googleNaming = true;
        if (emailField != null) emailField.gameObject.SetActive(false);
        if (passField != null) passField.gameObject.SetActive(false);
        if (nameField != null)
        {
            nameField.gameObject.SetActive(true);
            nameField.text = suggested;
        }
        if (formError != null)
            formError.text = "Choose a display name, then press Continue.";
        busy = false;
    }

    void CompleteGoogle()
    {
        FinishLogin(email, "");
        busy = false;
        CloseLogin();
        ApplyTheme();
    }

    IEnumerator ClaimGoogleUsername(string display)
    {
        if (busy)
            yield break;
        busy = true;
        if (nameError != null) nameError.text = "";
        if (display.Length < 3)
        {
            if (nameError != null)
                nameError.text = "Display name must be at least 3 characters.";
            busy = false;
            yield break;
        }

        string namePath = "usernames/" + Sanitize(display) + ".json";
        bool claimed = false;
        string claim = "{\"uid\":" + OnlineFirebase.Quote(uid) + "}";
        yield return OnlineFirebase.WriteObject(config, idToken, namePath, claim, true, (ok, err) => claimed = ok);
        if (!claimed)
        {
            if (nameError != null)
                nameError.text = "That display name is already taken.";
            busy = false;
            yield break;
        }

        username = display;
        bool saved = false;
        string saveError = null;
        yield return EnsureUserDocument((ok, err) => { saved = ok; saveError = err; });
        NoteSave(saved ? "" : saveError);
        googleNaming = false;
        FinishLogin(email, "");
        busy = false;
        CloseLogin();
        ApplyTheme();
    }

    IEnumerator RefreshLogin()
    {
        string savedRefresh = PlayerPrefs.GetString(PrefRefresh, "");
        if (!config.HasFirebase || string.IsNullOrEmpty(savedRefresh))
            yield break;
        string json = null;
        yield return OnlineFirebase.Refresh(config, savedRefresh, (ok, err) => json = ok);
        if (string.IsNullOrEmpty(json))
            yield break;
        idToken = OnlineFirebase.ReadString(json, "id_token");
        uid = OnlineFirebase.ReadString(json, "user_id");
        googleRefresh = OnlineFirebase.ReadString(json, "refresh_token");
        if (string.IsNullOrEmpty(googleRefresh))
            googleRefresh = savedRefresh;
        email = PlayerPrefs.GetString(PrefEmail, "");
        username = PlayerPrefs.GetString(PrefName, "");
        if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(uid))
            yield break;
        yield return LoadUserDocument();
        if (!string.IsNullOrEmpty(username))
            yield return EnsureUserDocument(null);
        FinishLogin(email, "");
        ApplyTheme();
    }

    IEnumerator LoadUserDocument()
    {
        if (string.IsNullOrEmpty(uid))
            yield break;
        string raw = "";
        yield return OnlineFirebase.ReadDocument(config, idToken, "users/" + uid, (text, ok) => raw = text);
        string savedName = OnlineFirebase.ReadField(raw, "username");
        string savedMail = OnlineFirebase.ReadField(raw, "email");
        if (!string.IsNullOrEmpty(savedName))
            username = savedName;
        if (!string.IsNullOrEmpty(savedMail))
            email = savedMail;
    }

    IEnumerator EnsureUserDocument(Action<bool, string> done)
    {
        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(username))
        {
            done?.Invoke(false, "missing profile");
            yield break;
        }
        bool ok = false;
        string err = null;
        yield return OnlineFirebase.WriteUser(config, idToken, uid, email ?? "", username, (wrote, error) =>
        {
            ok = wrote;
            err = error;
        });
        done?.Invoke(ok, err);
    }

    void FinishLogin(string mail, string pass)
    {
        if (string.IsNullOrEmpty(username))
            username = NameFromEmail(mail);
        if (string.IsNullOrEmpty(username))
            username = NameFromEmail(email);
        loggedIn = true;
        PlayerPrefs.SetString(PrefEmail, mail);
        PlayerPrefs.SetString(PrefName, username);
        if (remember && !string.IsNullOrEmpty(pass))
            PlayerPrefs.SetString(PrefPass, pass);
        else
            PlayerPrefs.DeleteKey(PrefPass);
        if (remember && !string.IsNullOrEmpty(googleRefresh))
            PlayerPrefs.SetString(PrefRefresh, googleRefresh);
        else if (string.IsNullOrEmpty(googleRefresh))
            PlayerPrefs.DeleteKey(PrefRefresh);
        PlayerPrefs.Save();
        Database db = Database.instance;
        if (db != null)
        {
            db.onlineMatch = true;
            db.maxPlayers = 32;
            db.EnsureBallSlotsPublic();
            EnsureColors(db);
            StampLocalNames();
        }
        EnsureLobbyPanel();
    }

    IEnumerator GoOffline()
    {
        if (InLobby)
            yield return LeaveLobbyRoutine();
        loggedIn = false;
        uid = "";
        idToken = "";
        InputsPaused = false;
        if (relay != null)
            relay.Disconnect();
        RestoreTheme();
        if (lobbyRoot != null)
            lobbyRoot.SetActive(false);
        if (nameLabel != null)
            nameLabel.gameObject.SetActive(false);
        Database db = Database.instance;
        if (db != null)
        {
            db.onlineMatch = false;
            db.maxPlayers = 8;
        }
    }

    void CloseLogin()
    {
        OverlayOpen = false;
        OverlayRoot = null;
        if (loginRoot != null)
            loginRoot.SetActive(false);
    }

    static void PinTopLeft(Component widget, Vector2 pos, Vector2 size)
    {
        if (widget == null)
            return;
        RectTransform rt = widget.GetComponent<RectTransform>();
        if (rt == null)
            return;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        SelectorClickable.Ensure(widget.gameObject);
    }

    void EnsureLobbyPanel()
    {
        if (lobbyRoot != null)
        {
            lobbyRoot.SetActive(true);
            return;
        }
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;
        RectTransform root = OnlineWidgets.Panel(canvas.transform, "Online Lobby", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 340f), new Color(0.05f, 0.07f, 0.12f, 0.82f));
        root.pivot = new Vector2(0f, 1f);
        lobbyRoot = root.gameObject;
        OnlineWidgets.CatchClicks(root.gameObject);
        joinField = OnlineWidgets.Field(root, "Join Code", "Join code", new Vector2(20f, -16f), new Vector2(380f, 52f), false);
        PinTopLeft(joinField, new Vector2(20f, -16f), new Vector2(380f, 52f));
        joinButton = OnlineWidgets.Button(root, "Join", new Vector2(20f, -78f), new Vector2(185f, 52f), OnlineWidgets.Green, _ => StartCoroutine(JoinLobby(joinField.text)));
        findButton = OnlineWidgets.Button(root, "Find", new Vector2(215f, -78f), new Vector2(185f, 52f), new Color(0.25f, 0.5f, 0.95f, 1f), _ => StartCoroutine(FindLobby()));
        createButton = OnlineWidgets.Button(root, "Create", new Vector2(20f, -140f), new Vector2(380f, 52f), OnlineWidgets.Yellow, _ => StartCoroutine(CreateLobby()));
        PinTopLeft(joinButton, new Vector2(20f, -78f), new Vector2(185f, 52f));
        PinTopLeft(findButton, new Vector2(215f, -78f), new Vector2(185f, 52f));
        PinTopLeft(createButton, new Vector2(20f, -140f), new Vector2(380f, 52f));
        codeText = OnlineWidgets.Label(root, "", 28, OnlineWidgets.Yellow);
        codeText.alignment = TextAnchor.MiddleCenter;
        codeText.rectTransform.anchorMin = codeText.rectTransform.anchorMax = new Vector2(0f, 1f);
        codeText.rectTransform.pivot = new Vector2(0f, 1f);
        codeText.rectTransform.anchoredPosition = new Vector2(20f, -16f);
        codeText.rectTransform.sizeDelta = new Vector2(380f, 52f);
        codeText.gameObject.SetActive(false);
        privateButton = OnlineWidgets.Button(root, "Private", new Vector2(20f, -78f), new Vector2(185f, 52f), OnlineWidgets.Red, _ => StartCoroutine(SetPublic(false)));
        publicButton = OnlineWidgets.Button(root, "Public", new Vector2(215f, -78f), new Vector2(185f, 52f), OnlineWidgets.Green, _ => StartCoroutine(SetPublic(true)));
        leaveButton = OnlineWidgets.Button(root, "Leave Lobby", new Vector2(20f, -140f), new Vector2(380f, 52f), OnlineWidgets.Red, _ => StartCoroutine(LeaveLobbyRoutine()));
        PinTopLeft(privateButton, new Vector2(20f, -78f), new Vector2(185f, 52f));
        PinTopLeft(publicButton, new Vector2(215f, -78f), new Vector2(185f, 52f));
        PinTopLeft(leaveButton, new Vector2(20f, -140f), new Vector2(380f, 52f));
        privateButton.gameObject.SetActive(false);
        publicButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        saveNotice = OnlineWidgets.Label(root, "", 16, OnlineWidgets.Red);
        saveNotice.alignment = TextAnchor.MiddleLeft;
        saveNotice.rectTransform.anchorMin = saveNotice.rectTransform.anchorMax = new Vector2(0f, 1f);
        saveNotice.rectTransform.pivot = new Vector2(0f, 1f);
        saveNotice.rectTransform.anchoredPosition = new Vector2(20f, -200f);
        saveNotice.rectTransform.sizeDelta = new Vector2(380f, 64f);
        PlaceUsername();
    }

    bool ShowOnlineChrome()
    {
        if (!loggedIn)
            return false;
        Database db = Database.instance;
        return db == null || (!db.gameStart && !db.startingGame && !db.storyPlaying);
    }

    void PlaceUsername()
    {
        if (!loggedIn)
            return;
        if (!ShowOnlineChrome())
        {
            if (nameLabel != null)
                nameLabel.gameObject.SetActive(false);
            return;
        }
        if (string.IsNullOrEmpty(username))
            username = NameFromEmail(email);
        if (string.IsNullOrEmpty(username))
            return;
        if (nameLabel == null)
        {
            Canvas canvas = FindCanvas();
            if (canvas == null)
                return;
            nameLabel = OnlineWidgets.Label(canvas.transform, username, 28, new Color(0.35f, 0.65f, 1f, 1f));
            nameLabel.alignment = TextAnchor.MiddleRight;
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rt = nameLabel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(520f, 40f);
            Canvas over = nameLabel.gameObject.AddComponent<Canvas>();
            over.overrideSorting = true;
            over.sortingOrder = 450;
        }
        nameLabel.gameObject.SetActive(true);
        nameLabel.text = username;
        nameLabel.rectTransform.anchoredPosition = new Vector2(-(RosterWidth + 28f), -8f);
        if (saveNotice != null)
            saveNotice.text = saveNoticeText;
    }

    string saveNoticeText;

    static string NameFromEmail(string mail)
    {
        if (string.IsNullOrEmpty(mail))
            return "";
        int at = mail.IndexOf('@');
        string name = at > 0 ? mail.Substring(0, at) : mail;
        return name.Trim();
    }

    void NoteSave(string error)
    {
        if (string.IsNullOrEmpty(error))
        {
            saveNoticeText = "";
        }
        else if (error.IndexOf("no firebase token", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            saveNoticeText = "Sign-in did not return a Firebase token, so the user profile was not saved.";
        }
        else if (error.IndexOf("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase) >= 0
            || error.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            saveNoticeText = "Firestore blocked the save. In Firebase, open Firestore Rules and publish access for signed-in users.";
        }
        else
        {
            saveNoticeText = ShortError(error, "Could not save to Firestore.");
        }
        if (saveNotice != null)
            saveNotice.text = saveNoticeText;
    }

    void RefreshLobbyButtons()
    {
        if (!loggedIn || lobbyRoot == null)
            return;
        if (!ShowOnlineChrome())
        {
            lobbyRoot.SetActive(false);
            if (nameLabel != null)
                nameLabel.gameObject.SetActive(false);
            return;
        }
        lobbyRoot.SetActive(true);
        PlaceUsername();
        bool inside = InLobby;
        joinField.gameObject.SetActive(!inside);
        joinButton.gameObject.SetActive(!inside);
        findButton.gameObject.SetActive(!inside);
        createButton.gameObject.SetActive(!inside);
        codeText.gameObject.SetActive(inside);
        privateButton.gameObject.SetActive(inside && IsHost);
        publicButton.gameObject.SetActive(inside && IsHost);
        leaveButton.gameObject.SetActive(inside);
        if (!inside)
        {
            bool canJoin = joinField != null && !string.IsNullOrEmpty(joinField.text.Trim());
            OnlineWidgets.SetButtonColors(joinButton, OnlineWidgets.Green, OnlineWidgets.BlueOutline, canJoin);
            OnlineWidgets.SetButtonColors(findButton, new Color(0.25f, 0.5f, 0.95f, 1f), OnlineWidgets.BlueOutline, true);
            OnlineWidgets.SetButtonColors(createButton, OnlineWidgets.Yellow, OnlineWidgets.Purple, true);
        }
        else
        {
            codeText.text = lobby.code;
            OnlineWidgets.SetButtonColors(privateButton, OnlineWidgets.Red, OnlineWidgets.Purple, !lobby.isPublic);
            OnlineWidgets.SetButtonColors(publicButton, OnlineWidgets.Green, OnlineWidgets.BlueOutline, lobby.isPublic);
        }
        ApplyMenuGates();
        PlaceLobby();
    }

    void PlaceLobby()
    {
        if (lobbyRoot == null)
            return;
        RectTransform panel = lobbyRoot.GetComponent<RectTransform>();
        if (panel == null)
            return;
        Vector2 pos = new Vector2(24f, -24f);
        if (ModeScreenOpen())
        {
            RectTransform back = VisibleBack();
            RectTransform parent = panel.parent as RectTransform;
            if (back != null && parent != null)
            {
                Canvas canvas = parent.GetComponentInParent<Canvas>();
                Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector3[] corners = new Vector3[4];
                back.GetWorldCorners(corners);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local))
                {
                    float anchorY = Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, panel.anchorMin.y);
                    pos.y = Mathf.Min(-24f, local.y - anchorY - 12f);
                }
            }
        }
        panel.anchoredPosition = pos;
    }

    static bool ModeScreenOpen()
    {
        MenuManager mm = MenuManager.instance;
        if (mm == null || mm.IsMenuVisible("Main Menu"))
            return false;
        if (mm.IsMenuVisible("Character Select") || mm.IsMenuVisible("Team Select") || mm.IsMenuVisible("Position Select") || mm.IsMenuVisible("Ball Select") || mm.IsMenuVisible("Level Select"))
            return true;
        Database db = Database.instance;
        return db != null && (db.gametype == Gametype.Arcade || db.gametype == Gametype.Vs || db.gametype == Gametype.Coop);
    }

    static RectTransform VisibleBack()
    {
        OpenMenuOnClick[] backs = FindObjectsByType<OpenMenuOnClick>(FindObjectsInactive.Exclude);
        RectTransform best = null;
        float bestTop = float.MinValue;
        for (int i = 0; i < backs.Length; i++)
        {
            if (backs[i] == null || backs[i].gameObject.name != "Back" || !backs[i].gameObject.activeInHierarchy)
                continue;
            RectTransform rt = backs[i].GetComponent<RectTransform>();
            if (rt == null)
                continue;
            Vector3 world = rt.TransformPoint(new Vector3(rt.rect.center.x, rt.rect.yMax, 0f));
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            if (screen.x > Screen.width * 0.4f)
                continue;
            if (screen.y > bestTop)
            {
                bestTop = screen.y;
                best = rt;
            }
        }
        return best;
    }

    void RefreshRoster()
    {
        if (!InLobby || !loggedIn || !ShowOnlineChrome())
        {
            ClearRoster();
            return;
        }
        Canvas canvas = FindCanvas();
        if (canvas == null || lobby.members == null)
            return;
        EnsureRosterShell(canvas.transform);
        rosterRoot.SetActive(true);

        int count = lobby.members.Count;
        while (rosterRows.Count < count)
        {
            Text row = OnlineWidgets.Label(rosterContent, "", 18, Color.white);
            row.text = "";
            row.raycastTarget = true;
            RectTransform rowRt = row.rectTransform;
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.sizeDelta = new Vector2(RosterWidth, RosterRowHeight);
            Text name = OnlineWidgets.Label(row.transform, "", 18, Color.white);
            name.gameObject.name = "Name";
            name.alignment = TextAnchor.MiddleCenter;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 8;
            name.resizeTextMaxSize = 18;
            name.raycastTarget = false;
            RectTransform nameRt = name.rectTransform;
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(30f, 0f);
            nameRt.offsetMax = new Vector2(-6f, 0f);
            SelectorClickable.Ensure(row.gameObject, 32f);
            rosterRows.Add(row.gameObject);
            row.gameObject.AddComponent<OnlineRosterClick>().label = name;
        }

        float listHeight = count * RosterRowHeight;
        float limit = RosterLimit(canvas);
        float openHeight = Mathf.Min(listHeight, limit);
        float goal = rosterOpen ? 1f : 0f;
        rosterShown = Mathf.MoveTowards(rosterShown, goal, Time.unscaledDeltaTime * 4.5f);
        float fade = rosterShown * rosterShown * (3f - 2f * rosterShown);
        float viewHeight = openHeight * fade;

        RectTransform rootRt = rosterRoot.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(RosterWidth, RosterHeader + viewHeight);
        rosterViewport.sizeDelta = new Vector2(RosterWidth, Mathf.Max(0f, viewHeight));
        rosterContent.sizeDelta = new Vector2(RosterWidth, listHeight);
        float slide = (1f - fade) * openHeight;
        if (rosterScroll != null && fade > 0.98f)
            rosterContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(rosterContent.anchoredPosition.y, 0f, Mathf.Max(0f, listHeight - openHeight)));
        else
            rosterContent.anchoredPosition = new Vector2(0f, slide);
        if (rosterScroll != null)
            rosterScroll.enabled = fade > 0.98f && listHeight > openHeight + 1f;
        if (rosterHeaderLabel != null)
            rosterHeaderLabel.text = (rosterOpen ? "Players  −" : "Players  +") + "  " + count;

        for (int i = 0; i < rosterRows.Count; i++)
        {
            bool show = i < count;
            rosterRows[i].SetActive(show);
            if (!show)
                continue;
            OnlineMember member = lobby.members[i];
            Text root = rosterRows[i].GetComponent<Text>();
            if (root != null)
                root.text = "";
            Text text = rosterRows[i].transform.Find("Name")?.GetComponent<Text>();
            if (text == null)
                continue;
            text.text = member.name;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = 18;
            text.color = member.uid == lobby.hostUid ? new Color(1f, 0.82f, 0.25f, 1f) : Color.white;
            Image ball = null;
            Image[] images = rosterRows[i].GetComponentsInChildren<Image>(true);
            for (int n = 0; n < images.Length; n++)
            {
                if (images[n] != null && images[n].gameObject.name == "Ball")
                {
                    ball = images[n];
                    break;
                }
            }
            if (ball == null)
            {
                GameObject iconGo = new GameObject("Ball", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.layer = 5;
                iconGo.transform.SetParent(rosterRows[i].transform, false);
                ball = iconGo.GetComponent<Image>();
            }
            Database db = Database.instance;
            if (db != null && db.playerColors != null && db.playerColors.Count > 0)
            {
                PlayerColors colors = db.playerColors[Mathf.Clamp(member.color, 0, db.playerColors.Count - 1)];
                ball.sprite = colors.sprite != null ? colors.sprite : OnlineWidgets.MenuSprite();
                ball.color = colors.color;
            }
            RectTransform rt = rosterRows[i].GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(RosterWidth, RosterRowHeight);
            rt.anchoredPosition = new Vector2(0f, -i * RosterRowHeight);
            RectTransform nameRt = text.rectTransform;
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = Vector2.one;
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.offsetMin = new Vector2(30f, 0f);
            nameRt.offsetMax = new Vector2(-6f, 0f);
            ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            ball.rectTransform.pivot = new Vector2(0f, 0.5f);
            ball.rectTransform.anchoredPosition = new Vector2(6f, 0f);
            ball.rectTransform.sizeDelta = new Vector2(18f, 18f);
            SelectorClickable.Ensure(rosterRows[i]);
            Collider col = rosterRows[i].GetComponent<Collider>();
            if (col != null)
                col.enabled = fade > 0.2f && i * RosterRowHeight < viewHeight - 4f;
            rosterRows[i].GetComponent<OnlineRosterClick>().uid = member.uid;
        }
        ScaleSelectors(count);
    }

    void EnsureRosterShell(Transform parent)
    {
        if (rosterRoot != null)
            return;
        RectTransform root = OnlineWidgets.Panel(parent, "Lobby Roster", new Vector2(1f, 1f), new Vector2(-16f, -56f), new Vector2(RosterWidth, RosterHeader), new Color(0.05f, 0.07f, 0.12f, 0.9f));
        root.pivot = new Vector2(1f, 1f);
        rosterRoot = root.gameObject;

        OnlineClick header = OnlineWidgets.Button(root, "Players", Vector2.zero, new Vector2(RosterWidth, RosterHeader), new Color(0.12f, 0.14f, 0.2f, 1f), _ => rosterOpen = !rosterOpen);
        PinTopLeft(header, Vector2.zero, new Vector2(RosterWidth, RosterHeader));
        rosterHeaderLabel = header.GetComponentInChildren<Text>();

        GameObject viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewGo.layer = 5;
        viewGo.transform.SetParent(root, false);
        rosterViewport = viewGo.GetComponent<RectTransform>();
        rosterViewport.anchorMin = rosterViewport.anchorMax = new Vector2(0f, 1f);
        rosterViewport.pivot = new Vector2(0f, 1f);
        rosterViewport.anchoredPosition = new Vector2(0f, -RosterHeader);
        rosterViewport.sizeDelta = new Vector2(RosterWidth, 0f);

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.layer = 5;
        contentGo.transform.SetParent(rosterViewport, false);
        rosterContent = contentGo.GetComponent<RectTransform>();
        rosterContent.anchorMin = new Vector2(0f, 1f);
        rosterContent.anchorMax = new Vector2(1f, 1f);
        rosterContent.pivot = new Vector2(0.5f, 1f);
        rosterContent.anchoredPosition = Vector2.zero;

        rosterScroll = root.gameObject.AddComponent<ScrollRect>();
        rosterScroll.viewport = rosterViewport;
        rosterScroll.content = rosterContent;
        rosterScroll.horizontal = false;
        rosterScroll.vertical = true;
        rosterScroll.movementType = ScrollRect.MovementType.Clamped;
        rosterScroll.scrollSensitivity = 28f;
        rosterScroll.inertia = true;
    }

    static float RosterLimit(Canvas canvas)
    {
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        float height = canvasRt != null ? canvasRt.rect.height : 800f;
        if (height < 200f)
            height = 800f;
        return Mathf.Max(RosterRowHeight, height - RosterHeader - 80f);
    }

    void ClearRoster()
    {
        if (rosterRoot != null)
            rosterRoot.SetActive(false);
        for (int i = 0; i < rosterRows.Count; i++)
        {
            if (rosterRows[i] != null)
                rosterRows[i].SetActive(false);
        }
    }

    public void OpenPlayerCard(string memberUid, int clicker)
    {
        if (!IsHost || !IsPrimary(clicker) || string.IsNullOrEmpty(memberUid) || memberUid == uid)
            return;
        OnlineMember member = FindMember(memberUid);
        if (member == null)
            return;
        cardUid = memberUid;
        if (playerCard != null)
            Destroy(playerCard);
        Canvas canvas = FindCanvas();
        RectTransform root = OnlineWidgets.Panel(canvas.transform, "Player Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 320f), new Color(0.05f, 0.07f, 0.12f, 0.96f));
        playerCard = root.gameObject;
        Canvas over = playerCard.AddComponent<Canvas>();
        over.overrideSorting = true;
        over.sortingOrder = 1900;
        OnlineWidgets.Label(root, member.name, 28, Color.white).rectTransform.anchoredPosition = new Vector2(0f, 110f);
        OnlineWidgets.Button(root, "Kick", new Vector2(0f, 40f), new Vector2(280f, 52f), OnlineWidgets.Red, _ => StartCoroutine(Kick(cardUid)));
        OnlineWidgets.Button(root, "Promote", new Vector2(0f, -30f), new Vector2(280f, 52f), OnlineWidgets.Yellow, _ => StartCoroutine(Promote(cardUid)));
        OnlineWidgets.Button(root, "Cancel", new Vector2(0f, -100f), new Vector2(280f, 52f), OnlineWidgets.Gray, _ => CloseCard());
    }

    void CloseCard()
    {
        cardUid = "";
        if (playerCard != null)
            Destroy(playerCard);
    }

    IEnumerator CreateLobby()
    {
        if (busy || InLobby)
            yield break;
        busy = true;
        OnlineLobbyData made = new OnlineLobbyData();
        made.code = Code();
        made.hostUid = uid;
        made.relayKey = Guid.NewGuid().ToString("N");
        made.relayUrl = config.relayUrl;
        made.members = LocalMembers();
        made.revision = 1;
        bool ok = false;
        string err = null;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(made.code), JsonUtility.ToJson(made), true, (wrote, error) =>
        {
            ok = wrote;
            err = error;
        });
        busy = false;
        if (!ok)
        {
            NoteSave(string.IsNullOrEmpty(err) ? "Could not create the lobby." : err);
            yield break;
        }
        lobby = made;
        NoteSave("");
        ConnectRelay(true);
        StampLocalNames();
    }

    IEnumerator JoinLobby(string code)
    {
        code = string.IsNullOrEmpty(code) ? "" : code.Trim().ToUpperInvariant();
        if (busy || code.Length < 4)
            yield break;
        busy = true;
        string json = "";
        yield return OnlineFirebase.ReadObject(config, idToken, LobbyPath(code), (text, gen, ok) => json = text);
        if (string.IsNullOrEmpty(json))
        {
            busy = false;
            yield break;
        }
        OnlineLobbyData found = JsonUtility.FromJson<OnlineLobbyData>(json);
        if (found == null || found.locked || found.members == null || found.members.Count >= 32)
        {
            busy = false;
            yield break;
        }
        if (found.members.Exists(m => m != null && m.uid == uid))
        {
            lobby = found;
            busy = false;
            ConnectRelay(false);
            yield break;
        }
        found.members.Add(new OnlineMember { uid = uid, name = username, guest = false, color = found.members.Count, parentUid = "" });
        found.revision++;
        bool wrote = false;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(code), JsonUtility.ToJson(found), false, (ok, err) => wrote = ok);
        if (wrote)
        {
            lobby = found;
            ConnectRelay(false);
            StampLocalNames();
        }
        busy = false;
    }

    IEnumerator FindLobby()
    {
        if (busy)
            yield break;
        busy = true;
        string index = "";
        yield return OnlineFirebase.ReadObject(config, idToken, "public/lobbies.json", (text, gen, ok) => index = text);
        busy = false;
        if (string.IsNullOrEmpty(index))
            yield break;
        string[] parts = index.Split(new[] { ',', '"', '[', ']', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length >= 4)
            {
                yield return JoinLobby(parts[i]);
                if (InLobby)
                    yield break;
            }
        }
    }

    IEnumerator RefreshLobby()
    {
        if (!InLobby)
            yield break;
        string json = "";
        yield return OnlineFirebase.ReadObject(config, idToken, LobbyPath(lobby.code), (text, gen, ok) => json = text);
        if (string.IsNullOrEmpty(json))
            yield break;
        OnlineLobbyData fresh = JsonUtility.FromJson<OnlineLobbyData>(json);
        if (fresh != null && fresh.revision >= lobby.revision)
            lobby = fresh;
        if (IsHost)
        {
            EnsureRemotePlayers();
            yield return PublishRoster();
        }
    }

    void EnsureRemotePlayers()
    {
        Database db = Database.instance;
        if (!IsHost || lobby == null || lobby.members == null || db == null)
            return;
        for (int i = 0; i < lobby.members.Count; i++)
        {
            OnlineMember member = lobby.members[i];
            if (member == null || member.uid == uid || member.parentUid == uid)
                continue;
            bool exists = db.players != null && db.players.Exists(p => p != null && p.nickName == member.name);
            if (exists)
                continue;
            ControllerLink.spawningRemote = true;
            GameObject go = new GameObject("Remote " + member.name);
            go.AddComponent<PlayerInput>();
            ControllerLink link = go.AddComponent<ControllerLink>();
            Player spawned = db.players != null ? db.players.Find(p => p != null && p.cLink == link) : null;
            if (spawned != null)
            {
                spawned.nickName = member.name;
                spawned.computer = false;
            }
        }
    }

    IEnumerator PublishRoster()
    {
        if (!IsHost)
            yield break;
        List<OnlineMember> mine = LocalMembers();
        for (int i = lobby.members.Count - 1; i >= 0; i--)
        {
            OnlineMember member = lobby.members[i];
            if (member != null && (member.uid == uid || member.parentUid == uid))
                lobby.members.RemoveAt(i);
        }
        lobby.members.AddRange(mine);
        lobby.revision++;
        if (lobby.isPublic)
            yield return OnlineFirebase.WriteObject(config, idToken, "public/lobbies.json", "[\"" + lobby.code + "\"]", false, null);
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
    }

    IEnumerator SetPublic(bool value)
    {
        if (!IsHost)
            yield break;
        lobby.isPublic = value;
        lobby.revision++;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
    }

    IEnumerator SetLocked(bool value)
    {
        if (!IsHost || lobby.locked == value)
            yield break;
        lobby.locked = value;
        lobby.revision++;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
    }

    IEnumerator LeaveLobbyRoutine()
    {
        if (!InLobby)
            yield break;
        if (IsHost)
        {
            OnlineMember next = null;
            for (int i = 0; i < lobby.members.Count; i++)
            {
                OnlineMember member = lobby.members[i];
                if (member != null && !member.guest && member.uid != uid)
                {
                    next = member;
                    break;
                }
            }
            lobby.members.RemoveAll(m => m != null && (m.uid == uid || m.parentUid == uid));
            if (next != null)
                lobby.hostUid = next.uid;
            lobby.revision++;
            if (lobby.members.Count == 0)
                lobby.locked = true;
            yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
        }
        else
        {
            lobby.members.RemoveAll(m => m != null && (m.uid == uid || m.parentUid == uid));
            lobby.revision++;
            yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
        }
        lobby = null;
        if (relay != null)
            relay.Disconnect();
        CloseCard();
        ScaleSelectors(1);
        ConvertLocalsToCpu();
    }

    public System.Collections.IEnumerator LeaveAndQuit()
    {
        yield return LeaveLobbyRoutine();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Kick(string memberUid)
    {
        if (!IsHost)
            yield break;
        lobby.members.RemoveAll(m => m != null && (m.uid == memberUid || m.parentUid == memberUid));
        lobby.revision++;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
        CloseCard();
    }

    IEnumerator Promote(string memberUid)
    {
        if (!IsHost)
            yield break;
        lobby.hostUid = memberUid;
        lobby.revision++;
        yield return OnlineFirebase.WriteObject(config, idToken, LobbyPath(lobby.code), JsonUtility.ToJson(lobby), false, null);
        CloseCard();
        if (relay != null)
            ConnectRelay(false);
    }

    void ConnectRelay(bool host)
    {
        if (relay == null || lobby == null || !config.HasRelay)
            return;
        relay.Connect(config.relayUrl, lobby.code, lobby.relayKey, uid, host);
    }

    void PumpRelay()
    {
        if (relay == null)
            return;
        while (relay.TryPop(out string json))
        {
            string type = OnlineFirebase.ReadString(json, "t");
            if (type == "act")
                ApplyAction(json);
            else if (type == "state" && !IsHost)
                ApplyState(json);
            else if (type == "in" && IsHost)
                ApplyInput(json);
            else if (type == "vote")
                NoteVote(json);
        }
    }

    void SendState()
    {
        if (Time.unscaledTime < nextStateSend)
            return;
        nextStateSend = Time.unscaledTime + 0.1f;
        Database db = Database.instance;
        if (db == null || db.players == null || relay == null)
            return;
        StringBuilder body = new StringBuilder();
        body.Append("{\"t\":\"state\",\"p\":[");
        bool any = false;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.spawnedPlayer == null)
                continue;
            Transform t = p.spawnedPlayer.transform;
            string pose = t.position.x.ToString("0.##") + "," + t.position.y.ToString("0.##") + "," + t.position.z.ToString("0.##")
                + "," + t.rotation.x.ToString("0.##") + "," + t.rotation.y.ToString("0.##") + "," + t.rotation.z.ToString("0.##") + "," + t.rotation.w.ToString("0.##")
                + "," + p.currentHealth;
            if (poseCache.TryGetValue(p.index, out string prev) && prev == pose)
                continue;
            poseCache[p.index] = pose;
            if (any) body.Append(',');
            any = true;
            body.Append("{\"i\":").Append(p.index).Append(",\"d\":").Append(OnlineFirebase.Quote(pose)).Append('}');
        }
        body.Append("]}");
        if (any)
            relay.Send(body.ToString());
    }

    void ApplyState(string json)
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
            return;
        int cursor = 0;
        while (true)
        {
            int iKey = json.IndexOf("\"i\":", cursor, StringComparison.Ordinal);
            if (iKey < 0)
                break;
            int iStart = iKey + 4;
            int iEnd = json.IndexOf(',', iStart);
            int index = int.Parse(json.Substring(iStart, iEnd - iStart));
            int dKey = json.IndexOf("\"d\":\"", iEnd, StringComparison.Ordinal);
            if (dKey < 0)
                break;
            int dStart = dKey + 5;
            int dEnd = json.IndexOf('"', dStart);
            string[] parts = json.Substring(dStart, dEnd - dStart).Split(',');
            cursor = dEnd + 1;
            Player p = db.players.Find(x => x != null && x.index == index);
            if (p == null || p.spawnedPlayer == null || parts.Length < 8)
                continue;
            Rigidbody body = p.spawnedPlayer.GetComponent<Rigidbody>();
            if (body != null)
                body.isKinematic = true;
            p.spawnedPlayer.transform.SetPositionAndRotation(
                new Vector3(Parse(parts[0]), Parse(parts[1]), Parse(parts[2])),
                new Quaternion(Parse(parts[3]), Parse(parts[4]), Parse(parts[5]), Parse(parts[6])));
            p.currentHealth = Mathf.RoundToInt(Parse(parts[7]));
        }
    }

    void SendLocalInput()
    {
        if (relay == null || !relay.Connected)
            return;
        Database db = Database.instance;
        if (db == null || db.players == null)
            return;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.cLink == null || p.computer)
                continue;
            if (!IsLocalAccountPlayer(p))
                continue;
            ControllerButtons move = p.cLink["Move"];
            Vector2 stick = move != null ? move.value : Vector2.zero;
            int bits = 0;
            if (Pressed(p.cLink, "Jump")) bits |= 1;
            if (Pressed(p.cLink, "Interact")) bits |= 2;
            if (Pressed(p.cLink, "Menu")) bits |= 4;
            if (Pressed(p.cLink, "Sprint")) bits |= 8;
            string packet = "{\"t\":\"in\",\"uid\":" + OnlineFirebase.Quote(UidFor(p))
                + ",\"x\":" + stick.x.ToString("0.##")
                + ",\"y\":" + stick.y.ToString("0.##")
                + ",\"b\":" + bits + "}";
            relay.SendDelta("in" + p.index, packet);
        }
        SendSelectors();
    }

    void SendSelectors()
    {
        Database db = Database.instance;
        if (db == null || db.players == null || db.gameStart)
            return;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.pso == null || !IsLocalAccountPlayer(p))
                continue;
            RectTransform rt = p.pso.GetComponent<RectTransform>();
            if (rt == null)
                continue;
            string key = UidFor(p);
            string pose = rt.anchoredPosition.x.ToString("0.#") + "," + rt.anchoredPosition.y.ToString("0.#");
            if (selectorCache.TryGetValue(key, out Vector2 prev) && (prev - rt.anchoredPosition).sqrMagnitude < 0.25f)
                continue;
            selectorCache[key] = rt.anchoredPosition;
            relay.Send("{\"t\":\"sel\",\"uid\":" + OnlineFirebase.Quote(key) + ",\"d\":" + OnlineFirebase.Quote(pose) + "}");
        }
    }

    void ApplyInput(string json)
    {
        string who = OnlineFirebase.ReadString(json, "uid");
        float x = Parse(OnlineFirebase.ReadString(json, "x"));
        float y = Parse(OnlineFirebase.ReadString(json, "y"));
        int bits = 0;
        int b = json.IndexOf("\"b\":", StringComparison.Ordinal);
        if (b >= 0)
            int.TryParse(json.Substring(b + 4).TrimEnd('}'), out bits);
        ControllerLink link = LinkForUid(who);
        if (link == null)
            return;
        link.remoteDrive = true;
        link.remoteMove = new Vector2(x, y);
        link.remoteJump = (bits & 1) != 0;
        link.remoteInteract = (bits & 2) != 0;
        link.remoteMenu = (bits & 4) != 0;
        link.remoteSprint = (bits & 8) != 0;
    }

    public static void BroadcastClick(string path, int player)
    {
        if (instance == null || instance.relay == null || !InLobby)
            return;
        string from = instance.UidForPlayer(player);
        instance.relay.Send("{\"t\":\"act\",\"path\":" + OnlineFirebase.Quote(path)
            + ",\"player\":" + player
            + ",\"from\":" + OnlineFirebase.Quote(from) + "}");
    }

    void ApplyAction(string json)
    {
        string from = OnlineFirebase.ReadString(json, "from");
        if (IsHost && relay != null)
            relay.Send(json);
        if (from == uid)
            return;
        string path = OnlineFirebase.ReadString(json, "path");
        int player = 0;
        int key = json.IndexOf("\"player\":", StringComparison.Ordinal);
        if (key >= 0)
            int.TryParse(json.Substring(key + 9).Split(',')[0], out player);
        GameObject go = GameObject.Find(path);
        if (go != null)
            go.SendMessage("OnClick", player, SendMessageOptions.DontRequireReceiver);
    }

    readonly Dictionary<string, bool> votes = new Dictionary<string, bool>();

    public void CastVote(bool yes)
    {
        if (!loggedIn || !IsPrimaryPlayer(uid))
            return;
        if (relay != null)
            relay.Send("{\"t\":\"vote\",\"uid\":" + OnlineFirebase.Quote(uid) + ",\"yes\":" + (yes ? "true" : "false") + "}");
        NoteVote("{\"uid\":\"" + uid + "\",\"yes\":" + (yes ? "true" : "false") + "}");
    }

    void NoteVote(string json)
    {
        string who = OnlineFirebase.ReadString(json, "uid");
        if (string.IsNullOrEmpty(who) || IsGuestUid(who))
            return;
        votes[who] = json.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0 && json.IndexOf("\"yes\":true", StringComparison.Ordinal) >= 0;
        if (!IsHost || lobby == null || lobby.members == null)
            return;
        int voters = 0;
        int yes = 0;
        for (int i = 0; i < lobby.members.Count; i++)
        {
            OnlineMember member = lobby.members[i];
            if (member == null || member.guest)
                continue;
            voters++;
            if (votes.TryGetValue(member.uid, out bool choice) && choice)
                yes++;
        }
        if (voters > 0 && yes * 2 > voters)
        {
            Database db = Database.instance;
            if (db != null)
                db.EndOnlineMatch();
        }
    }

    public static void FillOpenPositions()
    {
        Database db = Database.instance;
        if (db == null || db.players == null || !InLobby)
            return;
        Facing[] sides = { Facing.Up, Facing.Down, Facing.Left, Facing.Right };
        for (int p = 0; p < db.players.Count; p++)
        {
            Player player = db.players[p];
            if (player == null)
                continue;
            bool taken = false;
            for (int o = 0; o < db.players.Count; o++)
            {
                Player other = db.players[o];
                if (other == null || other == player)
                    continue;
                if (other.position == player.position && other.facing == player.facing && player.position >= 0)
                    taken = true;
            }
            if (player.position >= 0 && !taken)
                continue;
            bool seated = false;
            for (int s = 0; s < sides.Length && !seated; s++)
            {
                for (int slot = 0; slot < 2; slot++)
                {
                    bool open = true;
                    for (int o = 0; o < db.players.Count; o++)
                    {
                        Player other = db.players[o];
                        if (other != null && other != player && other.position == slot && other.facing == sides[s])
                            open = false;
                    }
                    if (!open)
                        continue;
                    player.facing = sides[s];
                    player.position = slot;
                    seated = true;
                    break;
                }
            }
        }
        if (TeamSelect.instance != null)
            TeamSelect.instance.EnforceSameSideTeams();
    }

    public static bool IsPrimary(int playerIndex)
    {
        if (instance == null || !instance.loggedIn)
            return true;
        Database db = Database.instance;
        if (db == null || db.players == null)
            return false;
        Player p = db.players.Find(x => x != null && x.index == playerIndex);
        return p != null && p.nickName == instance.username;
    }

    public static int TryAdoptLostAccount(ControllerLink newbie)
    {
        if (instance == null || !instance.loggedIn || newbie == null)
            return -1;
        Database db = Database.instance;
        if (db == null || db.players == null)
            return -1;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.cLink == null || p.cLink == newbie)
                continue;
            if (p.nickName != instance.username || !p.cLink.DeviceLost)
                continue;
            if (newbie.TryGetDevice(out InputDevice device))
                p.cLink.Reclaim(device);
            UnityEngine.Object.Destroy(newbie.gameObject);
            return p.index;
        }
        return -1;
    }

    void DropLostGuests()
    {
        Database db = Database.instance;
        if (!loggedIn || db == null || db.players == null)
            return;
        for (int i = db.players.Count - 1; i >= 0; i--)
        {
            Player p = db.players[i];
            if (p == null || p.cLink == null)
                continue;
            if (p.nickName != null && p.nickName.EndsWith("_guest") && p.cLink.DeviceLost)
                db.DisconnectPlayer(p.index);
        }
    }

    void StampLocalNames()
    {
        Database db = Database.instance;
        if (db == null || db.players == null || string.IsNullOrEmpty(username))
            return;
        bool namedPrimary = false;
        int guests = 0;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.computer || p.cLink == null)
                continue;
            if (!namedPrimary)
            {
                p.nickName = username;
                namedPrimary = true;
            }
            else
            {
                guests++;
                p.nickName = username + "_guest";
                if (guests > 1)
                    p.nickName += guests.ToString();
            }
        }
    }

    List<OnlineMember> LocalMembers()
    {
        StampLocalNames();
        List<OnlineMember> list = new List<OnlineMember>();
        Database db = Database.instance;
        if (db == null || db.players == null)
        {
            list.Add(new OnlineMember { uid = uid, name = username, guest = false, color = 0 });
            return list;
        }
        int color = 0;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.computer)
                continue;
            bool guest = p.nickName != username;
            list.Add(new OnlineMember
            {
                uid = guest ? uid + ":g" + color : uid,
                name = string.IsNullOrEmpty(p.nickName) ? username : p.nickName,
                guest = guest,
                color = color,
                parentUid = guest ? uid : ""
            });
            color++;
        }
        if (list.Count == 0)
            list.Add(new OnlineMember { uid = uid, name = username, guest = false, color = 0 });
        return list;
    }

    void ApplyMenuGates()
    {
        if (!loggedIn)
            return;
        int people = InLobby && lobby.members != null ? lobby.members.Count : 1;
        bool canHost = !InLobby || IsHost;
        SetMenuButton("Arcade", canHost && people <= 32);
        SetMenuButton("Vs", canHost && people <= 8);
        SetMenuButton("Tornament", canHost && people <= 32);
        SetMenuButton("Options", true);
        SetMenuButton("Quit Game", true);
        SetMenuButton("Story", !InLobby);
        SetMenuButton("Builder", !InLobby);
        SetMenuButton("Online", true);
        SwapBackButtons();
        EnsurePauseExtras();
    }

    void SetMenuButton(string name, bool active)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
            return;
        ButtonInteraction interaction = go.GetComponent<ButtonInteraction>();
        if (interaction != null)
        {
            if (active) interaction.Activate();
            else interaction.Deactivate();
        }
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            col.enabled = active;
    }

    void SwapBackButtons()
    {
        if (!InLobby)
            return;
        OpenMenuOnClick[] backs = FindObjectsByType<OpenMenuOnClick>(FindObjectsInactive.Exclude);
        for (int i = 0; i < backs.Length; i++)
        {
            if (backs[i] == null || backs[i].gameObject.name != "Back")
                continue;
            if (IsHost || MenuManager.instance != null && MenuManager.instance.IsMenuVisible("Main Menu"))
            {
                backs[i].enabled = true;
                continue;
            }
            backs[i].enabled = false;
            Text text = backs[i].GetComponentInChildren<Text>();
            if (text != null)
                text.text = "Leave Lobby";
            if (backs[i].GetComponent<OnlineClick>() == null)
            {
                OnlineClick click = backs[i].gameObject.AddComponent<OnlineClick>();
                click.clicked = player =>
                {
                    if (!IsPrimary(player))
                        return;
                    StartCoroutine(LeaveLobbyRoutine());
                };
            }
        }
    }

    bool pauseExtras;
    void EnsurePauseExtras()
    {
        Database db = Database.instance;
        if (db == null || !InLobby)
            return;
        if (!db.pauseMenuOpen)
        {
            pauseExtras = false;
            return;
        }
        if (pauseExtras)
            return;
        MenuManager mm = MenuManager.instance;
        if (mm == null)
            return;
        MenuClass pause = mm.FindMenu("Pause");
        if (pause == null || pause.holder == null)
            return;
        pauseExtras = true;
        if (!IsHost)
        {
            OnlineWidgets.Button(pause.holder.transform, "Leave Lobby", new Vector2(0f, -220f), new Vector2(250f, 58f), OnlineWidgets.Red, player =>
            {
                if (!IsPrimary(player))
                    return;
                InputsPaused = true;
            });
        }
        OnlineWidgets.Button(pause.holder.transform, "Vote End", new Vector2(-80f, -280f), new Vector2(140f, 52f), OnlineWidgets.Green, player =>
        {
            if (IsPrimary(player))
                CastVote(true);
        });
        OnlineWidgets.Button(pause.holder.transform, "Vote Stay", new Vector2(80f, -280f), new Vector2(140f, 52f), OnlineWidgets.Red, player =>
        {
            if (IsPrimary(player))
                CastVote(false);
        });
    }

    void ApplyTheme()
    {
        if (themed)
            return;
        themed = true;
        themeBackups.Clear();
        string[] names = { "Online", "Arcade", "Vs", "Tornament", "Story", "Builder" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = GameObject.Find(names[i]);
            if (go == null)
                continue;
            Image image = go.GetComponent<Image>();
            Outline outline = go.GetComponent<Outline>();
            Text label = go.GetComponentInChildren<Text>();
            ThemeBackup backup = new ThemeBackup
            {
                image = image,
                outline = outline,
                imageColor = image != null ? image.color : Color.white,
                outlineColor = outline != null ? outline.effectColor : OnlineWidgets.BlueOutline,
                label = label,
                labelColor = label != null ? label.color : Color.black,
                labelText = label != null ? label.text : ""
            };
            themeBackups.Add(backup);
            if (image != null)
                image.color = OnlineWidgets.Yellow;
            if (outline != null)
                outline.effectColor = OnlineWidgets.Purple;
            ButtonInteraction interaction = go.GetComponent<ButtonInteraction>();
            if (interaction != null)
                interaction.SetBaseColors(OnlineWidgets.Yellow, OnlineWidgets.Purple);
            if (names[i] == "Online" && label != null)
            {
                label.text = "Offline";
                label.color = OnlineWidgets.Red;
            }
        }
        background = FindBackground();
        if (background != null)
        {
            backgroundColor = background.color;
            background.color = new Color(0.18f, 0.08f, 0.28f, 1f);
        }
        EnsureColors(Database.instance);
    }

    void RestoreTheme()
    {
        for (int i = 0; i < themeBackups.Count; i++)
        {
            ThemeBackup backup = themeBackups[i];
            if (backup.image != null)
                backup.image.color = backup.imageColor;
            if (backup.outline != null)
                backup.outline.effectColor = backup.outlineColor;
            if (backup.label != null)
            {
                backup.label.text = backup.labelText;
                backup.label.color = backup.labelColor;
            }
            if (backup.image != null)
            {
                ButtonInteraction interaction = backup.image.GetComponent<ButtonInteraction>();
                if (interaction != null)
                    interaction.SetBaseColors(backup.imageColor, backup.outlineColor);
            }
        }
        themeBackups.Clear();
        if (background != null)
            background.color = backgroundColor;
        themed = false;
    }

    static Image FindBackground()
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Exclude);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].gameObject.name == "Background")
                return images[i];
        }
        return null;
    }

    public static void EnsureColors(Database db)
    {
        if (db == null || db.playerColors == null || db.playerColors.Count == 0)
            return;
        Sprite sprite = db.playerColors[0].sprite;
        while (db.playerColors.Count < 32)
        {
            int i = db.playerColors.Count;
            PlayerColors colors = new PlayerColors();
            colors.color = Color.HSVToRGB((i * 0.618034f) % 1f, 0.72f, 0.95f);
            colors.sprite = sprite;
            colors.EnsureScheme(true);
            db.playerColors.Add(colors);
        }
    }

    static void ScaleSelectors(int count)
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
            return;
        float scale = Mathf.Lerp(1f, 0.42f, Mathf.Clamp01((count - 1) / 31f));
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && p.pso != null)
                p.pso.transform.localScale = Vector3.one * scale;
        }
    }

    void ConvertLocalsToCpu()
    {
        Database db = Database.instance;
        if (db == null)
            return;
        db.DisconnectPlayer(PrimaryIndex());
    }

    int PrimaryIndex()
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
            return -1;
        Player p = db.players.Find(x => x != null && x.nickName == username);
        return p != null ? p.index : -1;
    }

    bool IsLocalAccountPlayer(Player p)
    {
        return p != null && (p.nickName == username || (p.nickName != null && p.nickName.StartsWith(username + "_guest")));
    }

    string UidFor(Player p)
    {
        if (p == null || p.nickName == username)
            return uid;
        return uid + ":" + p.nickName;
    }

    string UidForPlayer(int index)
    {
        Database db = Database.instance;
        Player p = db != null && db.players != null ? db.players.Find(x => x != null && x.index == index) : null;
        return UidFor(p);
    }

    bool IsPrimaryPlayer(string who)
    {
        return who == uid;
    }

    bool IsGuestUid(string who)
    {
        return who != null && who.Contains(":g");
    }

    ControllerLink LinkForUid(string who)
    {
        Database db = Database.instance;
        if (db == null || db.players == null)
            return null;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && UidFor(p) == who)
                return p.cLink;
        }
        return null;
    }

    OnlineMember FindMember(string memberUid)
    {
        if (lobby == null || lobby.members == null)
            return null;
        for (int i = 0; i < lobby.members.Count; i++)
        {
            if (lobby.members[i] != null && lobby.members[i].uid == memberUid)
                return lobby.members[i];
        }
        return null;
    }

    static bool Pressed(ControllerLink link, string action)
    {
        if (link == null)
            return false;
        ControllerButtons button = link[action];
        return button != null && button.isPressed;
    }

    static float Parse(string value)
    {
        float number;
        if (!float.TryParse(value, out number))
            return 0f;
        return number;
    }

    static string ShortError(string raw, string fallback)
    {
        if (string.IsNullOrEmpty(raw))
            return fallback;
        int message = raw.IndexOf("\"message\":\"", StringComparison.Ordinal);
        if (message >= 0)
        {
            int start = message + 11;
            int end = raw.IndexOf('"', start);
            if (end > start)
                return raw.Substring(start, Mathf.Min(160, end - start));
        }
        return raw.Length > 160 ? raw.Substring(0, 160) : raw;
    }

    static string Sanitize(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.Trim().ToLowerInvariant().Replace(" ", "");
    }

    static string Code()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] chars = new char[6];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
        return new string(chars);
    }

    static string LobbyPath(string code)
    {
        return "lobbies/" + code + ".json";
    }

    static Canvas FindCanvas()
    {
        Database db = Database.instance;
        if (db != null && db.playerSelectors != null)
        {
            Canvas[] canvases = db.playerSelectors.GetComponentsInParent<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject != db.playerSelectors.gameObject)
                    return canvases[i];
            }
        }
        return FindAnyObjectByType<Canvas>();
    }

    public static void ResumeInputs()
    {
        InputsPaused = false;
    }
}

public class OnlineRosterClick : MonoBehaviour
{
    public string uid;
    public Text label;

    public void OnClick(int player)
    {
        if (OnlineMode.instance != null)
            OnlineMode.instance.OpenPlayerCard(uid, player);
    }
}
