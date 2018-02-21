using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

static class CloudManager
{
    /// <summary>
    /// If lobby exists, updates player entry to new lobby and adds player to lobby.
    /// </summary>
    public static bool JoinLobby(User user, Lobby lobby, int maxPlayers)
    {
        // Check if lobby exists
        if (lobby == null || lobby.State.Value == null)
            return false;

        // Get list of players in lobby
        List<string> players = AllUsers(lobby).ToList();

        // If player is already in room
        if (players.Contains(user.Id))
            return true;

        // If too many players
        if (players.Count >= maxPlayers)
            return false;

        // Add player to lobby
        players.Add(user.Id);
        for (int i = 0; i < players.Count; i++)
            lobby.Users[i].UserId.Value = players[i];

        return true;
    }

    private static async Task<bool> Exists(string path)
    {
        DataSnapshot data;
        try { data = await Cloud.Database.RootReference.Child(path).GetValueAsync(); }
        catch { return false; }
        return data.Exists;
    }

    /// <summary>
    /// Attempts to generate a lobby code which is not in use. If all codes generated are in
    /// use, returns null.
    /// </summary>
    public static async Task<string> CreateLobbyCode()
    {
        for (int i = 0; i < 3; i++)
        {
            string code = GenerateRandomCode();
            string key = $"lobbies/{code}/state";

            if (!await Exists(key))
            {
                return code;
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes the player entry and removes the player from the lobby. If no players are left in the
    /// lobby, deletes the lobby.
    /// </summary>
    public static void LeaveLobby(User user, Lobby lobby)
    {
        user.Lobby.Value = null;

        if (lobby.Users[0].UserId.Value == user.Id)
        {
            DeleteLobby(lobby);
        }
        else
        {
            LobbyUser userInfo = lobby.Users.FirstOrDefault(u => u.UserId.Value == user.Id);
            if (userInfo != null)
            {
                userInfo.UserId.Value = null;
                userInfo.Scene.Value = null;
                userInfo.Ready.Value = null;
                userInfo.Vote.Value = null;

                foreach (var item in userInfo.Items)
                {
                    item.Name.Value = null;
                    item.Description.Value = null;
                    item.Image.Value = null;
                }
            }
        }
    }

    static void DeleteLobby(Lobby lobby)
    {
        lobby.State.Value = null;

        foreach (LobbyUser userInfo in lobby.Users)
        {
            userInfo.UserId.Value = null;
            userInfo.Scene.Value = null;
            userInfo.Ready.Value = null;
            userInfo.Vote.Value = null;

            foreach (var item in userInfo.Items)
            {
                item.Name.Value = null;
                item.Description.Value = null;
                item.Image.Value = null;
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public static int AssignPlayerScenes(User user, Lobby lobby)
    {
        List<string> players = AllUsers(lobby).ToList();
        players = players.OrderBy(_ => UnityEngine.Random.value).ToList();
        int ourScene = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == user.Id)
            {
                ourScene = (i + 1);
            }
            lobby.Users.First(u => u.UserId.Value == players[i]).Scene.Value = i + 1;
        }
        return ourScene;
    }

    public static void UploadDatabaseItem(User user, Lobby lobby, int slot, ObjectHintData hint)
    {
        lobby.Users.First(u => u.UserId.Value == user.Id).Items[slot - 1].Name.Value = hint.Name;
        lobby.Users.First(u => u.UserId.Value == user.Id).Items[slot - 1].Description.Value = hint.Hint;
        lobby.Users.First(u => u.UserId.Value == user.Id).Items[slot - 1].Image.Value = hint.Image;
    }

    public static void RemoveDatabaseItem(User user, Lobby lobby, int slot)
    {
        UploadDatabaseItem(user, lobby, slot, new ObjectHintData("", "", ""));
    }

    public static int GetPlayerNumber(User user, Lobby lobby, string player)
    {
        List<string> players = OtherUsers(lobby, user).ToList();
        players.Insert(0, user.Id);
        int playerNb = players.IndexOf(player);
        if (playerNb >= 0 && playerNb < players.Count)
        {
            return playerNb;
        }
        else return -1;
    }

    public static async Task<User> DownloadClues(User user, Lobby lobby, int playerNb)
    {
        List<string> players = OtherUsers(lobby, user).ToList();
        players.Insert(0, user.Id);
        if (playerNb < players.Count)
        {
            return await Cloud.Fetch<User>("users", players[playerNb]);
        }
        else return null;
    }

    public static IEnumerable<string> AllUsers(Lobby lobby) => lobby.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.UserId.Value))
            .Select(user => user.UserId.Value);

    public static IEnumerable<string> OtherUsers(Lobby lobby, User excludeUser) => AllUsers(lobby)
            .Where(user => user != excludeUser.Id);
    
    /// <summary>
    /// Generates a random five-character room code.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string roomCode = "";

        for (int i = 0; i < 5; i++)
        {
            // Gets a random valid character and adds it to the room code string
            int randomIndex = UnityEngine.Random.Range(0, validChars.Length - 1);
            char randomChar = validChars[randomIndex];
            roomCode += randomChar;
        }

        return roomCode;
    }
}
