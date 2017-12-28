using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMovement : MonoBehaviour
{
    public GameObject Target;
    public float Duration = 1f;
    public Action OnMoveEnded;

    private Vector3 m_startPosition, m_endPosition;
    private Quaternion m_startRotation, m_endRotation;
    private float m_elapsedTime;

    public void Reset()
    {
        m_startPosition = transform.position;
        m_endPosition = Target.transform.position;

        m_startRotation = transform.rotation;
        m_endRotation = Target.transform.rotation;

        m_elapsedTime = 0;
    }

    private void Start()
    {
        Reset();
    }

    private void Update()
    {
        m_elapsedTime += Time.deltaTime;

        if (m_elapsedTime <= Duration)
        {
            float t = m_elapsedTime / Duration;
            transform.position = Vector3.Lerp(m_startPosition, m_endPosition, t);
            transform.rotation = Quaternion.Lerp(m_startRotation, m_endRotation, t);
        }
        else
        {
            if (OnMoveEnded != null) OnMoveEnded();
        }
    }
}
