using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

class FancyInventory : MonoBehaviour
{
    [SerializeField] GameObject SlotTemplate = null;
    [SerializeField] GameObject MainScreen = null;
    [SerializeField] GameObject InspectScreen = null;
    [SerializeField] Text HintText = null;
    [SerializeField] Image HintImage = null;

    List<GameObject> m_buttons = new List<GameObject>();
    const int m_spacing = 235;
    const float m_scrollSpeed = 2000;

    void Start()
    {
        foreach (var button in m_buttons)
        {
            // Decrease container size
            GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y - 250);

            Destroy(button);
        }

        m_buttons.Clear();
        List<ObjectHintData> copy = StaticInventory.Hints.ToList();
        StaticInventory.Hints.Clear();
        foreach (var item in copy)
        {
            ObjectHintData data = item;
            AddItem(() => ItemButtonPressed(data), data);
        }
    }

    void ItemButtonPressed(ObjectHintData hint)
    {
        if (DragHandler.itemBeingDragged == null)
        {
            if (!string.IsNullOrEmpty(hint.Image))
            {
                HintImage.sprite = Resources.Load<Sprite>(hint.Image);
                HintImage.gameObject.SetActive(true);
            }
            else HintImage.gameObject.SetActive(false);

            HintText.text = hint.Hint;
            MainScreen.SetActive(false);
            InspectScreen.SetActive(true);
        }
    }

    public void AddItem(UnityAction itemAction, ObjectHintData item)
    {
        if (!m_buttons.Any(b => b.name == item.Name))
        {
            // Add item to static inventory
            if (!StaticInventory.Hints.Any(h => h.Name == item.Name))
            {
                StaticInventory.Hints.Add(new ObjectHintData(item.Name, item.Hint, item.Image)); // TODO
            }
            else return;

            // Increase button container size
            GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y + 250);

            // Create new button
            GameObject newSlot = Instantiate(SlotTemplate);
            newSlot.name = item.Name;

            // Set initial transform
            newSlot.transform.SetParent(transform);
            newSlot.transform.localPosition = SlotTemplate.transform.localPosition;
            newSlot.transform.localRotation = SlotTemplate.transform.localRotation;
            newSlot.transform.localScale = SlotTemplate.transform.localScale;

            // Set button position
            newSlot.transform.localPosition -= new Vector3(0, m_spacing * m_buttons.Count, 0);

            // Set click method
            foreach (Transform t in newSlot.transform)
            {
                if (t.GetComponent<Button>() != null)
                {
                    t.name = item.Name;

                    if (itemAction != null)
                    {
                        t.GetComponent<Button>().onClick.AddListener(itemAction);
                    }
                }
            }

            // Set button text
            foreach (Transform t in newSlot.transform)
            {
                foreach (Transform t2 in t)
                {
                    if (t2.gameObject.GetComponent<Text>() != null)
                    {
                        t2.gameObject.GetComponent<Text>().text = item.Name;
                    }
                    else if (t2.gameObject.GetComponent<Image>() != null)
                    {
                        if (!string.IsNullOrEmpty(item.Image))
                        {
                            t2.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(item.Image);
                        }
                        else
                        {
                            foreach (Transform t3 in t)
                            {
                                if (t3.gameObject.GetComponent<Text>() != null)
                                {
                                    t3.gameObject.GetComponent<Text>().gameObject.SetActive(true); // TODO: REMOVE TEMP FIX
                                    t2.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }

            // Activate button
            newSlot.SetActive(true);
            m_buttons.Add(newSlot);
        }
    }

    public void AddItems(UnityAction itemAction, ObjectHint[] items)
    {
        foreach (var item in items)
        {
            AddItem(itemAction, new ObjectHintData(item));
        }
    }
}
