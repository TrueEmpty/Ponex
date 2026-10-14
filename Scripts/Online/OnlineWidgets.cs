using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class OnlineClick : MonoBehaviour
{
    public Action<int> clicked;

    public void OnClick(int player)
    {
        AudioSettings.PlaySelection();
        if (clicked != null)
            clicked(player);
    }
}

public static class OnlineWidgets
{
    public static readonly Color Cyan = new Color(0f, 0.94f, 1f, 1f);
    public static readonly Color BlueOutline = new Color(0.09f, 0.34f, 0.95f, 1f);
    public static readonly Color Yellow = new Color(0.95f, 0.82f, 0.2f, 1f);
    public static readonly Color Purple = new Color(0.55f, 0.25f, 0.85f, 1f);
    public static readonly Color Green = new Color(0.25f, 0.75f, 0.35f, 1f);
    public static readonly Color Red = new Color(0.85f, 0.22f, 0.22f, 1f);
    public static readonly Color Gray = new Color(0.45f, 0.45f, 0.48f, 1f);

    static Sprite sharedSprite;

    public static Sprite MenuSprite()
    {
        if (sharedSprite != null)
            return sharedSprite;
        Image[] images = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].sprite != null && images[i].gameObject.name == "Online")
            {
                sharedSprite = images[i].sprite;
                break;
            }
        }
        return sharedSprite;
    }

    public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = MenuSprite();
        image.type = Image.Type.Sliced;
        return rt;
    }

    public static void CatchClicks(GameObject go)
    {
        if (go == null)
            return;
        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null)
            col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        RectTransform rt = go.GetComponent<RectTransform>();
        float w = rt != null && rt.rect.width > 1f ? rt.rect.width : rt != null ? rt.sizeDelta.x : 80f;
        float h = rt != null && rt.rect.height > 1f ? rt.rect.height : rt != null ? rt.sizeDelta.y : 80f;
        col.size = new Vector3(w, h, 1f);
        float pivotX = rt != null ? rt.pivot.x : 0.5f;
        float pivotY = rt != null ? rt.pivot.y : 0.5f;
        col.center = new Vector3((0.5f - pivotX) * w, (0.5f - pivotY) * h, 0f);
    }

    public static OnlineClick Button(Transform parent, string label, Vector2 pos, Vector2 size, Color fill, Action<int> click)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(ButtonInteraction), typeof(OnlineClick));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = fill;
        image.sprite = MenuSprite();
        image.type = Image.Type.Sliced;
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = BlueOutline;
        outline.effectDistance = new Vector2(4.2f, 3.9f);
        ButtonInteraction interaction = go.GetComponent<ButtonInteraction>();
        interaction.highlightColor = Color.white;
        interaction.pressedColor = new Color(0.06f, 0.24f, 0.44f, 1f);
        interaction.fade = true;
        SelectorClickable.Ensure(go, 80f);
        Text text = Label(rt, label, 22, Color.black);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        OnlineClick hook = go.GetComponent<OnlineClick>();
        hook.clicked = click;
        return hook;
    }

    public static InputField Field(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size, bool password)
    {
        RectTransform root = Panel(parent, name, new Vector2(0.5f, 0.5f), pos, size, Color.white);
        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 1f);
        Outline outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = BlueOutline;
        outline.effectDistance = new Vector2(3f, 3f);
        SelectorClickable.Ensure(root.gameObject, 40f);

        InputField field = root.gameObject.AddComponent<InputField>();
        Text text = Label(root, "", 22, Color.white);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(12f, 6f);
        text.rectTransform.offsetMax = new Vector2(-12f, -6f);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        Text hint = Label(root, placeholder, 20, new Color(1f, 1f, 1f, 0.45f));
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(12f, 6f);
        hint.rectTransform.offsetMax = new Vector2(-12f, -6f);
        hint.alignment = TextAnchor.MiddleLeft;
        hint.fontStyle = FontStyle.Italic;
        field.textComponent = text;
        field.placeholder = hint;
        field.lineType = InputField.LineType.SingleLine;
        field.shouldActivateOnSelect = false;
        if (password)
            field.contentType = InputField.ContentType.Password;
        root.gameObject.AddComponent<OnlineFieldFocus>().field = field;
        return field;
    }

    public static Text Label(Transform parent, string value, int size, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 40f);
        return text;
    }

    public static void SetButtonColors(OnlineClick button, Color fill, Color outlineColor, bool active)
    {
        if (button == null)
            return;
        Image image = button.GetComponent<Image>();
        Outline outline = button.GetComponent<Outline>();
        ButtonInteraction interaction = button.GetComponent<ButtonInteraction>();
        if (image != null)
            image.color = active ? fill : Color.Lerp(fill, Gray, 0.65f);
        if (outline != null)
            outline.effectColor = outlineColor;
        if (interaction != null)
        {
            interaction.SetBaseColors(image != null ? image.color : fill, outlineColor);
            if (active)
                interaction.Activate();
            else
                interaction.Deactivate();
        }
    }
}

