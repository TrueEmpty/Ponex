using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Arcade flow: character select (max 2), a versus card, then premade matches.
/// Health carries between rounds. A downed coop partner can be revived by holding bump nearby.
/// If they are still down when the round is won, the next round is played alone.
/// </summary>
[DefaultExecutionOrder(-50)]
public class ArcadeRun : MonoBehaviour
{
    public static ArcadeRun instance;
    public static bool Running => instance != null && instance.running;

    const float IntroSeconds = 5f;
    const float ReviveHoldSeconds = 2f;
    const float ReviveRange = 3.4f;

    bool running;
    bool coopRun;
    bool rosterBuilt;
    int level = 1;
    ArcadeMatch current;
    ComputerAI.CpuDifficulty difficulty;
    string clearA = "";
    string clearB = "";
    bool humansWon;

    bool loadoutSaved;
    int savedField;
    int savedBallCount;
    int savedSelectedBall;
    int[] savedBallSlots;
    bool? savedTeamSelect;
    bool? savedPositionSelect;

    readonly Dictionary<int, int> carriedHealth = new Dictionary<int, int>();
    readonly Dictionary<int, Vector3> downedAt = new Dictionary<int, Vector3>();
    float reviveHold;
    float regenTimer;
    float matchStart;
    bool matchClock;
    float playSeconds;

