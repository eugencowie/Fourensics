using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public static class StaticInventory{
    
    [SerializeField]
    public static List<ObjectHintData> Hints = new List<ObjectHintData>();

    public static void Reset()
    {
        Hints.Clear();
    }
}

public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject ButtonContainer = null;
    [SerializeField] private GameObject Button = null;

    private List<GameObject> m_buttons = new List<GameObject>();
    private const int m_spacing = 235;
    private const float m_scrollSpeed = 2000;
    private float m_scrollAmount = 0;

    //private Vector3 m_touchOrigin;
    //private bool m_isSwiping;

    private void Start()
    {
        foreach (var button in m_buttons)
        {
            Destroy(button);
        }

        m_buttons.Clear();
        
        foreach (var item in StaticInventory.Hints)
        {
            AddItem(null, item);
        }
    }

    private void Update()
    {
        /*if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
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
        }*/
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

            // Create new button
            GameObject newButton = Instantiate(Button);
            newButton.name = item.Name;

            // Set click method
            if (itemAction != null)
            {
                newButton.GetComponent<Button>().onClick.AddListener(itemAction);
            }

            // Set initial transform
            newButton.transform.SetParent(ButtonContainer.transform);
            newButton.transform.localPosition = Button.transform.localPosition;
            newButton.transform.localRotation = Button.transform.localRotation;
            newButton.transform.localScale = Button.transform.localScale;

            // Set button position
            newButton.transform.localPosition -= new Vector3(0, m_spacing * m_buttons.Count, 0);

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

        float maxScrollAmount = Mathf.Max(0, (m_buttons.Count * m_spacing) - (m_spacing * 5));
        m_scrollAmount = Mathf.Clamp(m_scrollAmount, 0, maxScrollAmount);

        ButtonContainer.transform.localPosition = new Vector3(0, m_scrollAmount, 0);
    }
}
