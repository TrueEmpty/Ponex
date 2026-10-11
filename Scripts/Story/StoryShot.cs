using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One cutscene's finished layout. Playing it and skipping it both end by applying this.
/// </summary>
public class StoryShot
{
    public Vector3 cameraFrom;
    public Vector3 cameraTo;
    public Vector3 lookAt;
    public bool placePlayer;
    public Vector3 playerPosition;
    public Quaternion playerRotation = Quaternion.identity;
    public readonly List<ShotObject> objects = new List<ShotObject>();

    public StoryShot Place(Transform actor, Vector3 position, Quaternion rotation)
    {
        placePlayer = actor != null;
        playerPosition = position;
        playerRotation = rotation;
        return this;
    }

    public StoryShot Camera(Vector3 from, Vector3 to, Vector3 look)
    {
        cameraFrom = from;
        cameraTo = to;
        lookAt = look;
        return this;
    }

    public StoryShot SetObject(GameObject target, bool active, bool move = false, Vector3 position = default, Quaternion rotation = default)
    {
        if (target == null)
            return this;
        objects.Add(new ShotObject
        {
            target = target,
            active = active,
            move = move,
            position = position,
            rotation = rotation
        });
        return this;
    }

    public void Apply(Camera cam, Transform player)
    {
        if (placePlayer && player != null)
        {
            CharacterController body = player.GetComponent<CharacterController>();
            if (body != null)
                body.enabled = false;
            player.SetPositionAndRotation(playerPosition, playerRotation);
            if (body != null)
                body.enabled = true;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            ShotObject item = objects[i];
            if (item == null || item.target == null)
                continue;
            if (item.move)
                item.target.transform.SetPositionAndRotation(item.position, item.rotation);
            item.target.SetActive(item.active);
        }

        if (cam != null)
        {
            cam.transform.position = cameraTo;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - cameraTo, Vector3.up);
        }
    }
}

public class ShotObject
{
    public GameObject target;
    public bool active = true;
    public bool move;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
}
