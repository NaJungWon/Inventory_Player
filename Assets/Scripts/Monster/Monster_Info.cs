using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Monster_Info")]
public class Monster_Info : ScriptableObject
{
    public string monsterName;
    public int monsterHp;
    public int monsterDamage;
    public float attackRange;
    public float monsterSpeed;
    public GameObject monster;
}
