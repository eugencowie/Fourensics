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
    public Key Key { get; }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public string Value
    {
        get { return m_value; }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        set { SetValue(value); }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public async Task SetValue(string value)
    {
        m_value = value;
        m_busy = true;
        try { await m_reference.SetValueAsync(m_value); }
        finally { m_busy = false; }
    }

    /// <summary>
    /// Event handler for when the value changes in the database.
    /// </summary>
    public event Action<CloudNode> ValueChanged;

    /// <summary>
    /// Creates and sets the value of a new node in the cloud-hosted database.
    /// </summary>
    public static CloudNode Create(Key key, string value = null) => new CloudNode(key) {
        Value = value
    };

    /// <summary>
    /// Fetches the value of an existing node in the cloud-hosted database.
    /// </summary>
    public static async Task<CloudNode> Fetch(Key key)
    {
        CloudNode node = new CloudNode(key);
        node.m_value = (string)(await node.m_reference.GetValueAsync()).Value;
        return node;
    }

    DatabaseReference m_reference;
    string m_value;
    bool m_busy;

    CloudNode(Key key)
    {
        Key = key;
        m_reference = Cloud.Database.RootReference.Child(key.ToString());
        m_reference.ValueChanged += OnValueChanged;
    }

    ~CloudNode()
    {
        m_reference.ValueChanged -= OnValueChanged;
    }

    void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        string value = (string)args.Snapshot.Value;
        if (!m_busy && m_value != value)
        {
            m_value = value;
            ValueChanged?.Invoke(this);
        }
    }
}

/// <summary>
/// Represents a node in the cloud-hosted database.
/// </summary>
class CloudNode<T> where T : struct
{
    /// <summary>
    /// The name of the node in the database.
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public T? Value
    {
        get { return m_value; }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        set { SetValue(value); }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Gets or sets the value in the database.
    /// </summary>
    public async Task SetValue(T? value)
    {
        m_value = value;
        m_busy = true;
        try { await m_reference.SetValueAsync(m_value); }
        finally { m_busy = false; }
    }

    /// <summary>
    /// Event handler for when the value changes in the database.
    /// </summary>
    public event Action<CloudNode<T>> ValueChanged;

    /// <summary>
    /// Creates and sets the value of a new node in the cloud-hosted database.
    /// </summary>
    public static CloudNode<T> Create(Key key, T? value = null) => new CloudNode<T>(key) {
        Value = value
    };

    /// <summary>
    /// Fetches the value of an existing node in the cloud-hosted database.
    /// </summary>
    public static async Task<CloudNode<T>> Fetch(Key key)
    {
        CloudNode<T> node = new CloudNode<T>(key);
        node.m_value = (T?)(await node.m_reference.GetValueAsync()).Value;
        return node;
    }

    DatabaseReference m_reference;
    T? m_value;
    bool m_busy;

    CloudNode(Key key)
    {
        Key = key;
        m_reference = Cloud.Database.RootReference.Child(key.ToString());
        m_reference.ValueChanged += OnValueChanged;
    }

    ~CloudNode()
    {
        m_reference.ValueChanged -= OnValueChanged;
    }

    void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        T? value = (T?)args.Snapshot.Value;
        if (!m_busy && !m_value.Equals(value))
        {
            m_value = value;
            ValueChanged?.Invoke(this);
        }
    }
}
