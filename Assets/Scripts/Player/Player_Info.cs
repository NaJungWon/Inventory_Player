using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Player_Info")]
public class Player_Info : ScriptableObject
{
    public string playerName;
    public Sprite playerSprite;
    public int playerHp;
    public int playerDamage;
}
