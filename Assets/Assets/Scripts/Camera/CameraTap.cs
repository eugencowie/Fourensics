using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Camera)), RequireComponent(typeof(CameraSwipe))]
public class CameraTap : MonoBehaviour
{
    [SerializeField] private GameObject BlurPlane = null;
    [SerializeField] private GameObject InventoryController = null;
    [SerializeField] private GameObject HintPanel = null;
    [SerializeField] private GameObject HintText = null;
    [SerializeField] private GameObject Spotlight = null;

    private Inventory Inventory
    {
        get { return InventoryController.GetComponent<Inventory>(); }
    }

    private Vector2 m_touchStartPos;
    private Vector2 m_touchEndPos;
    private bool m_isTouching;

    private Camera m_camera;
    private CameraSwipe m_cameraSwipe;

    bool isInpecting = false;
    bool isZoomed = false;

    Stack<GameObject> hints = new Stack<GameObject>();

    private void Start()
    {
        m_camera = GetComponent<Camera>();
        m_cameraSwipe = GetComponent<CameraSwipe>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            m_touchStartPos = Input.mousePosition;
            m_isTouching = true;
        }

        if (!Input.GetMouseButton(0) && m_isTouching)
        {
            m_isTouching = false;
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
        Ray ray = m_camera.ScreenPointToRay(tapPosition);

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            TapObject(hit.collider.gameObject);
        }
    }

    private void TapObject(GameObject tappedObject)
    {
        // If hit object has the inspectable component, we can inspect it
        ObjectInspectable inspectable = tappedObject.GetComponent<ObjectInspectable>();
        if (inspectable != null)
        {
            InspectObject(inspectable);
        }

        // If hit object has the zoomable component, we can zoom in on it
        ObjectZoomable zoomable = tappedObject.GetComponent<ObjectZoomable>();
        if (zoomable != null && !isZoomed)
        {
            ZoomToObject(zoomable);
        }
    }

    private void InspectObject(ObjectInspectable inspectable)
    {
        if (inspectable != null && !isInpecting)
        {
            // If hit object has hints, we can add them to the inventory
            ObjectHint[] hints = inspectable.gameObject.GetComponents<ObjectHint>();
            if (hints.Length > 0) {
                Inventory.AddItems(() => TapObject(inspectable.gameObject), hints);
            }

            Text text = HintText.GetComponent<Text>();
            text.text = "";
            foreach (var hint in hints) {
                text.text += string.Format("{0}: {1}\n", hint.Name, hint.Hint);
            }
            
            if (inspectable.audioSource != null && inspectable.audioClip != null)
            {
                inspectable.audioSource.PlayOneShot(inspectable.audioClip, 1f);
            }

            GameObject newObject = Instantiate(inspectable.gameObject);
            newObject.transform.parent = m_camera.transform;
            newObject.transform.localPosition = new Vector3(0, 0, 0.1f);
            newObject.transform.localScale *= inspectable.InspectScale * 0.1f;

            newObject.AddComponent<ObjectInspecting>().OnInspectEnded = () => {
                HideHintPanel();
                BlurPlane.SetActive(false);
                if (Spotlight != null) Spotlight.SetActive(false);
                /*enabled = */m_cameraSwipe.enabled = true;
                isInpecting = false;
                Destroy(newObject);
            };

            if (hints.Length > 0) ShowHintPanel();
            if (Spotlight != null) Spotlight.SetActive(true);
            BlurPlane.SetActive(true);
            /*enabled = */m_cameraSwipe.enabled = false;
            isInpecting = true;
        }
    }

    private void ZoomToObject(ObjectZoomable zoomable)
    {
        if (zoomable != null && !isInpecting && !isZoomed && zoomable.TargetCamera != null)
        {
            // If hit object has hints, we can add them to the inventory
            ObjectHint[] hints = zoomable.gameObject.GetComponents<ObjectHint>();
            if (hints.Length > 0) {
                Inventory.AddItems(() => TapObject(zoomable.gameObject), hints);
            }

            Text text = HintText.GetComponent<Text>();
            text.text = "";
            foreach (var hint in hints) {
                text.text += string.Format("{0}: {1}\n", hint.Name, hint.Hint);
            }

            // Create an inactive clone of this camera in its current location
            GameObject StartCamera = Instantiate(gameObject);
            StartCamera.SetActive(false);

            //Play audio
            if (zoomable.audioSource != null && zoomable.audioClip != null)
            {
                zoomable.audioSource.PlayOneShot(zoomable.audioClip, 1f);
            }

            // Add a camera movement component
            CameraMovement movement = gameObject.AddComponent<CameraMovement>();

            // Set camera movement parameters
            movement.Target = zoomable.TargetCamera;
            movement.Duration = zoomable.Duration;

            // Set what happens when the camera movement ends
            movement.OnMoveEnded = () => {
                if (hints.Length > 0) ShowHintPanel();
                ObjectZooming zooming = zoomable.gameObject.AddComponent<ObjectZooming>();
                zooming.OnZoomEnded = () => {
                    //if (isInpecting) return;
                    HideHintPanel();
                    movement.Target = StartCamera;
                    movement.OnMoveEnded = () => {
                        /*enabled = */m_cameraSwipe.enabled = true;
                        isZoomed = false;
                        Destroy(StartCamera);
                        Destroy(movement);
                    };

                    // Re-enable and restart the camera movement controller to take us home
                    movement.enabled = true;
                    movement.Reset();

                    // Our job here is done, delete the zooming component
                    Destroy(zooming);
                };

                // Disable the movement component
                movement.enabled = false;
            };

            // Disable this component and disable the camera swipe component
            /*enabled = */m_cameraSwipe.enabled = false;
            isZoomed = true;
        }
    }

    private void HideHintPanel()
    {
        var go = hints.Pop();
        go.SetActive(false);
        Destroy(go);

        if (hints.Count > 0)
        {
            hints.Peek().SetActive(true);
            HintPanel.transform.parent.gameObject.SetActive(true);
        }
        else
        {

            HintPanel.transform.parent.gameObject.SetActive(false);
        }
    }

    private void ShowHintPanel()
    {
        HintPanel.transform.parent.gameObject.SetActive(true);

        if (hints.Count > 0)
        {
            hints.Peek().SetActive(false);
        }
        
        var go = Instantiate(HintPanel, HintPanel.transform.parent);
        go.SetActive(true);
        hints.Push(go);
    }
}
