using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    public enum EnemyType { Basic, Fast, Strong }
    public float speed;
    public float health;


    //optional
    public float damage;

    protected void OnTriggerMove()
    {
        //move enemy forward
    }
}
