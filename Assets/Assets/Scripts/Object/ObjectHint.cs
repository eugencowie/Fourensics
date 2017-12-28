using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(ObjectHint))]
public class ObjectHintEditor : Editor
{
    SerializedProperty Image;
    SerializedProperty ImagePath;

    private void OnEnable()
    {
        Image = serializedObject.FindProperty("Image");
        ImagePath = serializedObject.FindProperty("ImagePath");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ObjectHint hint = (ObjectHint)target;
        ImagePath.stringValue = AssetDatabase.GetAssetPath(hint.Image).Replace("Assets/Resources/", "").Replace(".png", "");
        serializedObject.ApplyModifiedProperties();
    }
}

#endif

public class ObjectHint : MonoBehaviour
{
    public string Name = "";
    public string Hint = "";
    public Sprite Image = null;
    public string ImagePath = "";
}

[System.Serializable]
public class ObjectHintData
{
    //Saving
    
    public static ObjectHintData current;

    public string Name = "";
    public string Hint = "";
    public string Image = "";

    public ObjectHintData(string name, string hint, string image)
    {
        Name = name;
        Hint = hint;
        Image = image;
    }

    public ObjectHintData(ObjectHint hint)
        : this(hint.Name, hint.Hint, hint.ImagePath)
    {
    }
}
