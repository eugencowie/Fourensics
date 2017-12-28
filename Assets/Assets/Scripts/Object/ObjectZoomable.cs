using UnityEngine;

public class ObjectZoomable : MonoBehaviour
{
    public GameObject TargetCamera;
    public float Duration = 1f;
 
    public AudioSource audioSource;
    public AudioClip audioClip;
 
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
}
