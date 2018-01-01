using Firebase.Database;
using System;
using System.Threading.Tasks;

/// <summary>
/// Represents a node in the cloud-hosted database.
/// </summary>
class CloudNode
{
    /// <summary>
    /// The name of the node in the database.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public string Value
    {
        get { return m_value; }
        set { m_reference.SetValueAsync(m_value = value); }
    }

    /// <summary>
    /// Event handler for when the value changes in the database.
    /// </summary>
    public event Action<CloudNode> ValueChanged;

    /// <summary>
    /// Creates and sets the value of a new node in the cloud-hosted database.
    /// </summary>
    public static CloudNode Create(string path, string value = null) => new CloudNode(path) {
        Value = value
    };

    /// <summary>
    /// Fetches the value of an existing node in the cloud-hosted database.
    /// </summary>
    public static async Task<CloudNode> Fetch(string path)
    {
        CloudNode node = new CloudNode(path);
        node.m_value = (string)(await node.m_reference.GetValueAsync()).Value;
        return node;
    }

    DatabaseReference m_reference;
    string m_value;

    CloudNode(string key)
    {
        Key = key;
        m_reference = Static.FirebaseDatabase.RootReference.Child(key);
        m_reference.ValueChanged += OnValueChanged;
    }

    ~CloudNode()
    {
        m_reference.ValueChanged -= OnValueChanged;
    }

    void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        m_value = (string)args.Snapshot.Value;
        ValueChanged?.Invoke(this);
    }
}
