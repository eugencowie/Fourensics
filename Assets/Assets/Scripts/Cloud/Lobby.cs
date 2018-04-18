using System;
using System.Linq;
using System.Threading.Tasks;

enum LobbyState { Lobby, InGame, Voting, Finished, RetryLobby }

class Lobby : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode CreatedAt { get; private set; }
    public CloudNode<long> State { get; private set; }
    public CloudNode<long> Case { get; private set; }
    public LobbyUser[] Users { get; private set; }
    public CloudNode Retry { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        CreatedAt = CloudNode.Create(Key.Child("created-at"));
        State = CloudNode<long>.Create(Key.Child("state"));
        Case = CloudNode<long>.Create(Key.Child("case"));
        Retry = CloudNode.Create(Key.Child("retry"));
        Users = "0123".Select(n => Cloud.Create<LobbyUser>(Key.Child("users").Child(n.ToString()))).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        CreatedAt = await CloudNode.Fetch(Key.Child("created-at"));
        State = await CloudNode<long>.Fetch(Key.Child("state"));
        Case = await CloudNode<long>.Fetch(Key.Child("case"));
        Retry = await CloudNode.Fetch(Key.Child("retry"));
        Users = await Task.WhenAll("0123".Select(n => Cloud.Fetch<LobbyUser>(Key.Child("users").Child(n.ToString()))));
    }

    public void Reset()
    {
        CreatedAt = null;
        State.Value = null;
        Case.Value = null;
        Retry.Value = null;

        foreach (LobbyUser userInfo in Users)
        {
            if (userInfo != null)
                userInfo.Reset();
        }
    }

    static Lobby m_instance = null;

    public static async Task<Lobby> Get(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Lobby.Value))
            m_instance = null;

        else if (m_instance == null || m_instance.Id != user.Lobby.Value)
            m_instance = await Cloud.Fetch<Lobby>(new Key("lobbies").Child(user.Lobby.Value));

        return m_instance;
    }

    public static Lobby Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return m_instance = Cloud.Create<Lobby>(new Key("lobbies").Child(id));
    }
}

class LobbyUser : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode UserId { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode<long> Scene { get; private set; }
    public LobbyUserItem[] Items { get; private set; }
    public CloudNode<bool> Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public CloudNode<bool> Retry { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        UserId = CloudNode.Create(Key.Child("user-id"));
        Name = CloudNode.Create(Key.Child("name"));
        Scene = CloudNode<long>.Create(Key.Child("scene"));
        Items = "012345".Select(n => Cloud.Create<LobbyUserItem>(Key.Child("items").Child(n.ToString()))).ToArray();
        Ready = CloudNode<bool>.Create(Key.Child("ready"));
        Vote = CloudNode.Create(Key.Child("vote"));
        Retry = CloudNode<bool>.Create(Key.Child("retry"));
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        UserId = await CloudNode.Fetch(Key.Child("user-id"));
        Name = await CloudNode.Fetch(Key.Child("name"));
        Scene = await CloudNode<long>.Fetch(Key.Child("scene"));
        Items = await Task.WhenAll("012345".Select(n => Cloud.Fetch<LobbyUserItem>(Key.Child("items").Child(n.ToString()))));
        Ready = await CloudNode<bool>.Fetch(Key.Child("ready"));
        Vote = await CloudNode.Fetch(Key.Child("vote"));
        Retry = await CloudNode<bool>.Fetch(Key.Child("retry"));
    }

    public void Reset()
    {
        UserId.Value = null;
        Name.Value = null;
        Scene.Value = null;
        foreach (var x in Items) x.Reset();
        Ready.Value = null;
        Vote.Value = null;
        Retry.Value = null;
    }
}

class LobbyUserItem : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Description { get; private set; }
    public CloudNode Image { get; private set; }
    public CloudNode<bool> Highlight { get; private set; }

    public event Action<CloudNode> ValueChanged
    {
        add
        {
            Name.ValueChanged += value;
            Description.ValueChanged += value;
            Image.ValueChanged += value;
        }
        remove
        {
            Name.ValueChanged -= value;
            Description.ValueChanged -= value;
            Image.ValueChanged -= value;
        }
    }

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Name = CloudNode.Create(Key.Child("name"));
        Description = CloudNode.Create(Key.Child("description"));
        Image = CloudNode.Create(Key.Child("image"));
        Highlight = CloudNode<bool>.Create(Key.Child("highlight"));
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch(Key.Child("name"));
        Description = await CloudNode.Fetch(Key.Child("description"));
        Image = await CloudNode.Fetch(Key.Child("image"));
        Highlight = await CloudNode<bool>.Fetch(Key.Child("highlight"));
    }

    public void Reset()
    {
        Name.Value = null;
        Description.Value = null;
        Image.Value = null;
        Highlight.Value = null;
    }
}
