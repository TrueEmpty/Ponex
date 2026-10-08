using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridControl : MonoBehaviour
{
    Database db;
    public Outline ol;
    MenuManager mm;
    public string group = "Default";

    public Vector2 position = Vector2.zero;
    public bool canSelect = true;
    public float orderValue = 0;
    public Vector2Int gridSize;
    public Vector2Int curPos;
    public List<GridControl> fc;

    public List<int> playersInControl = new List<int>(); //-10 for all
    Color baseEffectColor;

    public bool basedOnPlayerState = false;
    public bool canLoopH = true;
    public bool canLoopV = true;
    string selectionSound = "Selection";
#pragma warning disable CS0414 // Used by commented grid confirm/cancel SFX below
    string selectedSound = "Selected";
    string deselectSound = "DeSelect";
#pragma warning restore CS0414

    // Start is called before the first frame update
    void Start()
    {
        db = Database.instance;

        if(ol == null)
        {
            ol = GetComponent<Outline>();
        }
        mm = MenuManager.instance;

        //Add itself to Menu manager
        if(!mm.controls.Contains(this))
        {
            mm.controls.Add(this);
        }

        if (ol != null)
        {
            baseEffectColor = ol.effectColor;
        }

        orderValue = OrderValue();
    }

    int lastFcRebuildForGrid = -1;
    Color lastOutlineApplied = new Color(-1f, -1f, -1f, -1f);
    bool lastSelectedState;

    // Update is called once per frame
    void Update()
    {
        ControlsInGroup();

        // Only recompute grid layout when the control list actually changed
        if (fcRebuildCounter != lastFcRebuildForGrid)
        {
            GetGridSize();
            lastFcRebuildForGrid = fcRebuildCounter;
        }
        CurrentPosition();

        bool selected = IsSelected();
        if (ol == null)
            return;

        if (selected)
        {
            Color eC = Color.clear;

            for (int i = 0; i < playersInControl.Count; i++)
            {
                int p = playersInControl[i];
                bool pass = true;

                if (basedOnPlayerState)
                {
                    if (p >= 0 && p < db.players.Count)
                    {
                        if (db.players[p].state != group)
                            pass = false;
                    }
                    else if (p != -10)
                    {
                        pass = false;
                    }
                }

                if (p >= 0 && p < db.playerColors.Count && pass)
                {
                    if (eC == Color.clear)
                        eC = db.playerColors[p].color;
                    else
                        eC = Color.Lerp(eC, db.playerColors[p].color, .5f);
                }
                else if (p == -10)
                {
                    eC = db.apColor;
                }
            }

            eC.a = 1f;
            if (!lastSelectedState || !ColorsEqual(lastOutlineApplied, eC))
            {
                ol.effectColor = eC;
                lastOutlineApplied = eC;
            }
            lastSelectedState = true;
        }
        else
        {
            if (lastSelectedState || lastOutlineApplied.a > 0.01f)
            {
                Color eC = baseEffectColor;
                eC.a = 0f;
                ol.effectColor = eC;
                lastOutlineApplied = eC;
            }
            lastSelectedState = false;
        }
    }

    static bool ColorsEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f
            && Mathf.Abs(a.g - b.g) < 0.002f
            && Mathf.Abs(a.b - b.b) < 0.002f
            && Mathf.Abs(a.a - b.a) < 0.002f;
    }
    
    void Navagate()
    {
        for (int i = playersInControl.Count - 1; i >= 0; i--)
        {
            int c = playersInControl[i];

            if ((c >= 0 && c < db.players.Count) || c == -10)
            {
                Player p = db.allplay;
                
                if(c != -10)
                {
                    p = db.players[c];
                }

                if (p.WithinLastUpdate && (!basedOnPlayerState || (basedOnPlayerState && p.state == group)) && !p.gridLock)
                {
                    List<string> l = new List<string>();
                    List<string> r = new List<string>();
                    List<string> u = new List<string>();
                    List<string> d = new List<string>();
                    List<string> g = new List<string>();
                    List<string> x = new List<string>();

                    if ((p.computer && p == db.players.Find(x=> x.computer && !x.characterSelected)) || c == -10)
                    {
                        int fC = db.players.FindIndex(x => x.computer && !x.characterSelected);

                        if (c == -10)
                        {
                            fC = c;
                        }

                        if (fC == c)
                        {
                            List<Player> rP = db.players.FindAll(x => x.characterSelected && !x.computer && x.WithinLastUpdate);

                            if (c == -10)
                            {
                                rP = db.players.FindAll(x => !x.computer);
                            }

                            if (rP.Count > 0)
                            {
                                for (int z = 0; z < rP.Count; z++)
                                {/*
                                    l.AddRange(rP[z].buttons.left);
                                    r.AddRange(rP[z].buttons.right);
                                    u.AddRange(rP[z].buttons.up);
                                    d.AddRange(rP[z].buttons.down);
                                    g.AddRange(rP[z].buttons.confirm);
                                    x.AddRange(rP[z].buttons.cancel);*/
                                }
                            }
                        }
                    }
                    else
                    {
                        /*l.AddRange(p.buttons.left);
                        r.AddRange(p.buttons.right);
                        u.AddRange(p.buttons.up);
                        d.AddRange(p.buttons.down);
                        g.AddRange(p.buttons.confirm);
                        x.AddRange(p.buttons.cancel);*/
                    }

                    GridControl nG = null;
                    Vector2Int np = curPos;
                    int tic = 0;
                    int maxTic = 0;
                    int cI;
                    bool pass = false;
                    Vector2Int dir = Vector2Int.zero;
                    bool loopingH = false;
                    bool loopingV = false;

                    /*if (bm.KeyDown(r))
                    {
                        dir = new Vector2Int(0, 1);
                        maxTic = gridSize.y;
                    }

                    if (bm.KeyDown(l))
                    {
                        dir = new Vector2Int(0, -1);
                        maxTic = gridSize.y;
                    }

                    if (bm.KeyDown(u))
                    {
                        dir = new Vector2Int(-1, 0);
                        maxTic = gridSize.x;
                    }

                    if (bm.KeyDown(d))
                    {
                        dir = new Vector2Int(1, 0);
                        maxTic = gridSize.x;
                    }

                    if (bm.KeyDown(g))
                    {
                        db.PlaySound(selectedSound);
                        p.lastGridUpdate = Time.time;
                        SendMessage("OnClick", c, SendMessageOptions.DontRequireReceiver);
                        FireOnClick();
                    }

                    if (bm.KeyDown(x))
                    {
                        db.PlaySound(deselectSound);
                        p.lastGridUpdate = Time.time;
                        SendMessage("OnCancel", c, SendMessageOptions.DontRequireReceiver);
                    }*/

                    if (dir != Vector2Int.zero)
                    {
                        LoopDirection lD = LoopDirection.Right;

                        while (!pass && tic <= maxTic)
                        {
                            np += dir;

                            if (np.y >= gridSize.y)
                            {
                                np.y = 0;
                                loopingH = true;
                                lD = LoopDirection.Right;
                            }
                            else if (np.y < 0)
                            {
                                np.y = gridSize.y - 1;
                                loopingH = true;
                                lD = LoopDirection.Left;
                            }

                            if (np.x >= gridSize.x)
                            {
                                np.x = 0;
                                loopingV = true;
                                lD = LoopDirection.Down;
                            }
                            else if (np.x < 0)
                            {
                                np.x = gridSize.x - 1;
                                loopingV = true;
                                lD = LoopDirection.Up;
                            }

                            cI = GetIndex(np);

                            if (cI >= 0 && cI < fc.Count)
                            {
                                if (fc[cI].canSelect && fc[cI].gameObject.activeInHierarchy && fc[cI] != this)
                                {
                                    nG = fc[cI];
                                    pass = true;
                                }
                            }

                            tic++;
                        }

                        if(loopingH || loopingV)
                        {
                            p.lastGridUpdate = Time.time;
                            SendMessage("OnLoop", new Looped(c, gameObject, lD), SendMessageOptions.DontRequireReceiver);
                        }

                        if (pass && nG != null && (canLoopH || (!canLoopH && !loopingH)) && (canLoopV || (!canLoopV && !loopingV)))
                        {
                            db.PlaySound(selectionSound);
                            nG.AddPlayer(c);
                            playersInControl.RemoveAt(i);
                        }
                    }
                }
            }
            else
            {
                playersInControl.RemoveAt(i);
            }
        }
    }

    void FireOnClick()
    {
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.Invoke();
        }
    }

    public void AddPlayer(int c)
    {
        if(c == -10)
        {
            db.allplay.lastGridUpdate = Time.time;
        }
        else
        {
            db.players[c].lastGridUpdate = Time.time;
        }

        SendMessage("PlayerAdded",new PlayerAdded(c,gameObject), SendMessageOptions.DontRequireReceiver);

        if(!playersInControl.Contains(c))
        {
            playersInControl.Add(c);
        }
    }

    static readonly List<float> xAccountedScratch = new List<float>(32);
    static readonly List<float> yAccountedScratch = new List<float>(32);

    void GetGridSize()
    {
        gridSize = Vector2Int.one;

        if (fc == null || fc.Count == 0 || mm == null)
            return;

        xAccountedScratch.Clear();
        yAccountedScratch.Clear();
        float tol = mm.inLineTolorance;

        for (int i = 0; i < fc.Count; i++)
        {
            if (fc[i] == null)
                continue;
            float px = fc[i].position.x;
            float py = fc[i].position.y;

            bool hasX = false;
            for (int x = 0; x < xAccountedScratch.Count; x++)
            {
                if (Mathf.Abs(xAccountedScratch[x] - px) < tol)
                {
                    hasX = true;
                    break;
                }
            }
            if (!hasX)
                xAccountedScratch.Add(px);

            bool hasY = false;
            for (int y = 0; y < yAccountedScratch.Count; y++)
            {
                if (Mathf.Abs(yAccountedScratch[y] - py) < tol)
                {
                    hasY = true;
                    break;
                }
            }
            if (!hasY)
                yAccountedScratch.Add(py);
        }

        gridSize = new Vector2Int(yAccountedScratch.Count, xAccountedScratch.Count);
    }

    int cachedSelfIndex = -2;
    int cachedSelfIndexRebuild = -1;

    void CurrentPosition()
    {
        if (fc == null || fc.Count == 0)
            return;

        if (cachedSelfIndexRebuild != fcRebuildCounter || cachedSelfIndex < 0 || cachedSelfIndex >= fc.Count || fc[cachedSelfIndex] != this)
        {
            cachedSelfIndex = -1;
            for (int i = 0; i < fc.Count; i++)
            {
                if (fc[i] == this)
                {
                    cachedSelfIndex = i;
                    break;
                }
            }
            cachedSelfIndexRebuild = fcRebuildCounter;
        }

        int index = cachedSelfIndex;
        if (index >= 0 && gridSize.y > 0)
        {
            curPos.x = Mathf.FloorToInt(index / gridSize.y);
            curPos.y = index % gridSize.y;
        }
    }

    int GetIndex(Vector2Int pos)
    {
        return (pos.x * gridSize.y) + pos.y;
    }

    int cachedControlsVersion = -1;
    int cachedGroupEnabledCount = -1;
    int fcRebuildCounter;
    float nextGroupScanTime;

    void ControlsInGroup()
    {
        if (mm == null || mm.controls == null)
            return;

        // Throttle full scans — Count alone misses enable/disable; scanning every frame was costly
        int version = mm.controls.Count;
        bool force = version != cachedControlsVersion || fc == null;
        if (!force && Time.unscaledTime < nextGroupScanTime)
            return;

        nextGroupScanTime = Time.unscaledTime + 0.2f;

        int enabledCount = 0;
        for (int i = 0; i < mm.controls.Count; i++)
        {
            GridControl c = mm.controls[i];
            if (c != null && c.group == group && c.isActiveAndEnabled)
                enabledCount++;
        }

        if (!force && enabledCount == cachedGroupEnabledCount)
            return;

        cachedControlsVersion = version;
        cachedGroupEnabledCount = enabledCount;
        if (fc == null)
            fc = new List<GridControl>(enabledCount);
        else
            fc.Clear();

        for (int i = 0; i < mm.controls.Count; i++)
        {
            GridControl c = mm.controls[i];
            if (c != null && c.group == group && c.isActiveAndEnabled)
                fc.Add(c);
        }
        fc.Sort((p1, p2) => p2.OrderValue().CompareTo(p1.OrderValue()));
        fcRebuildCounter++;
    }

    bool IsSelected()
    {
        bool result = false;

        if(basedOnPlayerState)
        {
            if(playersInControl.Count > 0)
            {
                if(playersInControl.Exists(x=> x == -10))
                {
                    result = true;
                }
                else
                {
                    for (int i = 0; i < db.players.Count; i++)
                    {
                        if (db.players[i].state == group)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            MenuClass mC = mm.GetOpenMenu();

            if (mC.title == group)
            {
                if (playersInControl.Count > 0)
                {
                    result = true;
                }
            }
        }

        return result;
    }

    public float OrderValue()
    {
        float result = position.y * 100000;
        result += -position.x;

        return result;
    }
}

public class PlayerAdded
{
    public int p;
    public GameObject owner;

    public PlayerAdded(int player,GameObject caller)
    {
        p = player;
        owner = caller;
    }
}

public class Looped
{
    public PlayerAdded pA;
    public LoopDirection direction;

    public Looped(int player, GameObject caller, LoopDirection dir)
    {
        pA = new PlayerAdded(player, caller);
        direction = dir;
    }

    public Looped(PlayerAdded player, LoopDirection dir)
    {
        pA = player;
        direction = dir;
    }
}

public enum LoopDirection
{
    Up,
    Down,
    Left,
    Right
}