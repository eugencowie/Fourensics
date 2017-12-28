using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FancyInventory : MonoBehaviour
{
    [SerializeField] private GameObject SlotTemplate = null;
    [SerializeField] private GameObject MainScreen = null;
    [SerializeField] private GameObject InspectScreen = null;
    [SerializeField] private Text HintText = null;
    [SerializeField] private Image HintImage = null;

    private List<GameObject> m_buttons = new List<GameObject>();

    private Vector3 m_initialPosition;
    private const int m_spacing = 235;
    private const float m_scrollSpeed = 2000;
    private float m_scrollAmount = 0;

    //private Vector3 m_touchOrigin;
    //private bool m_isSwiping;

    private void Start()
    {
        m_initialPosition = transform.localPosition;

        foreach (var button in m_buttons)
        {
            Destroy(button);
        }

        m_buttons.Clear();

        LoadFromStaticInv();
    }

    public void LoadFromStaticInv()
    {
        foreach (var item in StaticInventory.Hints)
        {
            ObjectHintData data = item;
            AddItem(() => ItemButtonPressed(data), data);
        }
    }

    private void ItemButtonPressed(ObjectHintData hint)
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

    /*private void Update()
    {
        if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
        {
            m_touchOrigin = Input.mousePosition;
            m_isSwiping = true;
        }

        if (!Input.GetMouseButton(0))
        {
            m_isSwiping = false;
        }

        if (m_isSwiping)
        {
            Vector3 screenPos = Camera.main.ScreenToViewportPoint(Input.mousePosition - m_touchOrigin);

            float movement = screenPos.normalized.y * m_scrollSpeed * Time.deltaTime;

            if (Mathf.Abs(movement) > 20)
            {
                Scroll(movement);
            }
        }
    }*/

    public void AddItem(UnityAction itemAction, ObjectHintData item)
    {
        if (!m_buttons.Any(b => b.name == item.Name))
        {
            // Add item to static inventory
            if (!StaticInventory.Hints.Any(h => h.Name == item.Name))
            {
                StaticInventory.Hints.Add(new ObjectHintData(item.Name, item.Hint, item.Image)); // TODO
            }

            // Create new button
            GameObject newSlot = Instantiate(SlotTemplate);
            newSlot.name = item.Name;

            // Set initial transform
            newSlot.transform.SetParent(transform);
            newSlot.transform.localScale = Vector3.one;

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

            // Scroll inventory to show the new item
            float maxScrollAmount = Mathf.Max(0, (m_buttons.Count * m_spacing) - (m_spacing * 5));
            ScrollTo(maxScrollAmount);
        }
    }

    public void AddItems(UnityAction itemAction, ObjectHint[] items)
    {
        foreach (var item in items)
        {
            AddItem(itemAction, new ObjectHintData(item));
        }
    }

    public void ScrollUpButtonPressed()
    {
        Scroll(-m_spacing);
    }

    public void ScrollDownButtonPressed()
    {
        Scroll(m_spacing);
    }

    private void Scroll(float movement)
    {
        ScrollTo(m_scrollAmount + movement);
    }

    private void ScrollTo(float position)
    {
        m_scrollAmount = position;

        float maxScrollAMount = Mathf.Max(0, (m_buttons.Count * m_spacing) - (m_spacing * 5));
        m_scrollAmount = Mathf.Clamp(m_scrollAmount, 0, maxScrollAMount);

        transform.localPosition = m_initialPosition + new Vector3(0, m_scrollAmount, 0);
    }
}
