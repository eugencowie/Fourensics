using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(VotingSuspect))]
public class VotingSuspectEditor : Editor
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

        VotingSuspect hint = (VotingSuspect)target;
        ImagePath.stringValue = AssetDatabase.GetAssetPath(hint.Image).Replace("Assets/Resources/", "").Replace(".png", "");
        serializedObject.ApplyModifiedProperties();
    }
}

#endif

public class VotingSuspect : MonoBehaviour
{
    public GameObject Slot;

    public Text Name = null;
    public Text Description = null;
    public Image Image = null;
    public string ImagePath = "";
}

public class VotingSuspectData
{
    public string Name = "";
    public string Description = "";
    public string Image = "";

    public VotingSuspectData(string name, string desc, string image)
    {
        Name = name;
        Description = desc;
        Image = image;
    }

    public VotingSuspectData(VotingSuspect suspect)
        : this(suspect.Name.text, suspect.Description.text, suspect.ImagePath)
    {
    }
}
