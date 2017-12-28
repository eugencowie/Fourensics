using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectInspecting : MonoBehaviour
{
    public float InspectDistance = 0.1f;
    public float InspectScale = 0.3f;

    private Vector2 m_touchStartPos;
    private Vector2 m_touchEndPos;

    public const float turnSpeed = 120.0f;

    private Vector3 mouseOrigin;
    private bool isRotating;

    public Action OnInspectEnded;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            mouseOrigin = Input.mousePosition;
            isRotating = true;
        }

        if (!Input.GetMouseButton(0)) isRotating = false;

        if (isRotating)
        {
            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);
            Vector3 movement = pos.normalized * turnSpeed * -1;
            transform.RotateAround(transform.position, Vector3.up, movement.x * Time.deltaTime);
            transform.RotateAround(transform.position, Vector3.forward, movement.y * Time.deltaTime);
        }
        
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            m_touchStartPos = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0))
        {
            m_touchEndPos = Input.mousePosition;
            OnTouchEnded();
        }
    }

    private void OnTouchEnded()
    {
        Vector2 touchDistance = m_touchEndPos - m_touchStartPos;

        // If swipe has small distance it is probably a tap.
        if (touchDistance.magnitude < 20)
        {
            if (OnInspectEnded != null) OnInspectEnded();
        }
    }
}
