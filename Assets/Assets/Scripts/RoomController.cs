using Firebase.Database;
using System.Collections;
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

    private void Start()
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

        NetworkController.GetPlayerLobby(room => {
            if (!string.IsNullOrEmpty(room)) {
                NetworkController.GetPlayers(room, players => {
                    m_roomCode = room;
                    foreach (var player in players) m_readyPlayers[player] = false;
                    NetworkController.RegisterReadyChanged(room, OnReadyChanged);
                    NetworkController.RegisterCluesChanged(room, OnSlotChanged);
                    ReadyButton.SetActive(true);
                    DatabaseButton.SetActive(true);
                });
            }
            else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });
    }
    
    public void DatabaseButtonPressed()
    {
        if (DatabaseButton.activeSelf)
        {
            DatabaseButton.SetActive(false);
            SceneManager.LoadScene("Communication Detective/Scenes/Database");
        }
    }
    
    private void OnReadyChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        if (ReadyButton == null)
            return;

        if (args.Snapshot.Exists)
        {
            string value = args.Snapshot.Value.ToString();

            if (value == "true")
            {
                string[] key = entry.Key.Split('/');
                string player = key[1];
                m_readyPlayers[player] = true;

                if (player == OnlineManager.GetPlayerId())
                {
                    ConfirmReady();
                }

                if (!m_readyPlayers.Any(p => p.Value == false))
                {
                    NetworkController.DeregisterReadyChanged(m_roomCode);
                    SceneManager.LoadScene("Communication Detective/Scenes/Voting");
                }
            }
        }
    }

    public void ConfirmLeave()
    {
        NetworkController.LeaveLobby(m_roomCode, _ => {
            SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });

        //NetworkController.LeaveLobby(m_roomCode, success => {
        //    if (success) SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        //});
    }
    
    public void ConfirmReady()
    {
        if (ReadyButton.activeSelf)
        {
            ReadyButton.SetActive(false);
            NetworkController.ReadyUp(success => {
                ReadyButton.SetActive(true);
                if (success)
                {
                    ReadyButton.GetComponent<Image>().color = Color.yellow;
                    foreach (Transform t in ReadyButton.gameObject.transform)
                    {
                        var text = t.GetComponent<Text>();
                        if (text != null) text.text = "Waiting...";
                    }
                }
            });
        }
    }

    private void OnSlotChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        string[] keys = entry.Key.Split('/');
        if (keys.Length >= 5)
        {
            string field = keys[4];

            if (args.Snapshot.Exists && field == "name")
            {
                string value = args.Snapshot.Value.ToString();

                int slot;
                if (!string.IsNullOrEmpty(value) && int.TryParse(keys[3].Replace("slot-", ""), out slot))
                {
                    NetworkController.GetPlayerNumber(m_roomCode, keys[1], player => {
                        if (DatabaseButton != null && !StaticClues.SeenSlots.Any(s => s.Equals(new SlotData(player.ToString(), slot.ToString(), value)))) {
                            foreach (Transform t in DatabaseButton.transform) {
                                if (t.gameObject.name == "Alert") {
                                    t.gameObject.SetActive(true);
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}