public class OnlineFieldFocus : MonoBehaviour
{
    public InputField field;

    static readonly List<OnlineFieldFocus> order = new List<OnlineFieldFocus>();
    static OnlineFieldFocus current;

    public static bool Typing => current != null && current.isActiveAndEnabled && current.field != null;

    public static OnlineFieldFocus At(Vector2 screen)
    {
        OnlineFieldFocus best = null;
        float bestArea = float.MaxValue;
        for (int i = 0; i < order.Count; i++)
        {
            OnlineFieldFocus focus = order[i];
            if (focus == null || !focus.isActiveAndEnabled)
                continue;
            RectTransform rt = focus.GetComponent<RectTransform>();
            if (rt == null)
                continue;
            Canvas canvas = focus.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, cam, out Vector2 local))
                continue;
            Rect area = rt.rect;
            area.xMin -= 36f;
            area.xMax += 36f;
            area.yMin -= 28f;
            area.yMax += 28f;
            if (!area.Contains(local))
                continue;
            float size = Mathf.Abs(rt.rect.width * rt.rect.height);
            if (size < bestArea)
            {
                bestArea = size;
                best = focus;
            }
        }
        return best;
    }

    bool sized;

    void OnEnable()
    {
        if (!order.Contains(this))
            order.Add(this);
        sized = false;
    }

    void OnDisable()
    {
        order.Remove(this);
        if (current == this)
            current = null;
    }

    public void OnClick(int player)
    {
        Focus();
    }

    static Keyboard hooked;
    static bool textEventThisFrame;

    public void Focus()
    {
        if (current != null && current != this)
            current.SetSelected(false);
        current = this;
        SetSelected(true);
        if (field != null)
            field.DeactivateInputField();
        HookKeyboard();
        PlayerInputManager manager = PlayerInputManager.instance;
        if (manager != null && manager.joiningEnabled)
            manager.DisableJoining();
    }

    static void HookKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (hooked == keyboard)
            return;
        if (hooked != null)
            hooked.onTextInput -= OnTextInput;
        hooked = keyboard;
        hooked.onTextInput += OnTextInput;
    }

    static void OnTextInput(char typed)
    {
        if (current == null || current.field == null)
            return;
        if (typed == '\b')
        {
            current.DeleteBack();
            textEventThisFrame = true;
            return;
        }
        if (char.IsControl(typed))
            return;
        current.Insert(typed);
        textEventThisFrame = true;
    }

    public static void Clear()
    {
        if (current != null)
        {
            current.SetSelected(false);
            if (current.field != null)
                current.field.DeactivateInputField();
        }
        current = null;
    }

    void SetSelected(bool selected)
    {
        Outline outline = GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = selected ? Color.white : OnlineWidgets.BlueOutline;
    }

    void Update()
    {
        HookKeyboard();
        SelectorClickable.Ensure(gameObject);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size += new Vector3(72f, 56f, 0f);
            sized = true;
        }
        if (current != this || field == null)
            return;

        Keyboard keys = Keyboard.current;
        if (keys == null)
            return;

        if (keys.tabKey.wasPressedThisFrame)
        {
            FocusNext(keys.shiftKey.isPressed);
            textEventThisFrame = false;
            return;
        }
        if (keys.escapeKey.wasPressedThisFrame)
        {
            Clear();
            textEventThisFrame = false;
            return;
        }

        bool shift = keys.shiftKey.isPressed;
        bool ctrl = keys.ctrlKey.isPressed || keys.leftCommandKey.isPressed || keys.rightCommandKey.isPressed;
        if (!textEventThisFrame && keys.backspaceKey.wasPressedThisFrame)
        {
            DeleteBack();
            return;
        }
        if (keys.deleteKey.wasPressedThisFrame)
        {
            DeleteForward();
            textEventThisFrame = false;
            return;
        }
        if (keys.leftArrowKey.wasPressedThisFrame)
        {
            field.caretPosition = Mathf.Max(0, field.caretPosition - 1);
            textEventThisFrame = false;
            return;
        }
        if (keys.rightArrowKey.wasPressedThisFrame)
        {
            field.caretPosition = Mathf.Min(field.text.Length, field.caretPosition + 1);
            textEventThisFrame = false;
            return;
        }
        if (!ctrl && !textEventThisFrame)
        {
            foreach (KeyControl key in keys.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame)
                    continue;
                char typed = ToChar(key.keyCode, shift);
                if (typed != '\0')
                    Insert(typed);
            }
        }
        textEventThisFrame = false;
    }

    void FocusNext(bool backward)
    {
        if (order.Count == 0)
            return;
        int index = order.IndexOf(this);
        if (index < 0)
            index = 0;
        int step = backward ? -1 : 1;
        for (int n = 1; n <= order.Count; n++)
        {
            int next = (index + step * n) % order.Count;
            if (next < 0)
                next += order.Count;
            OnlineFieldFocus candidate = order[next];
            if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.field != null)
            {
                candidate.Focus();
                return;
            }
        }
    }

    void Insert(char typed)
    {
        string text = field.text ?? "";
        int caret = Mathf.Clamp(field.caretPosition, 0, text.Length);
        field.text = text.Insert(caret, typed.ToString());
        field.caretPosition = caret + 1;
        field.ForceLabelUpdate();
    }

    void DeleteBack()
    {
        string text = field.text ?? "";
        int caret = Mathf.Clamp(field.caretPosition, 0, text.Length);
        if (caret <= 0 || text.Length == 0)
            return;
        field.text = text.Remove(caret - 1, 1);
        field.caretPosition = caret - 1;
        field.ForceLabelUpdate();
    }

    void DeleteForward()
    {
        string text = field.text ?? "";
        int caret = Mathf.Clamp(field.caretPosition, 0, text.Length);
        if (caret >= text.Length)
            return;
        field.text = text.Remove(caret, 1);
        field.caretPosition = caret;
        field.ForceLabelUpdate();
    }

    static char ToChar(Key key, bool shift)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            char letter = (char)('a' + (key - Key.A));
            return shift ? char.ToUpperInvariant(letter) : letter;
        }
        if (key >= Key.Digit1 && key <= Key.Digit9)
        {
            int digit = (key - Key.Digit1) + 1;
            if (!shift)
                return (char)('0' + digit);
            switch (digit)
            {
                case 1: return '!';
                case 2: return '@';
                case 3: return '#';
                case 4: return '$';
                case 5: return '%';
                case 6: return '^';
                case 7: return '&';
                case 8: return '*';
                default: return '(';
            }
        }
        if (key == Key.Digit0)
            return shift ? ')' : '0';
        if (key >= Key.Numpad0 && key <= Key.Numpad9)
            return (char)('0' + (key - Key.Numpad0));

        switch (key)
        {
            case Key.Space: return ' ';
            case Key.Minus: return shift ? '_' : '-';
            case Key.Equals: return shift ? '+' : '=';
            case Key.LeftBracket: return shift ? '{' : '[';
            case Key.RightBracket: return shift ? '}' : ']';
            case Key.Backslash: return shift ? '|' : '\\';
            case Key.Semicolon: return shift ? ':' : ';';
            case Key.Quote: return shift ? '"' : '\'';
            case Key.Comma: return shift ? '<' : ',';
            case Key.Period: return shift ? '>' : '.';
            case Key.Slash: return shift ? '?' : '/';
            case Key.Backquote: return shift ? '~' : '`';
            case Key.NumpadPlus: return '+';
            case Key.NumpadMinus: return '-';
            case Key.NumpadMultiply: return '*';
            case Key.NumpadDivide: return '/';
            case Key.NumpadPeriod: return '.';
            default: return '\0';
        }
    }
}
