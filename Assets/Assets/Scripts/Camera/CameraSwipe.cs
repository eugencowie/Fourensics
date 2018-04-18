using UnityEngine;

public class CameraSwipe : MonoBehaviour
{
    [SerializeField] bool m_enableConstraints = true;
    [SerializeField] int LeftConstraint = 20;
    [SerializeField] int RightConstraint = 255;

    const float m_turnSpeed = 500.0f;
    Vector3 m_touchOrigin;
    bool m_isTouching;

    float m_velocity = 0;

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
        }

        if (m_isTouching)
        {
            Vector3 screenPos = Camera.main.ScreenToViewportPoint(Input.mousePosition - m_touchOrigin);
            m_velocity = screenPos.x * m_turnSpeed * Time.deltaTime * -1;
        }

        if (m_enableConstraints == false || (transform.rotation.eulerAngles.y > LeftConstraint - m_velocity &&
            transform.rotation.eulerAngles.y < RightConstraint - m_velocity))
        {
            transform.RotateAround(transform.position, Vector3.up, m_velocity);
        }

        if (Mathf.Abs(m_velocity) > 0.01)
        {
            m_velocity *= 0.9f;
        }
        else
        {
            m_velocity = 0;
        }
    }
}
