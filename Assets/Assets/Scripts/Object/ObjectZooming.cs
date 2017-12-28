using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectZooming : MonoBehaviour
{
    public Action OnZoomEnded;

    private Vector2 m_touchStartPos;
    private Vector2 m_touchEndPos;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            m_touchStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            m_touchEndPos = Input.mousePosition;
            TouchEnded();
        }
    }

    private void TouchEnded()
    {
        Vector2 touchDistance = m_touchEndPos - m_touchStartPos;

        // If swipe has small distance it is probably a tap.
        if (touchDistance.magnitude < 20)
        {
            // Get the average of the touch start and end position.
            Vector2 tapPosition = m_touchStartPos + (touchDistance / 2);

            HandleTap(tapPosition);
        }
    }

    private void HandleTap(Vector2 tapPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(tapPosition);

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            if (OnZoomEnded != null && hit.collider.gameObject == gameObject)
            {
                OnZoomEnded();
            }
        }
    }
}
