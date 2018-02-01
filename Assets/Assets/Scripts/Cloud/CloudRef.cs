/*
using Firebase.Database;
using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a reference to a node in the cloud-hosted database.
/// </summary>
class CloudRef<T> where T : class, ICloudNode
{
    public delegate Task<T> FetchNodeFactory(string id);

    /// <summary>
    /// The name of the reference to a node in the database.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The identifier of the target node in the database.
    /// </summary>
    public string Target { get; private set; }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public T Value
    {
        get { return m_value; }
        set { m_reference.SetValueAsync(Target = (m_value = value)?.Id); }
    }

    /// <summary>
    /// Event handler for when the value changes in the database.
    /// </summary>
    public event Action<CloudRef<T>> ValueChanged;

    /// <summary>
    /// Creates and sets the value of a new reference to a node in the cloud-hosted database.
    /// </summary>
    public static CloudRef<T> Create(string path, FetchNodeFactory factory, T value = null) => new CloudRef<T>(path, factory) {
        Value = value
    };

    /// <summary>
    /// Fetches the value of an existing reference to a node in the cloud-hosted database.
    /// </summary>
    public static async Task<CloudRef<T>> Fetch(string path, FetchNodeFactory factory)
    {
        CloudRef<T> node = new CloudRef<T>(path, factory);
        node.m_value = await factory?.Invoke((string)(await node.m_reference.GetValueAsync()).Value);
        return node;
    }

    DatabaseReference m_reference;
    FetchNodeFactory m_factory;
    T m_value;

    CloudRef(string key, FetchNodeFactory factory)
    {
        Key = key;
        m_reference = Static.FirebaseDatabase.RootReference.Child(key);
        m_reference.ValueChanged += OnValueChanged;
        m_factory = factory;
    }

    ~CloudRef()
    {
        m_reference.ValueChanged -= OnValueChanged;
    }

    async void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        m_value = await m_factory?.Invoke((string)args.Snapshot.Value);
        ValueChanged?.Invoke(this);
    }
}
*/

interface ICloudNode
{
    string Id { get; }
}
