using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static GameObject itemBeingDragged;
    Vector3 startPosition;
    Transform startParent;

    private void Start()
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (enabled)
        {
            itemBeingDragged = Instantiate(gameObject, gameObject.transform.parent.parent.parent.parent);
            itemBeingDragged.name = gameObject.name;
            startPosition = itemBeingDragged.transform.position;
            startParent = gameObject.transform.parent;
            itemBeingDragged.GetComponent<Image>().raycastTarget = false;
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (enabled)
        {
            itemBeingDragged.transform.position = eventData.position;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (enabled)
        {
            itemBeingDragged.GetComponent<Image>().raycastTarget = true;
            if (itemBeingDragged.transform.parent == startParent)
            {
                itemBeingDragged.transform.position = startPosition;
            }
            Destroy(itemBeingDragged);
            itemBeingDragged = null;
        }
    }
}