    float introShownAt = -1f;
    GameObject card;
    GameObject endCard;
    GameObject difficultyButton;
    TeamModeToggle teamToggle;
    bool hidTeamToggle;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        ArcadeRecords.EnsureLoaded();
    }

    void Update()
    {
        RefreshDifficultyButton();
        RefreshTeamToggle();
        TickMatch();
    }

    public void EnsureMatchReady()
    {
        Database db = Database.instance;
        if (db == null)
            return;

        if (!running)
            BeginRun(db);

        if (current == null)
            PickMatch();

        if (!rosterBuilt)
        {
            BuildOpponents(db);
            rosterBuilt = true;
        }

        ApplyLoadout(db);
        ShowCard();
        introShownAt = Time.unscaledTime;
    }

    public IEnumerator WaitRemainder()
    {
        while (Time.unscaledTime < introShownAt + IntroSeconds)
            yield return null;
        HideCard();
    }

    public static void RestoreHealth(Player p)
    {
        if (instance == null || p == null || !instance.running)
            return;
        if (p.arcadeOut)
        {
            p.currentHealth = 0;
            return;
        }
        if (instance.carriedHealth.TryGetValue(p.index, out int hp))
            p.currentHealth = Mathf.Clamp(hp, 1, Mathf.Max(1, p.maxHealth));
    }

    public void ApplySeats()
    {
        Database db = Database.instance;
        if (db == null || db.players == null || current == null)
            return;

        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;
            p.arcadeInfiniteHealth = false;
            if (p.arcadeOut)
                p.currentHealth = 0;
        }

        List<Player> humans = ActiveHumans(db);
        List<ArcadeSeat> humanSeats = Seats(true);
        List<ArcadeSeat> enemySeats = Seats(false);
        FixSharedSides(humanSeats, enemySeats);

        for (int i = 0; i < humans.Count; i++)
        {
            ArcadeSeat seat = SeatForHuman(humanSeats, humans[i].arcadeHumanSlot);
            if (seat == null && i < humanSeats.Count)
                seat = humanSeats[i];
            if (seat != null)
                Place(humans[i], seat, false);
        }

        List<Player> foes = new List<Player>();
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && p.arcadeCpu)
                foes.Add(p);
        }

        for (int i = 0; i < foes.Count && i < enemySeats.Count; i++)
            Place(foes[i], enemySeats[i], current.enemiesInfiniteHealth);

        if (db.players != null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p == null || !p.computer)
                    continue;
                ComputerAI.SetDifficulty(p, difficulty);
                ComputerAI.AssignMatchBrain(p);
            }
        }
    }

    public void ModifyBalls()
    {
        if (current == null)
            return;

        float damageScale = Mathf.Max(0.25f, current.ballDamageScale);
        float sizeScale = Mathf.Max(0.35f, current.ballSizeScale);
        if (Mathf.Abs(damageScale - 1f) < 0.01f && Mathf.Abs(sizeScale - 1f) < 0.01f)
            return;

        for (int i = 0; i < LiveBallRegistry.Count; i++)
        {
            BallInfo info = LiveBallRegistry.GetAt(i);
            if (info == null || info.ball == null)
                continue;
            info.ball = new Ball(info.ball);
            info.ball.damage = Mathf.Max(1, Mathf.RoundToInt(info.ball.damage * damageScale));
            if (Mathf.Abs(sizeScale - 1f) > 0.01f)
                info.transform.localScale *= sizeScale;
        }
    }

    public bool PollMatchEnd()
    {
        Database db = Database.instance;
        if (db == null || current == null || !db.gameStart)
            return false;

        List<Player> humans = ActiveHumans(db);
        List<Player> foes = Foes(db);
        bool survival = current.surviveSeconds > 0.5f;
        float elapsed = matchClock ? Time.time - matchStart : 0f;

        bool anyHuman = false;
        for (int i = 0; i < humans.Count; i++)
        {
            if (humans[i].currentHealth > 0)
                anyHuman = true;
        }

        bool foesDown = true;
        for (int i = 0; i < foes.Count; i++)
        {
            if (foes[i].currentHealth > 0)
                foesDown = false;
        }

        bool roundOver = (survival && elapsed >= current.surviveSeconds)
            || (!survival && foes.Count > 0 && foesDown);

        // One living player is enough. On a split round the two of you are different
        // teams, so when only you both are left the match is already won.
        bool win = false;
        bool loss = false;
        if (!anyHuman)
            loss = true;
        else if (roundOver)
            win = true;

        if (!win && !loss)
            return false;

        humansWon = win && !loss;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null)
                continue;
            if (p.arcadeOut)
                p.won = false;
            else if (p.arcadeHumanSlot >= 0)
                p.won = humansWon && p.currentHealth > 0;
            else
                p.won = !humansWon && p.currentHealth > 0;
        }
        return true;
    }

    public void ResolveMatch()
    {
        Database db = Database.instance;
        if (db == null)
            return;

        HideCard();
        SnapshotHealth(db);
        RemoveOpponents(db);
        rosterBuilt = false;
        current = null;
        matchClock = false;
        GameSettings.ClearHazardOverride();

        if (!humansWon)
        {
            RememberNames(db);
            ShowEnd(false);
            return;
        }

        level++;
        if (level > 10)
        {
            RememberNames(db);
            ArcadeRecords.RecordClear(coopRun, clearA, clearB, difficulty);
            ShowEnd(true);
            return;
        }

        PickMatch();
        db.CharactersPicked("balls");
    }

    public void EndRun()
    {
        running = false;
        rosterBuilt = false;
        current = null;
        matchClock = false;
        carriedHealth.Clear();
        downedAt.Clear();
        GameSettings.ClearHazardOverride();
        RestoreLoadout();
        HideCard();
        if (endCard != null)
        {
            Destroy(endCard);
            endCard = null;
        }

        Database db = Database.instance;
        if (db != null && db.players != null)
        {
            RemoveOpponents(db);
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p == null)
                    continue;
                p.arcadeOut = false;
                p.arcadeInfiniteHealth = false;
                p.arcadeHumanSlot = -1;
            }
            if (CharacterSelect.instance != null)
                CharacterSelect.instance.PruneMissingPortraits();
        }
    }

    void BeginRun(Database db)
    {
        running = true;
        level = 1;
        rosterBuilt = false;
        current = null;
        carriedHealth.Clear();
        downedAt.Clear();
        playSeconds = 0f;
        difficulty = ArcadeRecords.SelectedDifficulty;

        List<Player> humans = LivingLobbyHumans(db);
        coopRun = humans.Count >= 2;
        clearA = humans.Count > 0 ? humans[0].name : "";
        clearB = humans.Count > 1 ? humans[1].name : "";
        for (int i = 0; i < humans.Count; i++)
        {
            humans[i].arcadeHumanSlot = i;
            humans[i].arcadeOut = false;
            humans[i].arcadeCpu = false;
        }

        if (!loadoutSaved)
        {
            db.EnsureBallSlotsPublic();
            savedField = db.selectedField;
            savedBallCount = db.ballCount;
            savedSelectedBall = db.selectedBall;
            savedBallSlots = (int[])db.ballSlots.Clone();
            savedTeamSelect = db.teamSelect;
            savedPositionSelect = db.positionSelect;
            loadoutSaved = true;
        }

        db.teamSelect = false;
        db.positionSelect = false;
        db.maxPlayers = 2;
        PickMatch();
    }

    void RememberNames(Database db)
    {
        if (db.players == null)
            return;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeHumanSlot < 0)
                continue;
            if (string.IsNullOrEmpty(p.name))
                continue;
            if (p.arcadeHumanSlot == 0)
                clearA = p.name;
            else if (p.arcadeHumanSlot == 1)
                clearB = p.name;
        }
    }

    void PickMatch()
    {
        List<ArcadeMatch> pool = ArcadeMatchDefaults.Matching(coopRun, Mathf.Clamp(level, 1, 10));
        if (pool == null || pool.Count == 0)
        {
            current = null;
            return;
        }
        current = pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    void BuildOpponents(Database db)
    {
        RemoveOpponents(db);
        if (current == null || current.seats == null)
            return;

        int cap = db.maxPlayers;
        db.maxPlayers = 8;
        List<ArcadeSeat> foes = Seats(false);
        for (int i = 0; i < foes.Count; i++)
        {
            if (db.players.Count >= 8)
                break;
            int index = db.PlayerAdd(null);
            if (index < 0)
                break;
            Player p = db.players.Find(x => x.index == index);
            if (p == null)
                continue;
            p.arcadeCpu = true;
            p.computer = true;
            p.name = foes[i].characterName;
            p.arcadeHumanSlot = -1;
            ComputerAI.SetDifficulty(p, difficulty);
            if (db.characters != null)
            {
                Characters roster = db.characters.Find(c => c != null && c.name == p.name);
                if (roster != null)
                    p.SetUpCharacter(roster);
            }
        }
        db.maxPlayers = cap;
    }

    void RemoveOpponents(Database db)
    {
        if (db.players == null)
            return;
        for (int i = db.players.Count - 1; i >= 0; i--)
        {
            Player p = db.players[i];
            if (p == null || !p.arcadeCpu)
                continue;
            if (p.spawnedPlayer != null)
                Destroy(p.spawnedPlayer);
            if (p.spawnedLifeline != null)
                Destroy(p.spawnedLifeline);
            db.players.RemoveAt(i);
        }
    }

    void ApplyLoadout(Database db)
    {
        if (current == null)
            return;

        GameSettings.HazardOverride = current.hazards;
        if (db.fields != null)
        {
            int field = db.fields.FindIndex(f => f != null && f.name == current.fieldName);
            if (field >= 0)
                db.selectedField = field;
        }

        db.EnsureBallSlotsPublic();
        string[] names = current.ballNames;
        int count = names != null && names.Length > 0 ? names.Length : 1;
        db.ballCount = Mathf.Clamp(count, 1, Database.MaxMatchBalls);
        for (int slot = 0; slot < Database.MaxMatchBalls; slot++)
        {
            int ballIndex = -1;
            if (names != null && slot < names.Length && db.balls != null)
            {
                ballIndex = db.balls.FindIndex(b => b != null && b.name == names[slot]);
                if (ballIndex < 0)
                    ballIndex = -1;
            }
            db.ballSlots[slot] = slot < db.ballCount ? ballIndex : -1;
        }
        db.selectedBall = db.ballSlots[0];
    }

    void RestoreLoadout()
    {
        if (!loadoutSaved)
            return;
        Database db = Database.instance;
        if (db == null)
            return;
        db.selectedField = savedField;
        db.ballCount = savedBallCount;
        db.selectedBall = savedSelectedBall;
        if (savedBallSlots != null)
        {
            db.EnsureBallSlotsPublic();
            for (int i = 0; i < db.ballSlots.Length && i < savedBallSlots.Length; i++)
                db.ballSlots[i] = savedBallSlots[i];
        }
        if (savedTeamSelect.HasValue)
            db.teamSelect = savedTeamSelect.Value;
        if (savedPositionSelect.HasValue)
            db.positionSelect = savedPositionSelect.Value;
        loadoutSaved = false;
    }

    void SnapshotHealth(Database db)
    {
        carriedHealth.Clear();
        if (db.players == null)
            return;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeHumanSlot < 0)
                continue;
            if (p.currentHealth <= 0)
            {
                p.arcadeOut = true;
                continue;
            }
            p.arcadeOut = false;
            carriedHealth[p.index] = p.currentHealth;
        }
    }

    void TickMatch()
    {
        Database db = Database.instance;
        if (!running || db == null || !db.gameStart || current == null)
        {
            if (!db || !db.gameStart)
                matchClock = false;
            return;
        }

        if (!matchClock)
        {
            matchClock = true;
            matchStart = Time.time;
            regenTimer = 0f;
            reviveHold = 0f;
            downedAt.Clear();
        }

        if (db.LocalMatchPaused || db.winnerScreen)
            return;

        playSeconds += Time.deltaTime;
        RememberDowned(db);
        TickRegen(db);
        TickRevive(db);
    }

    void RememberDowned(Database db)
    {
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeOut || p.arcadeHumanSlot < 0)
                continue;
            if (p.currentHealth <= 0 && p.spawnedPlayer != null)
                downedAt[p.index] = p.spawnedPlayer.transform.position;
        }
    }

    void TickRegen(Database db)
    {
        if (!current.enemiesRegenerate)
            return;
        regenTimer += Time.deltaTime;
        if (regenTimer < Mathf.Max(0.5f, current.regenInterval))
            return;
        regenTimer = 0f;
        int amount = Mathf.Max(1, current.regenAmount);
        List<Player> foes = Foes(db);
        for (int i = 0; i < foes.Count; i++)
        {
            Player p = foes[i];
            if (p.arcadeInfiniteHealth || p.currentHealth <= 0)
                continue;
            p.currentHealth = Mathf.Min(p.maxHealth, p.currentHealth + amount);
        }
    }

    void TickRevive(Database db)
    {
        if (!coopRun || downedAt.Count == 0)
            return;

        List<Player> humans = ActiveHumans(db);
        Player saver = null;
        Player downed = null;
        for (int i = 0; i < humans.Count; i++)
        {
            Player candidate = humans[i];
            if (candidate.currentHealth <= 0 || candidate.spawnedPlayer == null)
                continue;
            if (!candidate.computer)
            {
                saver = candidate;
                break;
            }
            if (saver == null)
                saver = candidate;
        }
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeOut || p.currentHealth > 0)
                continue;
            if (!downedAt.ContainsKey(p.index))
                continue;
            downed = p;
            break;
        }

        if (saver == null || downed == null)
        {
            reviveHold = 0f;
            return;
        }

        Vector3 spot = downedAt[downed.index];
        Vector3 from = saver.spawnedPlayer.transform.position;
        float dist = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(spot.x, spot.y));
        if (dist <= ReviveRange && JumpHeld(saver))
            reviveHold += Time.deltaTime;
        else
            reviveHold = 0f;

        if (reviveHold < ReviveHoldSeconds)
            return;

        reviveHold = 0f;
        downedAt.Remove(downed.index);
        downed.arcadeOut = false;
        downed.currentHealth = Mathf.Max(1, downed.maxHealth / 2);
        downed.maxHealth = Mathf.Max(1, downed.maxHealth);
        db.SpawnJoinedPlayer(downed, downed.index);
    }

    static bool JumpHeld(Player p)
    {
        if (p != null && p.cLink != null)
        {
            ControllerButtons button = p.cLink["Jump"];
            if (button != null && button.isPressed)
                return true;
        }
        if (p != null && p.inputLinks != null)
        {
            for (int i = 0; i < p.inputLinks.Count; i++)
            {
                ControllerLink link = p.inputLinks[i];
                if (link == null)
                    continue;
                ControllerButtons button = link["Jump"];
                if (button != null && button.isPressed)
                    return true;
            }
        }
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    void Place(Player p, ArcadeSeat seat, bool infinite)
    {
        p.facing = seat.facing;
        p.team = seat.team;
        p.ticWallSideOffset = seat.wallOffset;
        p.position = 0;
        p.arcadeInfiniteHealth = infinite;
    }

    List<ArcadeSeat> Seats(bool humans)
    {
        List<ArcadeSeat> list = new List<ArcadeSeat>();
        if (current == null || current.seats == null)
            return list;
        for (int i = 0; i < current.seats.Length; i++)
        {
            ArcadeSeat seat = current.seats[i];
            if (seat == null)
                continue;
            if (seat.IsHuman == humans)
                list.Add(seat);
        }
        return list;
    }

    static ArcadeSeat SeatForHuman(List<ArcadeSeat> seats, int slot)
    {
        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i].humanSlot == slot)
                return seats[i];
        }
        return null;
    }

    static void FixSharedSides(List<ArcadeSeat> humans, List<ArcadeSeat> foes)
    {
        List<ArcadeSeat> all = new List<ArcadeSeat>();
        all.AddRange(humans);
        all.AddRange(foes);
        Facing[] order = { Facing.Up, Facing.Down, Facing.Left, Facing.Right };
        for (int i = 0; i < all.Count; i++)
        {
            ArcadeSeat seat = all[i];
            int teamOnSide = TeamOnFacing(all, seat.facing, i);
            if (teamOnSide < 0 || teamOnSide == seat.team)
                continue;
            for (int f = 0; f < order.Length; f++)
            {
                int used = TeamOnFacing(all, order[f], i);
                if (used < 0 || used == seat.team)
                {
                    seat.facing = order[f];
                    break;
                }
            }
        }
    }

    static int TeamOnFacing(List<ArcadeSeat> seats, Facing facing, int before)
    {
        for (int i = 0; i < before; i++)
        {
            if (seats[i].facing == facing)
                return seats[i].team;
        }
        return -1;
    }

    static List<Player> LivingLobbyHumans(Database db)
    {
        List<Player> list = new List<Player>();
        if (db.players == null)
            return list;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu)
                continue;
            list.Add(p);
        }
        return list;
    }

    static List<Player> ActiveHumans(Database db)
    {
        List<Player> list = new List<Player>();
        if (db.players == null)
            return list;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeOut)
                continue;
            if (p.arcadeHumanSlot < 0)
                continue;
            list.Add(p);
        }
        return list;
    }

    static List<Player> Foes(Database db)
    {
        List<Player> list = new List<Player>();
        if (db.players == null)
            return list;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p != null && p.arcadeCpu)
                list.Add(p);
        }
        return list;
    }

    void ShowCard()
    {
        if (card != null)
            Destroy(card);
        card = BuildVersusCard();
    }

    public bool HasEndCard => endCard != null;

    public void HideCard()
    {
        if (card != null)
        {
            Destroy(card);
            card = null;
        }
    }

    void RetryFromStart()
    {
        if (endCard != null)
        {
            Destroy(endCard);
            endCard = null;
        }

        running = false;
        rosterBuilt = false;
        current = null;
        matchClock = false;
        playSeconds = 0f;
        carriedHealth.Clear();
        downedAt.Clear();
        GameSettings.ClearHazardOverride();

        Database db = Database.instance;
        if (db == null)
            return;

        db.gameStart = false;
        db.someoneWon = false;
        db.winnerScreen = false;
        db.startingGame = false;
        Time.timeScale = 1f;

        if (db.players != null)
        {
            for (int i = 0; i < db.players.Count; i++)
            {
                Player p = db.players[i];
                if (p == null || p.arcadeCpu)
                    continue;
                p.arcadeOut = false;
                p.arcadeInfiniteHealth = false;
                p.won = false;
                if (p.maxHealth > 0)
                    p.currentHealth = p.maxHealth;
            }
        }

        db.CharactersPicked("balls");
    }

    void ShowEnd(bool won)
    {
        if (endCard != null)
            Destroy(endCard);
        endCard = new GameObject("Arcade Result", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = endCard.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 620;
        CanvasScaler scaler = endCard.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform root = endCard.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image bg = endCard.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.06f, 1f);

        Font font = MenuFont();
        if (won)
            BuildWinScreen(root, font);
        else
            BuildLoseScreen(root, font);
    }

    void BuildLoseScreen(RectTransform root, Font font)
    {
        MakeText(root, "Result Title", "Defeated", 64, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(900f, 90f), font, Color.white);
        MakeText(root, "Lost Match", "Lost on Match " + Mathf.Clamp(level, 1, 10), 32, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(800f, 48f), font, new Color(0.95f, 0.55f, 0.4f, 1f));

        WireEndButton(MakeMenuButton(root, "Retry", new Vector2(0f, 30f)), RetryFromStart);
        WireEndButton(MakeMenuButton(root, "Character Select", new Vector2(0f, -60f)), () =>
        {
            if (Database.instance != null)
                Database.instance.ReturnToCharacterSelect();
        });
        WireEndButton(MakeMenuButton(root, "Main Menu", new Vector2(0f, -150f)), () =>
        {
            if (Database.instance != null)
                Database.instance.LeaveMatchToMainMenu();
        });
    }

    void BuildWinScreen(RectTransform root, Font font)
    {
        MakeText(root, "Result Title", "Arcade Clear", 64, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 80f), font, Color.white);
        MakeText(root, "Clear Time", FormatPlayTime(playSeconds), 36, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(700f, 50f), font, new Color(0.95f, 0.88f, 0.45f, 1f));

        List<Player> cast = RunHumans();
        float span = 180f;
        float origin = -((cast.Count - 1) * span) * 0.5f;
        for (int i = 0; i < cast.Count; i++)
        {
            Player p = cast[i];
            float x = origin + i * span;
            Portrait(root, p, new Vector2(x, 150f), new Vector2(0.5f, 0.5f));
            string label = string.IsNullOrEmpty(p.name) ? "P" + (p.index + 1) : p.name;
            MakeText(root, "Winner " + i, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 78f), new Vector2(170f, 32f), font, Color.white);
        }

        WireEndButton(MakeMenuButton(root, "Character Select", new Vector2(0f, -150f)), () =>
        {
            if (Database.instance != null)
                Database.instance.ReturnToCharacterSelect();
        });
        WireEndButton(MakeMenuButton(root, "Main Menu", new Vector2(0f, -240f)), () =>
        {
            if (Database.instance != null)
                Database.instance.LeaveMatchToMainMenu();
        });
    }

    List<Player> RunHumans()
    {
        List<Player> list = new List<Player>();
        Database db = Database.instance;
        if (db == null || db.players == null)
            return list;
        for (int i = 0; i < db.players.Count; i++)
        {
            Player p = db.players[i];
            if (p == null || p.arcadeCpu || p.arcadeHumanSlot < 0)
                continue;
            list.Add(p);
        }
        list.Sort((a, b) => a.arcadeHumanSlot.CompareTo(b.arcadeHumanSlot));
        return list;
    }

    static void WireEndButton(GameObject button, Action action)
    {
        if (button == null || action == null)
            return;
        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(260f, 65f);
        SelectorClickable.Ensure(button, 160f);
        StoryButton click = button.GetComponent<StoryButton>();
        if (click != null)
            click.clicked = _ => action();
    }

    static string FormatPlayTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs = total % 60;
        if (hours > 0)
            return hours + ":" + minutes.ToString("00") + ":" + secs.ToString("00");
        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    GameObject BuildVersusCard()
    {
        Database db = Database.instance;
        GameObject rootGo = new GameObject("Arcade Versus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 560;
        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image bg = rootGo.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.08f, 1f);
        bg.raycastTarget = true;

        Font font = MenuFont();
        string matchName = current != null ? current.matchName : "Match";
        string diffName = DifficultyLabel(difficulty);
        string field = current != null ? current.fieldName : "";

        MakeText(root, "Match Number", "Match " + Mathf.Clamp(level, 1, 10), 40, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -52f), new Vector2(400f, 64f), font, Color.white);
        MakeText(root, "Match Name", matchName, 48, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 64f), font, Color.white);
        MakeText(root, "Difficulty", diffName, 26, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(500f, 36f), font, DifficultyColor(difficulty));
        MakeText(root, "Vs", "Vs", 72, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 90f), font, Color.white);
        MakeText(root, "Level", field, 22, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(700f, 36f), font, new Color(0.8f, 0.86f, 0.92f, 1f));

        Image line = NewImage(root, "Diagonal", Color.white);
        RectTransform lineRt = line.rectTransform;
        lineRt.anchorMin = new Vector2(0.5f, 0.5f);
        lineRt.anchorMax = new Vector2(0.5f, 0.5f);
        lineRt.sizeDelta = new Vector2(1500f, 5f);
        lineRt.localRotation = Quaternion.Euler(0f, 0f, 32f);
        lineRt.SetAsFirstSibling();

        if (db != null)
        {
            List<Player> humans = ActiveHumans(db);
            if (humans.Count == 0)
                humans = LivingLobbyHumans(db);
            const float humanSize = 280f;
            for (int i = 0; i < humans.Count; i++)
            {
                float x = 70f + i * (humanSize + 28f);
                MakeText(root, "P" + (humans[i].index + 1), "P" + (humans[i].index + 1), 36, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x + humanSize * 0.5f, 90f + humanSize + 28f), new Vector2(humanSize, 44f), font, Color.white);
                Portrait(root, humans[i], new Vector2(x, 90f), new Vector2(0f, 0f), humanSize);
            }

            List<Player> foes = Foes(db);
            int cols = foes.Count <= 3 ? Mathf.Max(1, foes.Count) : (foes.Count <= 4 ? 2 : 3);
            float foeSize = foes.Count > 4 ? 200f : 240f;
            float foeStep = foeSize + 16f;
            for (int i = 0; i < foes.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = -48f - col * foeStep;
                float y = -150f - row * foeStep;
                Portrait(root, foes[i], new Vector2(x, y), new Vector2(1f, 1f), foeSize);
            }
        }

        return rootGo;
    }

    void Portrait(RectTransform parent, Player p, Vector2 anchored, Vector2 anchor, float size = 96f)
    {
        GameObject go = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(size, size);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = p != null ? p.portrait : null;
        image.color = p != null && p.portrait == null ? p.portraitColor : Color.white;
        if (image.texture == null && (p == null || p.portraitColor.a < 0.1f))
            image.color = new Color(0.2f, 0.55f, 0.7f, 1f);
    }

    void RefreshDifficultyButton()
    {
        Database db = Database.instance;
        MenuManager mm = MenuManager.instance;
        bool show = db != null
            && db.gametype == Gametype.Arcade
            && mm != null
            && mm.IsMenuVisible("Character Select")
            && !running
            && !db.gameStart;

        if (!show)
        {
            if (difficultyButton != null)
                difficultyButton.SetActive(false);
            return;
        }

        if (difficultyButton == null)
            difficultyButton = CreateDifficultyButton(mm);
        if (difficultyButton == null)
            return;

        difficultyButton.SetActive(true);
        Text label = difficultyButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            ComputerAI.CpuDifficulty diff = ArcadeRecords.SelectedDifficulty;
            label.text = DifficultyLabel(diff);
            label.color = Color.black;
        }
    }

    GameObject CreateDifficultyButton(MenuManager mm)
    {
        MenuClass menu = mm.FindMenu("Arcade");
        Transform parent = menu != null && menu.holder != null ? menu.holder.transform : null;
        if (parent == null)
            return null;

        GameObject go = MakeMenuButton(parent as RectTransform, DifficultyLabel(ArcadeRecords.SelectedDifficulty), new Vector2(0f, -128f));
        if (go == null)
            return null;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -118f);
        StoryButton click = go.GetComponent<StoryButton>();
        if (click != null)
        {
            click.clicked = _ =>
            {
                ComputerAI.CpuDifficulty next = NextDifficulty(ArcadeRecords.SelectedDifficulty);
                ArcadeRecords.SelectedDifficulty = next;
            };
        }
        return go;
    }

    void RefreshTeamToggle()
    {
        if (teamToggle == null)
        {
            TeamModeToggle[] found = FindObjectsByType<TeamModeToggle>(FindObjectsInactive.Include);
            if (found != null && found.Length > 0)
                teamToggle = found[0];
        }
        if (teamToggle == null)
            return;

        Database db = Database.instance;
        MenuManager mm = MenuManager.instance;
        bool hide = db != null && db.gametype == Gametype.Arcade && mm != null && mm.IsMenuVisible("Character Select");
        if (hide && teamToggle.gameObject.activeSelf)
        {
            teamToggle.gameObject.SetActive(false);
            hidTeamToggle = true;
        }
        else if (!hide && hidTeamToggle)
        {
            teamToggle.gameObject.SetActive(true);
            hidTeamToggle = false;
        }
    }

    static GameObject MakeMenuButton(RectTransform parent, string title, Vector2 anchored)
    {
        if (parent == null)
            return null;

        GameObject go = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.tag = "Selection";
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(170f, 65f);

        Image image = go.GetComponent<Image>();
        image.sprite = MenuSprite();
        image.color = new Color(0f, 0.941309f, 1f, 1f);
        image.type = Image.Type.Simple;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.094f, 0.338f, 0.945f, 1f);
        shadow.effectDistance = new Vector2(4.24f, 3.93f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.layer = 5;
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 8f);
        textRt.offsetMax = new Vector2(-10f, -8f);
        Text text = textGo.GetComponent<Text>();
        text.font = MenuFont();
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.text = title;
        text.raycastTarget = false;

        StoryButton button = go.AddComponent<StoryButton>();
        button.SetBaseColor(image.color);
        return go;
    }

    static Text MakeText(RectTransform parent, string name, string value, int size, FontStyle style, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 box, Font font, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Image NewImage(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Font MenuFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static Sprite menuSprite;

    static Sprite MenuSprite()
    {
        if (menuSprite != null)
            return menuSprite;
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].sprite == null)
                continue;
            if (images[i].gameObject.name == "Arcade" || images[i].gameObject.name == "Vs" || images[i].gameObject.name == "Story")
            {
                menuSprite = images[i].sprite;
                return menuSprite;
            }
        }
        return null;
    }

    static ComputerAI.CpuDifficulty NextDifficulty(ComputerAI.CpuDifficulty current)
    {
        switch (current)
        {
            case ComputerAI.CpuDifficulty.Easy: return ComputerAI.CpuDifficulty.Normal;
            case ComputerAI.CpuDifficulty.Normal: return ComputerAI.CpuDifficulty.Hard;
            case ComputerAI.CpuDifficulty.Hard: return ComputerAI.CpuDifficulty.Expert;
            case ComputerAI.CpuDifficulty.Expert: return ComputerAI.CpuDifficulty.Impossible;
            default: return ComputerAI.CpuDifficulty.Easy;
        }
    }

    public static string DifficultyLabel(ComputerAI.CpuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ComputerAI.CpuDifficulty.Easy: return "Easy";
            case ComputerAI.CpuDifficulty.Normal: return "Normal";
            case ComputerAI.CpuDifficulty.Hard: return "Hard";
            case ComputerAI.CpuDifficulty.Expert: return "Expert";
            case ComputerAI.CpuDifficulty.Impossible: return "Impossible";
            default: return "Normal";
        }
    }

    static Color DifficultyColor(ComputerAI.CpuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ComputerAI.CpuDifficulty.Easy: return new Color(0.45f, 0.85f, 0.95f, 1f);
            case ComputerAI.CpuDifficulty.Normal: return new Color(0.95f, 0.85f, 0.35f, 1f);
            case ComputerAI.CpuDifficulty.Hard: return new Color(1f, 0.55f, 0.25f, 1f);
            case ComputerAI.CpuDifficulty.Expert: return new Color(0.95f, 0.35f, 0.45f, 1f);
            default: return new Color(0.85f, 0.25f, 0.95f, 1f);
        }
    }
}
