using UnityEngine;
using UnityEngine.UI;

public class ChangeCount : MonoBehaviour
{
    void Awake()
    {
        StaticSlot.ChangeText = GetComponent<Text>();
        StaticSlot.UpdateText();
    }
}
