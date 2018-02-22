using System;
using System.Linq;
using System.Threading.Tasks;

enum LobbyState { Lobby, InGame, Voting, Finished }

class Lobby : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode<long> State { get; private set; }
    public LobbyUser[] Users { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        State = CloudNode<long>.Create(Key.Child("state"));
        Users = "0123".Select(n => Cloud.Create<LobbyUser>(Key.Child("users").Child(n.ToString()))).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        State = await CloudNode<long>.Fetch(Key.Child("state"));
        Users = await Task.WhenAll("0123".Select(n => Cloud.Fetch<LobbyUser>(Key.Child("users").Child(n.ToString()))));
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
    public CloudNode<long> Scene { get; private set; }
    public CloudNode<bool> Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public LobbyUserItem[] Items { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        UserId = CloudNode.Create(Key.Child("user-id"));
        Scene = CloudNode<long>.Create(Key.Child("scene"));
        Ready = CloudNode<bool>.Create(Key.Child("ready"));
        Vote = CloudNode.Create(Key.Child("vote"));
        Items = "012345".Select(n => Cloud.Create<LobbyUserItem>(Key.Child("items").Child(n.ToString()))).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        UserId = await CloudNode.Fetch(Key.Child("user-id"));
        Scene = await CloudNode<long>.Fetch(Key.Child("scene"));
        Ready = await CloudNode<bool>.Fetch(Key.Child("ready"));
        Vote = await CloudNode.Fetch(Key.Child("vote"));
        Items = await Task.WhenAll("012345".Select(n => Cloud.Fetch<LobbyUserItem>(Key.Child("items").Child(n.ToString()))));
    }
}

class LobbyUserItem : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Description { get; private set; }
    public CloudNode Image { get; private set; }

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
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch(Key.Child("name"));
        Description = await CloudNode.Fetch(Key.Child("description"));
        Image = await CloudNode.Fetch(Key.Child("image"));
    }
}
