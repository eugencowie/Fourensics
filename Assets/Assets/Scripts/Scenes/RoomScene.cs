using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticRoom
{
    public static bool SeenWelcome = false;

    public static void Reset()
    {
        SeenWelcome = false;
    }
}

public class RoomScene : MonoBehaviour
{
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject DatabaseButton = null;

    private string m_roomCode;

    public GameObject mainScreen;
    public GameObject welcomeScreen;

    async void Start()
    {
        if (StaticRoom.SeenWelcome)
        {
            mainScreen.SetActive(true);
            welcomeScreen.SetActive(false);
        }
        StaticRoom.SeenWelcome = true;

        ReadyButton.SetActive(false);
        DatabaseButton.SetActive(false);

        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);
        if (m_lobby == null)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }

        string room = m_lobby.Id;
        if (!string.IsNullOrEmpty(room))
        {
            m_roomCode = room;
            await RegisterListeners();
            ReadyButton.SetActive(true);
            DatabaseButton.SetActive(true);
        }
        else SceneManager.LoadScene("Lobby");
    }

    private async Task RegisterListeners()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);

        foreach (LobbyUserItem clue in m_lobby.Users.Where(u => u.UserId.Value != m_user.Id).Select(u => u.Items).SelectMany(i => i))
            clue.ValueChanged += OnSlotChanged;

        foreach (LobbyUser user in m_lobby.Users)
            user.Ready.ValueChanged += OnReadyChanged;
    }

    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            DatabaseButton.SetActive(false);
            SceneManager.LoadScene("Database");
        }
    }

    private async void OnReadyChanged(CloudNode<bool> entry)
    {
        Debug.Log($"RoomScene.OnReadyChanged({entry.Value})");

        if (ReadyButton == null)
            return;

        if (entry.Value != null)
        {
            bool value = entry.Value ?? false;

            if (value == true)
            {
                User m_user = await User.Get();
                Lobby m_lobby = await Lobby.Get(m_user);

                bool everyoneReady = CloudManager.AllUsers(m_lobby).All(x => x.Ready.Value.HasValue && x.Ready.Value == true);

                if (everyoneReady)
                {
                    SceneManager.LoadScene("Voting");
                }
            }
        }
    }

    public async void ConfirmLeave()
    {
        User m_user = await User.Get();
        Lobby m_lobby = await Lobby.Get(m_user);
        CloudManager.LeaveLobby(m_user, m_lobby);
        SceneManager.LoadScene("Lobby");

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadScene("Lobby");
        //});
    }

    public async void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            User m_user = await User.Get();
            Lobby m_lobby = await Lobby.Get(m_user);
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
            CloudNode<bool> ready = m_lobby.Users.First(u => u.UserId.Value == m_user.Id).Ready;
            ready.Value = true;
            OnReadyChanged(ready);
        }
    }

    private async void OnSlotChanged(CloudNode entry)
    {
        string[] keys = entry.Key.ToString().Split('/');
        if (keys.Length >= 5)
        {
            string field = keys[4];

            if (entry.Value != null && field == "name")
            {
                string value = entry.Value;

                int slot;
                if (!string.IsNullOrEmpty(value) && int.TryParse(keys[3].Replace("slot-", ""), out slot))
                {
                    User m_user = await User.Get();
                    Lobby m_lobby = await Lobby.Get(m_user);
                    int player = CloudManager.GetPlayerNumber(m_user, m_lobby, keys[1]);
                    if (DatabaseButton != null && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(player.ToString(), slot.ToString(), value))))
                    {
                        foreach (Transform t in DatabaseButton.transform)
                        {
                            if (t.gameObject.name == "Alert")
                            {
                                t.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
