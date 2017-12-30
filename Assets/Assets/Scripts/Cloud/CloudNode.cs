using Firebase.Database;
using System;
using System.Threading.Tasks;

class CloudNode
{
    public event Action<CloudNode> OnValueChanged;

    public string Path { get; }

    DatabaseReference m_reference;
    string m_value;

    public CloudNode(string path)
    {
        Path = path;

        m_reference = Static.FirebaseDatabase.RootReference.Child(path);
        m_value = null;

        m_reference.ValueChanged += ValueChanged;
    }

    ~CloudNode()
    {
        m_reference.ValueChanged -= ValueChanged;
    }

    void ValueChanged(object sender, ValueChangedEventArgs args)
    {
        m_value = (string)args.Snapshot.Value;
        OnValueChanged?.Invoke(this);
    }

    public bool Exists()
    {
        return (m_value != null);
    }

    public string Get()
    {
        return m_value;
    }

    public void Set(string value)
    {
        m_reference.SetValueAsync(m_value = value);
    }

    public static async Task<CloudNode> Fetch(string path)
    {
        CloudNode node = new CloudNode(path);
        node.m_value = (string)(await node.m_reference.GetValueAsync()).Value;
        return node;
    }
}
