using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

static class StaticInventory
{
    [SerializeField]
    public static List<ObjectHintData> Hints = new List<ObjectHintData>();

    public static void Reset()
    {
        Hints.Clear();
    }
}

class Inventory : MonoBehaviour
{
    [SerializeField] GameObject ButtonContainer = null;
    [SerializeField] GameObject Button = null;

    List<GameObject> m_buttons = new List<GameObject>();
    const int m_spacing = 235;
    const float m_scrollSpeed = 2000;

    void Start()
    {
        foreach (var button in m_buttons)
        {
            // Decrease button container size
            ButtonContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonContainer.GetComponent<RectTransform>().sizeDelta.x, ButtonContainer.GetComponent<RectTransform>().sizeDelta.y - 250);

            Destroy(button);
        }

        m_buttons.Clear();

        List<ObjectHintData> copy = StaticInventory.Hints.ToList();
        StaticInventory.Hints.Clear();
        foreach (var item in copy)
        {
            AddItem(null, item);
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
            ButtonContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonContainer.GetComponent<RectTransform>().sizeDelta.x, ButtonContainer.GetComponent<RectTransform>().sizeDelta.y + 250);

            // Create new button
            GameObject newButton = Instantiate(Button);
            newButton.name = item.Name;

            // Set initial transform
            newButton.transform.SetParent(ButtonContainer.transform);
            newButton.transform.localPosition = Button.transform.localPosition;
            newButton.transform.localRotation = Button.transform.localRotation;
            newButton.transform.localScale = Button.transform.localScale;

            // Set button position
            newButton.transform.localPosition -= new Vector3(0, m_spacing * m_buttons.Count, 0);

            // Set click method
            if (itemAction != null)
            {
                newButton.GetComponent<Button>().onClick.AddListener(itemAction);
            }

            // Set button text
            foreach (Transform t in newButton.transform)
            {
                if (t.gameObject.GetComponent<Text>() != null)
                {
                    t.gameObject.GetComponent<Text>().text = item.Name;
                }
                else if (t.gameObject.GetComponent<Image>() != null)
                {
                    if (!string.IsNullOrEmpty(item.Image))
                    {
                        t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(item.Image);
                    }
                    else
                    {
                        foreach (Transform t2 in newButton.transform)
                        {
                            if (t2.gameObject.GetComponent<Text>() != null)
                            {
                                t2.gameObject.GetComponent<Text>().gameObject.SetActive(true); // TODO: REMOVE TEMP FIX
                                t.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }

            // Activate button
            newButton.SetActive(true);
            m_buttons.Add(newButton);
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
