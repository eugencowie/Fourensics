using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
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

public class RoomController : MonoBehaviour
{
    [SerializeField] private GameObject ReadyButton = null;
    [SerializeField] private GameObject DatabaseButton = null;

    private OnlineManager NetworkController;

    private string m_roomCode;

    private Dictionary<string, bool> m_readyPlayers = new Dictionary<string, bool>();

    public GameObject mainScreen;
    public GameObject welcomeScreen;

    async void Start()
    {
        Debug.Log("Seen welcome? " + StaticRoom.SeenWelcome);
        if (StaticRoom.SeenWelcome)
        {
            mainScreen.SetActive(true);
            welcomeScreen.SetActive(false);
        }
        StaticRoom.SeenWelcome = true;

        NetworkController = new OnlineManager();

        ReadyButton.SetActive(false);
        DatabaseButton.SetActive(false);

        string room = await NetworkController.GetPlayerLobby();
        if (!string.IsNullOrEmpty(room))
        {
            string[] players = NetworkController.GetPlayers();
            m_roomCode = room;
            foreach (var player in players) m_readyPlayers[player] = false;
            NetworkController.RegisterReadyChanged(OnReadyChanged);
            NetworkController.RegisterCluesChanged(OnSlotChanged);
            ReadyButton.SetActive(true);
            DatabaseButton.SetActive(true);
        }
        else SceneManager.LoadScene("Lobby");
    }

    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            DatabaseButton.SetActive(false);
            SceneManager.LoadScene("Database");
        }
    }

    private void OnReadyChanged(CloudNode entry)
    {
        if (ReadyButton == null)
            return;

        if (entry.Exists())
        {
            string value = entry.Get();

            if (value == "true")
            {
                string[] key = entry.Path.Split('/');
                string player = key[1];
                m_readyPlayers[player] = true;

                if (player == SignIn.User.Id)
                {
                    ConfirmReady();
                }

                if (!m_readyPlayers.Any(p => p.Value == false))
                {
                    //NetworkController.DeregisterReadyChanged(OnReadyChanged);
                    SceneManager.LoadScene("Voting");
                }
            }
        }
    }

    public void ConfirmLeave()
    {
        NetworkController.LeaveLobby();
        SceneManager.LoadScene("Lobby");

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadScene("Lobby");
        //});
    }

    public void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            NetworkController.ReadyUp();
            ReadyButton.SetActive(true);
            ReadyButton.GetComponent<Image>().color = Color.yellow;
            foreach (Transform t in ReadyButton.gameObject.transform)
            {
                var text = t.GetComponent<Text>();
                if (text != null) text.text = "Waiting...";
            }
        }
    }

    private void OnSlotChanged(CloudNode entry)
    {
        string[] keys = entry.Path.Split('/');
        if (keys.Length >= 5)
        {
            string field = keys[4];

            if (entry.Exists() && field == "name")
            {
                string value = entry.Get();

                int slot;
                if (!string.IsNullOrEmpty(value) && int.TryParse(keys[3].Replace("slot-", ""), out slot))
                {
                    int player = NetworkController.GetPlayerNumber(keys[1]);
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
