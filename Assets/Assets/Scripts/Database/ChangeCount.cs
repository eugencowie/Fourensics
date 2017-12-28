using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class ChangeCount : MonoBehaviour {

    void Awake()
    {
        StaticSlot.ChangeText = GetComponent<Text>();
        StaticSlot.UpdateText();
    }
}
