using UnityEngine;

public class ObjectInspectable : MonoBehaviour
{
    public Vector3 InspectRotation = Vector3.zero;
    public float InspectScale = 3f;

    public AudioSource audioSource;
    public AudioClip audioClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
}
