using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncreaseDamageOnBallHit : MonoBehaviour
{
    string tagId = "Ball";

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag.ToLower().Trim() == tagId.ToLower().Trim())
        {
            BallInfo bI = collision.gameObject.GetComponent<BallInfo>();

            if (bI != null)
            {
                bI.ball.damage ++;
            }
        }
    }
}
