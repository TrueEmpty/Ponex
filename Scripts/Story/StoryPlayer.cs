using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class StoryPlayer : MonoBehaviour
{
    public float walkSpeed = 4.2f;
    public float runSpeed = 8f;
    public float jumpSpeed = 7f;
    public float gravity = -22f;

    CharacterController body;
    StoryWorld world;
    float vertical;
    public bool cutsceneLocked;

    public void Setup(StoryWorld owner)
    {
        world = owner;
        body = GetComponent<CharacterController>();
        if (body == null)
            body = gameObject.AddComponent<CharacterController>();
        body.height = 2f;
        body.radius = 0.45f;
        body.center = new Vector3(0f, 1f, 0f);
    }

    void Update()
    {
        if (body == null)
            return;

        ReadInput(out Vector2 move, out bool sprint, out bool jump, out bool interact, out bool menu);

        if (menu && world != null)
            world.TogglePause();

        if (cutsceneLocked || (world != null && world.Paused))
        {
            vertical = 0f;
            return;
        }

        if (interact && world != null)
            world.Interact();

        Transform cam = world != null ? world.CameraTransform : null;
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 wish = forward * move.y + right * move.x;
        if (wish.sqrMagnitude > 1f)
            wish.Normalize();

        float speed = sprint ? runSpeed : walkSpeed;
        if (body.isGrounded && vertical < 0f)
            vertical = -2f;
        if (body.isGrounded && jump)
            vertical = jumpSpeed;
        vertical += gravity * Time.deltaTime;

        Vector3 velocity = wish * speed;
        velocity.y = vertical;
        body.Move(velocity * Time.deltaTime);

        if (wish.sqrMagnitude > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(wish, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 12f * Time.deltaTime);
        }
    }

    public static void ReadInput(out Vector2 move, out bool sprint, out bool jump, out bool interact, out bool menu)
    {
        move = Vector2.zero;
        sprint = false;
        jump = false;
        interact = false;
        menu = false;

        Database db = Database.instance;
        if (db != null && db.controllers != null)
        {
            for (int i = 0; i < db.controllers.Count; i++)
            {
                ControllerLink link = db.controllers[i];
                if (link == null)
                    continue;
                ControllerButtons stick = link["Move"];
                if (stick != null)
                    move += stick.value;
                sprint |= Pressed(link, "Sprint");
                jump |= PressedDown(link, "Jump");
                interact |= PressedDown(link, "Interact");
                menu |= PressedDown(link, "Menu");
            }
        }

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        Keyboard keys = Keyboard.current;
        if (keys == null)
            return;
        if (move.sqrMagnitude < 0.04f)
        {
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) move.y += 1f;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) move.y -= 1f;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) move.x += 1f;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) move.x -= 1f;
        }
        sprint |= keys.leftShiftKey.isPressed;
        jump |= keys.spaceKey.wasPressedThisFrame;
        interact |= keys.eKey.wasPressedThisFrame;
        menu |= keys.enterKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame;
    }

    public static bool JumpHeld()
    {
        Database db = Database.instance;
        if (db != null && db.controllers != null)
        {
            for (int i = 0; i < db.controllers.Count; i++)
            {
                if (Pressed(db.controllers[i], "Jump"))
                    return true;
            }
        }
        Keyboard keys = Keyboard.current;
        return keys != null && keys.spaceKey.isPressed;
    }

    static bool Pressed(ControllerLink link, string action)
    {
        if (link == null)
            return false;
        ControllerButtons button = link[action];
        return button != null && button.isPressed;
    }

    static bool PressedDown(ControllerLink link, string action)
    {
        if (link == null)
            return false;
        ControllerButtons button = link[action];
        return button != null && button.wasPressedThisFrame;
    }
}
