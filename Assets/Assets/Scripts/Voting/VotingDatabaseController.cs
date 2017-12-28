using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VotingDatabaseController : MonoBehaviour
{
    public GameObject MainScreen, WaitScreen;

    //[SerializeField] private GameObject VotingButton = null;
    [SerializeField] private GameObject ButtonTemplate = null;
    [SerializeField] private GameObject[] Backgrounds = new GameObject[4];
    [SerializeField] private Data[] Data = new Data[4];

    private OnlineManager NetworkController;
    private string m_lobby;
    private int m_scene;

    int playerItemsLoaded = 0;

    private void Start()
    {
        NetworkController = new OnlineManager();
        
        MainScreen.SetActive(false);
        WaitScreen.SetActive(true);

        NetworkController.GetPlayerScene(scene => {
            if (scene > 0) {
                m_scene = scene;
                SetBackground();
                NetworkController.GetPlayerLobby(lobby => {
                    if (!string.IsNullOrEmpty(lobby)) {
                        NetworkController.GetPlayers(lobby, players => {
                            m_lobby = lobby;
                            DownloadItems();
                            NetworkController.RegisterCluesChanged(m_lobby, OnSlotChanged);
                        });
                    }
                    else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
                });
            }
            else SceneManager.LoadScene("Communication Detective/Scenes/Lobby");
        });

        for (int i = 0; i < Data.Length; i++)
        {
            Data data = Data[i];
            data.PlayerButton.GetComponent<Button>().onClick.AddListener(() => PlayerButtonPressed(data));
        }

        PlayerButtonPressed(Data[0]);
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
        SceneManager.LoadScene("Communication Detective/Scenes/Voting");
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

        if (playerItemsLoaded >= 24)
        {
            WaitScreen.SetActive(false);
            MainScreen.SetActive(true);
        }
    }
    
    private void DownloadItems()
    {
        int tmp = 0;
        NetworkController.DownloadClues(m_lobby, tmp, player => {
            for (int j = 0; j < player.Clues.Clues.Length; j++) {
                int tmp2 = j;
                var clue = player.Clues.Clues[tmp2];
                clue.PullEntries(_ => {
                    CheckPlayerItemsLoaded();
                    if (!string.IsNullOrEmpty(clue.Name.Value)) {
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
                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = clue.Hint.Value;
                    }
                });
            }
        });
    }
    
    private void OnSlotChanged(OnlineDatabaseEntry entry, ValueChangedEventArgs args)
    {
        string[] key = entry.Key.Split('/');
        if (key.Length >= 5)
        {
            string player = key[1];
            string field = key[4];

            if (args.Snapshot.Exists)
            {
                string value = args.Snapshot.Value.ToString();
                Debug.Log(entry.Key + " = " + value);

                int slotNb = -1;
                if (int.TryParse(key[3].Replace("slot-", ""), out slotNb))
                {
                    NetworkController.GetPlayerNumber(m_lobby, player, playerNb =>
                    {
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
                    });
                }
            }
            else
            {
                Debug.Log(entry.Key + " removed");

                int slotNb = -1;
                if (int.TryParse(key[3].Replace("slot-", ""), out slotNb))
                {
                    NetworkController.GetPlayerNumber(m_lobby, player, playerNb =>
                    {
                        var slot = Data[playerNb].Slots[slotNb - 1];

                        slot.GetComponent<Slot>().Text.GetComponent<Text>().text = "";

                        foreach (Transform t1 in slot.transform)
                        {
                            Destroy(t1.gameObject);
                        }
                    });
                }
            }
        }
    }
}
