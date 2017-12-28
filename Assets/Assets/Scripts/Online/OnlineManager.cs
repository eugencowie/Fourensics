using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LobbyState { Lobby, InGame, Voting, Finished }

public enum LobbyError { None, Unknown, TooFewPlayers, TooManyPlayers }

public class OnlineManager
{
    private OnlineDatabase m_database;
    private Player m_player;
    private Lobby m_lobby;

    public OnlineManager()
    {
        m_database = new OnlineDatabase();
        m_player = new Player(m_database, GetPlayerId());
        m_lobby = null;
    }

    #region Async methods

    /// <summary>
    /// If the player is listed as being in a lobby and that lobby does exist, returns the lobby
    /// code. Otherwise, returns null.
    /// </summary>
    public void GetPlayerLobby(Action<string> returnLobby)
    {
        OnlineDatabase.ValidateAction(ref returnLobby, "GetPlayerLobby");

        // If 'players/{0}/lobby' exists and 'lobbies/{1}' exists, return lobby code.
        m_player.Lobby.Pull(success => {
            if (success) {
                m_lobby = new Lobby(m_database, m_player.Lobby.Value);
                m_lobby.Exists(exists => {
                    if (exists) returnLobby(m_lobby.Id);
                    else m_player.Delete(_ => returnLobby(null));
                });
            }
            else returnLobby(null);
        });
    }

    /// <summary>
    /// If the player is listed as being in a lobby and that lobby does exist, returns the lobby
    /// code. Otherwise, returns null.
    /// </summary>
    public void GetPlayerScene(Action<int> returnScene)
    {
        OnlineDatabase.ValidateAction(ref returnScene, "GetPlayerScene");

        // If 'players/{0}/scene' exists, return it.
        m_player.Scene.Pull(success => {
            if (success) {
                int scene;
                if (int.TryParse(m_player.Scene.Value, out scene)) returnScene(scene);
                else returnScene(0);
            }
            else returnScene(0);
        });
    }

    /// <summary>
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public void JoinLobby(string code, int maxPlayers, Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess, string.Format("JoinLobby({0})", code));

