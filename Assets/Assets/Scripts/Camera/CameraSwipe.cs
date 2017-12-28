using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraSwipe : MonoBehaviour
{
    [SerializeField] private int LeftConstraint = 20;
    [SerializeField] private int RightConstraint = 255;

    private const float m_turnSpeed = 100.0f;
    private Vector3 m_touchOrigin;
    private bool m_isRotating;

    private Camera m_camera;

    private void Start()
    {
        m_camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            m_touchOrigin = Input.mousePosition;
            m_isRotating = true;
        }

        if (!Input.GetMouseButton(0))
        {
            m_isRotating = false;
        }

        if (m_isRotating)
        {
            Vector3 screenPos = m_camera.ScreenToViewportPoint(Input.mousePosition - m_touchOrigin);

            float movement = screenPos.normalized.x * m_turnSpeed * Time.deltaTime * -1;

            if (transform.rotation.eulerAngles.y > LeftConstraint - movement && transform.rotation.eulerAngles.y < RightConstraint - movement)
            {
                transform.RotateAround(transform.position, Vector3.up, movement);
            }
        }
    }
}
