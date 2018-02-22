using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VotingDatabaseScene : MonoBehaviour
{
    public GameObject MainScreen, WaitScreen;

    //[SerializeField] private GameObject VotingButton = null;
    [SerializeField] private GameObject ButtonTemplate = null;
    [SerializeField] private GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] private Data[] Data = new Data[4];

    private string m_lobbyCode;
    private int m_scene;

    int playerItemsLoaded = 0;

    async void Start()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        MainScreen.SetActive(false);
        WaitScreen.SetActive(true);

        int scene = (int)(CloudManager.OnlyUser(m_lobby, m_user).Scene.Value ?? 0);
        if (scene > 0)
        {
            m_scene = scene;
            SetBackground();
            string lobby = m_lobby.Id;
            if (!string.IsNullOrEmpty(lobby))
            {
                m_lobbyCode = lobby;
                await DownloadItems();
                await RegisterListeners();
            }
            else SceneManager.LoadScene("Lobby");
        }
        else SceneManager.LoadScene("Lobby");

        for (int i = 0; i < Data.Length; i++)
        {
            Data data = Data[i];
            data.PlayerButton.GetComponent<Button>().onClick.AddListener(() => PlayerButtonPressed(data));
        }

        PlayerButtonPressed(Data[0]);
    }

    private async Task RegisterListeners()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUserItem clue in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged += OnSlotChanged;
    }

    private void SetBackground()
    {
        if (m_scene <= Backgrounds.Length)
        {
            foreach (var bg in Backgrounds)
                bg.SetActive(false);

            Backgrounds[m_scene - 1].SetActive(true);
        }
    }

    public void VotingButtonPressed()
    {
        SceneManager.LoadScene("Voting");
    }

    private void PlayerButtonPressed(Data data)
    {
        foreach (var button in Data.Select(d => d.PlayerButton))
        {
            ColorBlock colours = button.GetComponent<Button>().colors;
            colours.normalColor = colours.highlightedColor = Color.white;
            button.GetComponent<Button>().colors = colours;
        }
        ColorBlock colours2 = data.PlayerButton.GetComponent<Button>().colors;
        colours2.normalColor = colours2.highlightedColor = Color.green;
        data.PlayerButton.GetComponent<Button>().colors = colours2;

        foreach (var cluePanel in Data.Select(d => d.CluePanel))
        {
            cluePanel.SetActive(false);
        }
        data.CluePanel.SetActive(true);
    }

    private void CheckPlayerItemsLoaded()
    {
        playerItemsLoaded++;
        Debug.Log(playerItemsLoaded);

        //if (playerItemsLoaded >= 24)
        {
            WaitScreen.SetActive(false);
            MainScreen.SetActive(true);
        }
    }

    private async Task DownloadItems()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        int tmp = 0;
        //User player = await CloudManager.DownloadClues(tmp);
        for (int j = 0; j < CloudManager.OnlyUser(m_lobby, m_user).Items.Length; j++)
        {
            int tmp2 = j;
            var clue = CloudManager.OnlyUser(m_lobby, m_user).Items[tmp2];
            CheckPlayerItemsLoaded();
            if (!string.IsNullOrEmpty(clue.Name.Value))
            {
                var slot = Data[tmp].Slots[tmp2];
                foreach (Transform t in slot.transform) if (t.gameObject.name == clue.Name.Value) Destroy(t.gameObject);
                var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                newObj.SetActive(true);
                newObj.name = clue.Name.Value;
                newObj.transform.SetParent(slot.transform);
                if (!string.IsNullOrEmpty(clue.Image.Value))
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Value;
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(clue.Image.Value);
                        }
                    }
                }
                else
                {
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = clue.Name.Value;
                            t.gameObject.GetComponent<Text>().gameObject.SetActive(true);
                        }
                        if (t.gameObject.GetComponent<Image>() != null)
                        {
                            t.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                        }
                    }
                }
                newObj.GetComponent<DragHandler>().enabled = false;
                slot.GetComponent<Slot>().Text.GetComponent<Text>().text = clue.Description.Value;
            }
        }
    }

    private async void OnSlotChanged(CloudNode entry)
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        string player = m_lobby.Users.First(x => x.Id == entry.Key.Parent.Parent.Parent.Id).UserId.Value;
        string field = entry.Key.Id;

        if (entry.Value != null)
        {
            string value = entry.Value;
            Debug.Log(entry.Key + " = " + value);

            int slotNb = -1;
            if (int.TryParse(entry.Key.Parent.Id, out slotNb))
            {
                int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);
                var slot = Data[playerNb].Slots[slotNb - 1];
                if (field == "name")
                {
                    foreach (Transform t in slot.transform) if (t.gameObject.name == value) Destroy(t.gameObject);
                    var newObj = Instantiate(ButtonTemplate, ButtonTemplate.transform.parent);
                    newObj.SetActive(true);
                    newObj.name = value;
                    newObj.transform.SetParent(slot.transform);
                    foreach (Transform t in newObj.transform)
                    {
                        if (t.gameObject.GetComponent<Text>() != null)
                        {
                            t.gameObject.GetComponent<Text>().text = value;
                        }
                    }
                    newObj.GetComponent<DragHandler>().enabled = false;
                }
                else if (field == "hint")
                {
                    slot.GetComponent<Slot>().Text.GetComponent<Text>().text = value;
                }
                else if (field == "image")
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        foreach (Transform t1 in slot.transform)
                        {
                            foreach (Transform t in t1)
                            {
                                if (t.gameObject.GetComponent<Image>() != null)
                                {
                                    t.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(value);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (Transform t1 in slot.transform)
                        {
                            foreach (Transform t in t1)
                            {
                                if (t.gameObject.GetComponent<Image>() != null)
                                {
                                    t.gameObject.GetComponent<Image>().gameObject.SetActive(false);
                                }
                                if (t.gameObject.GetComponent<Text>() != null)
                                {
                                    t.gameObject.GetComponent<Text>().gameObject.SetActive(true); // TODO: REMOVE TEMP FIX
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log(entry.Key + " removed");

            int slotNb = -1;
            if (int.TryParse(entry.Key.Parent.Id, out slotNb))
            {
                int playerNb = CloudManager.GetPlayerNumber(m_user, m_lobby, player);
                var slot = Data[playerNb].Slots[slotNb - 1];

                slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";

                foreach (Transform t1 in slot.transform)
                {
                    Destroy(t1.gameObject);
                }
            }
        }
    }
}
