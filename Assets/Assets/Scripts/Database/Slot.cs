using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class StaticSlot
{
    // ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓ ↙ ↙ ↙ 
    public static int MaxRemovals = 5; // ← ← 
    // ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑ ↖ ↖ ↖
    private static int m_TimesRemoved;
    public static int TimesRemoved
    {
        get
        {
            return m_TimesRemoved;
        }
        set
        {
            m_TimesRemoved = value;
            UpdateText();
        }
    }

    public static void UpdateText()
    {
        if (ChangeText != null)
        {
            ChangeText.text = "Changes Remaining : " + (5 - m_TimesRemoved);
        }
    }

    public static Text ChangeText;

    public static void Reset()
    {
        MaxRemovals = 5;
        m_TimesRemoved = 0;
    }
}

class Slot : MonoBehaviour, IDropHandler
{
    public GameObject item
    {
        get
        {
            if (transform.childCount > 0)
            {
                return transform.GetChild(0).gameObject;
            }
            return null;
        }
    }

    public bool CanDrop = false;

    private AudioSource m_audioSource;
    public AudioClip emailAudioClip;

    public GameObject Text;
    public Button EditButton;

    public GameObject MainScreen;
    public EditItemText EditItemText;

    public DatabaseScene DatabaseController;

    [Range(1, 6)]
    public int SlotNumber;

    private void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
    }

    #region IDropHandler implementation
    public async void OnDrop(PointerEventData eventData)
    {
        if (item == null && CanDrop)
        {
            string newName = DragHandler.itemBeingDragged.name;
            if (StaticInventory.Hints.Any(h => h.Name == newName))
            {
                m_audioSource.PlayOneShot(emailAudioClip, 1f);
                ObjectHintData hint = StaticInventory.Hints.First(h => h.Name == newName);

                GameObject newObject = Instantiate(DragHandler.itemBeingDragged, DragHandler.itemBeingDragged.transform.parent);
                newObject.name = DragHandler.itemBeingDragged.name;
                newObject.transform.SetParent(transform);

                newObject.GetComponent<Image>().raycastTarget = true;

                newObject.GetComponent<Button>().onClick.AddListener(async () => {
                    if (CanDrop && StaticSlot.TimesRemoved < StaticSlot.MaxRemovals)
                    {
                        Text.GetComponent<Text>().text = "";
                        EditButton.gameObject.SetActive(false);
                        EditButton.onClick.RemoveAllListeners();
                        await DatabaseController.RemoveItem(SlotNumber);
                        Destroy(newObject);
                        StaticSlot.TimesRemoved++;
                    }
                    else Debug.Log("YOU CANT GO THERE (EG. you have removed your maximum amount of times)");
                });

                newObject.GetComponent<DragHandler>().enabled = false;

                Text.GetComponent<Text>().text = hint.Hint;
                EditButton.gameObject.SetActive(true);
                EditButton.onClick.AddListener(() => {
                    MainScreen.SetActive(false);
                    EditItemText.gameObject.SetActive(true);
                    EditItemText.SetTextField(hint.Hint);
                    EditItemText.OnCancel = () => {
                        EditItemText.OnCancel = null;
                        EditItemText.OnSubmit = null;
                        EditItemText.gameObject.SetActive(false);
                        MainScreen.SetActive(true);
                    };
                    EditItemText.OnSubmit = async newText => {
                        EditItemText.OnCancel = null;
                        EditItemText.OnSubmit = null;
                        EditItemText.gameObject.SetActive(false);
                        MainScreen.SetActive(true);
                        hint.Hint = newText;
                        Text.GetComponent<Text>().text = hint.Hint;
                        await DatabaseController.UploadItem(SlotNumber, hint);
                    };
                });

                await DatabaseController.UploadItem(SlotNumber, hint);
            }
        }
    }
    #endregion
}
