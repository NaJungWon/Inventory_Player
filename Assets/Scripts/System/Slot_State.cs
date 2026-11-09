using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Slot_State : MonoBehaviour
{
    public SlotState slotState;
    public Image image;
    public TextMeshProUGUI text;
    
    [Header("슬롯 색상")]
    public Color defaultColor; // 기본 색상
    public Color lockedColor; // 잠금 색상
    public Color canoccupiedColor; // 차지 가능 색상
    public Color cannotoccupiedColor; // 차지 불가능 색상
    public bool hasDefaultColor;
    public enum SlotState
    {
        None,
        Empty,
        Locked,
        Occupied
    }
    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            defaultColor = image.color;
            hasDefaultColor = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Color GetBaseColor()
    {
        if (hasDefaultColor) return defaultColor;
        if (image != null) return image.color;
        return Color.white;
    }
}
