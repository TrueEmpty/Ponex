using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Ball
{
    public string name = "";
    public GameObject prefab;
    public GameObject selection;
    public Color color = Color.red;

    public float startSpeed = 1;
    public float minSpeed = 1;
    public float maxSpeed = 2;
    public float speedIncrease = .5f;

    public int damage = 1;

    public Ball()
    {

    }

    public Ball(Ball ball)
    {
        name = ball.name;
        prefab = ball.prefab;
        selection = ball.selection;

        startSpeed = ball.startSpeed;
        minSpeed = ball.minSpeed;
        maxSpeed = ball.maxSpeed;
        speedIncrease = ball.speedIncrease;

        damage = ball.damage;
    }

    /// <summary>Highest speed bumps can push the ball toward.</summary>
    public float BumpSpeedCap => maxSpeed * 2f;
}