        // If 'lobbies/{0}' exists, push 'players/{1}/lobby', get 'lobbies/{0}/players', add
        // player to list and push 'lobbies/{0}/players' back up.
        m_lobby = new Lobby(m_database, code);
        m_lobby.Exists(exists => {
            if (exists) {
                m_lobby.Players.Pull(lobbySuccess => {
                    if (lobbySuccess) {
                        List<string> players = m_lobby.Players.Value.Split(',').ToList();
                        if (players.Count < maxPlayers && !players.Contains(m_player.Id)) {
                            m_player.Lobby.Value = code;
                            m_player.Lobby.Push(playerSuccess => {
                                if (playerSuccess) {
                                    players.Add(m_player.Id);
                                    players.RemoveAll(s => string.IsNullOrEmpty(s));
                                    m_lobby.Players.Value = string.Join(",", players.ToArray());
                                    m_lobby.Players.Push(roomPlayersSuccess => {
                                        if (roomPlayersSuccess) {
                                            m_player.Clues.PushEntries(returnSuccess);
                                        }
                                        else returnSuccess(false);
                                    });
                                }
                                else returnSuccess(false);
                            });
                        }
                        else returnSuccess(false);
                    }
                    else returnSuccess(false);
                });
            }
            else returnSuccess(false);
        });
    }

    /// <summary>
    /// Creates a lobby on the server.
    /// </summary>
    public void CreateLobby(string code, Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess, string.Format("CreateLobby({0})", code));

        m_lobby = new Lobby(m_database, code);

        m_lobby.CreatedTime.Value = DateTimeOffset.UtcNow.ToString("o");
        m_lobby.State.Value = ((int)LobbyState.Lobby).ToString();

        m_lobby.CreatedTime.Push(success1 => {
            m_lobby.Players.Push(success2 => {
                m_lobby.State.Push(success3 => {
                    returnSuccess(success1 && success2 && success3);
                });
            });
        });
    }

    /// <summary>
    /// Attempts to generate a lobby code which is not in use. If all codes generated are in
    /// use, returns null.
    /// </summary>
    public void CreateLobbyCode(Action<string> returnCode)
    {
        OnlineDatabase.ValidateAction(ref returnCode, "CreateLobbyCode");

        // Three attempts to find a unique room code.
        string[] codes = { GenerateRandomCode(), GenerateRandomCode(), GenerateRandomCode() };
        List<string> keys = codes.Select(c => new Lobby(m_database, c).Key).ToList();

        // If the generated room code already exists, try again (up to three tries).
        m_database.Exists(keys[0], exists0 => {
            if (!exists0) returnCode(codes[0]);
            else m_database.Exists(keys[1], exists1 => {
                if (!exists1) returnCode(codes[1]);
                else m_database.Exists(keys[2], exists2 => {
                    if (!exists2) returnCode(codes[2]);
                    else returnCode(null);
                });
            });
        });
    }

    /// <summary>
    /// Deletes the player entry and removes the player from the lobby. If no players are left in the
    /// lobby, deletes the lobby.
    /// </summary>
    public void LeaveLobby(string code, Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess, string.Format("LeaveLobby({0})", code));

        // Delete 'players/{0}', pull 'lobbies/{1}/players', remove the player from list and push
        // 'lobbies/{1}/players' back up (unless there are no players left, then delete the lobby).
        /*m_player.Delete(success1 => {
            if (success1) {
                //m_lobby = new Lobby(m_database, code); // TODO
                m_lobby.Players.Pull(success2 => {
                    if (success2) {
                        //List<string> layers = m_lobby.Players.Value.Split(',').ToList();
                        //layers.Remove(m_player.Id);
                        //layers.RemoveAll(s => string.IsNullOrEmpty(s));
                        //if (layers.Count > 0) {
                        //    m_lobby.Players.Value = string.Join(",", layers.ToArray());
                        //    m_lobby.Players.Push(returnSuccess);
                        //} else {
                            m_lobby.Delete(returnSuccess);
                        //}
                    }
                    else returnSuccess(false);
                });
            }
            else returnSuccess(false);
        });*/

        m_player.Delete(returnSuccess);
    }

    /// <summary>
    /// Checks if the lobby has the required number of players.
    /// </summary>
    public void CanStartGame(string code, int requiredPlayers, Action<LobbyError> returnError)
    {
        // TODO: remove this in final build
        returnError(LobbyError.None);
        return;

        OnlineDatabase.ValidateAction(ref returnError, string.Format("CanStartGame({0}, {1})", code, requiredPlayers));

        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                if (players.Count < requiredPlayers) returnError(LobbyError.TooFewPlayers);
                else if (players.Count > requiredPlayers) returnError(LobbyError.TooManyPlayers);
                else returnError(LobbyError.None);
            }
            else returnError(LobbyError.Unknown);
        });
    }

    /// <summary>
    /// Pushes a new lobby state to the server.
    /// </summary>
    public void SetLobbyState(string code, LobbyState state, Action<bool> returnSuccess=null)
    {
        OnlineDatabase.ValidateAction(ref returnSuccess, string.Format("SetLobbyState({0}, {1})", code, state));

        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.State.Value = ((int)state).ToString();
        m_lobby.State.Push(returnSuccess);
    }

    /// <summary>
    ///
    /// </summary>
    public void AssignPlayerScenes(string code, Action<int> returnScene)
    {
        OnlineDatabase.ValidateAction(ref returnScene, string.Format("AssignPlayerScenes({0})", code));

        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
                int ourScene = -1;
                for (int i = 0; i < players.Count; i++) {
                    Player player = new Player(m_database, players[i]);
                    if (player.Id == m_player.Id) {
                        ourScene = (i+1);
                    }
                    player.Scene.Value = (i+1).ToString();
                    player.Scene.Push();
                }
                returnScene(ourScene);
            }
            else returnScene(-1);
        });
    }

    #endregion

    #region Async database methods

    public void UploadDatabaseItem(int slot, ObjectHintData hint)
    {
        m_player.Clues.Clues[slot-1].Name.Value = hint.Name;
        m_player.Clues.Clues[slot-1].Hint.Value = hint.Hint;
        m_player.Clues.Clues[slot-1].Image.Value = hint.Image;
        m_player.Clues.Clues[slot-1].PushEntries();
    }

    public void RemoveDatabaseItem(int slot)
    {
        m_player.Clues.Clues[slot - 1].Delete();
    }

    public void RegisterCluesChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        Lobby lobby = new Lobby(m_database, code); // TODO
        lobby.Players.Pull(success => {
            if (success) {
                string val = lobby.Players.Value;
                List<string> players = lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    foreach (PlayerClue clue in player.Clues.Clues) {
                        clue.RegisterListeners(listener);
                    }
                }
            }
        });
    }

    public void DeregisterCluesChanged() // TODO: make all these functions static
    {
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    foreach (PlayerClue clue in player.Clues.Clues) {
                        clue.DeregisterListeners();
                    }
                }
            }
        });
    }

    public void GetPlayerNumber(string code, string player, Action<int> returnPlayerNumber)
    {
        OnlineDatabase.ValidateAction(ref returnPlayerNumber);

        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                players.Insert(0, m_player.Id);
                int playerNb = players.IndexOf(player);
                if (playerNb >= 0 && playerNb < players.Count) {
                    returnPlayerNumber(playerNb);
                }
                else returnPlayerNumber(-1);
            }
            else returnPlayerNumber(-1);
        });
    }

    public void DownloadClues(string code, int playerNb, Action<Player> returnPlayer)
    {
        OnlineDatabase.ValidateAction(ref returnPlayer);

        //m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                players.Remove(m_player.Id);
                players.Insert(0, m_player.Id);
                if (playerNb < players.Count) {
                    Player player = new Player(m_database, players[playerNb]);
                    player.Clues.PullEntries(pullSuccess => {
                        if (success) returnPlayer(player);
                        else returnPlayer(null);
                    });
                }
                else returnPlayer(null);
            }
            else returnPlayer(null);
        });
    }

    #endregion

    #region Async voting methods

    public void GetPlayers(string code, Action<string[]> returnPlayers)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success)
            {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                returnPlayers(players.ToArray());
            }
            else returnPlayers(null);
        });
    }

    public void ReadyUp(Action<bool> returnSuccess=null)
    {
        m_player.Ready.Value = "true";
        m_player.Ready.Push(returnSuccess);
    }

    public void SubmitVote(string suspect, Action<bool> returnSuccess=null)
    {
        m_player.Vote.Value = suspect;
        m_player.Vote.Push(returnSuccess);
    }

    public void RegisterReadyChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Ready.RegisterListener(listener);
                }
            }
        });
    }

    public void DeregisterReadyChanged(string code) // TODO: make all these functions static
    {
        m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Ready.DeregisterListener();
                }
            }
        });
    }

    public void RegisterVoteChanged(string code, OnlineDatabaseEntry.Listener listener)
    {
        m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Vote.RegisterListener(listener);
                }
            }
        });
    }

    public void DeregisterVoteChanged(string code) // TODO: make all these functions static
    {
        m_lobby = new Lobby(m_database, code); // TODO
        m_lobby.Players.Pull(success => {
            if (success) {
                List<string> players = m_lobby.Players.Value.Split(',').ToList();
                players.RemoveAll(s => string.IsNullOrEmpty(s));
                //players.Remove(m_player.Id);
                foreach (string playerId in players) {
                    Player player = new Player(m_database, playerId);
                    player.Vote.DeregisterListener();
                }
            }
        });
    }

    #endregion

    #region Listeners

    public void RegisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.RegisterListener(path, listener);
    }

    public void DeregisterListener(string path, EventHandler<ValueChangedEventArgs> listener)
    {
        m_database.DeregisterListener(path, listener);
    }

    #endregion

    #region Utility methods

    /// <summary>
    /// Returns a unique player id.
    /// </summary>
    public static string GetPlayerId()
    {
        const string key = "UniquePlayerId";

        if (PlayerPrefs.HasKey(key))
        {
            string value = PlayerPrefs.GetString(key);

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        string id = SystemInfo.deviceUniqueIdentifier;
        PlayerPrefs.SetString(key, id);
        return id;
    }

    /// <summary>
    /// Generates a random five-character room code.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string roomCode = "";

        for (int i = 0; i < 5; i++)
        {
            // Gets a random valid character and adds it to the room code string.
            int randomIndex = UnityEngine.Random.Range(0, validChars.Length - 1);
            char randomChar = validChars[randomIndex];
            roomCode += randomChar;
        }

        return roomCode;
    }

    #endregion
}
