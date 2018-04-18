using System;
using UnityEngine;

public class ObjectInspecting : MonoBehaviour
{
    public float InspectDistance = 0.1f;
    public float InspectScale = 0.3f;

    const float m_turnSpeed = 500.0f;
    Vector3 m_touchOrigin;
    bool m_isTouching;
    Vector2 m_velocity = Vector2.zero;

    public Action OnInspectEnded;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !UI.IsPointerOverUIObject())
        {
            m_touchOrigin = Input.mousePosition;
            m_isTouching = true;
        }

        if (!Input.GetMouseButton(0))
        {
            m_isTouching = false;
            OnTouchEnded(Input.mousePosition);
        }

        if (m_isTouching)
        {
            Vector3 screenPos = Camera.main.ScreenToViewportPoint(Input.mousePosition - m_touchOrigin);
            m_velocity = screenPos * m_turnSpeed * Time.deltaTime * -1;
        }
        
        transform.RotateAround(transform.position, Vector3.up, m_velocity.x);
        transform.RotateAround(transform.position, Vector3.forward, -m_velocity.y);

        if (Mathf.Abs(m_velocity.magnitude) > 0.01)
        {
            m_velocity *= 0.9f;
        }
        else
        {
            m_velocity = Vector2.zero;
        }
    }

    private void OnTouchEnded(Vector3 touchEnd)
    {
        Vector2 touchDistance = touchEnd - m_touchOrigin;

        // If swipe has small distance it is probably a tap.
        if (touchDistance.magnitude < 20)
        {
            OnInspectEnded?.Invoke();
        }
    }
}
