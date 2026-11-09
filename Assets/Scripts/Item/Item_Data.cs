using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item_Data")]
public class Item_Data : ScriptableObject
{
    [Header("아이템 이름/레벨/타입/이미지/수치")]
    public string itemName;
    public int itemLevel;
    public ItemType itemType;
    public List<Sprite> itemImages;
    public WeaponType weaponType;
    public int itemValue;
    public float itemCooldown;
    [Header("배치 아이템 프리팹")]
    public GameObject item;
    [Header("전투 아이템 프리팹")]
    public GameObject battleItem;

    public enum ItemType
    {
        Item,
        Slot
    }

    public enum WeaponType
    {
        None,
        Throw,
        Shoot,
        Heal
    }
}
