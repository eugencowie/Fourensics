using UnityEngine;

public class ObjectInspectable : MonoBehaviour
{
    public float InspectScale = 3f;

    public AudioSource audioSource;
    public AudioClip audioClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
}
