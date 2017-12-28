using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VotingDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static GameObject itemBeingDragged;
    Vector3 startPosition;
    Transform startParent;
    
    public VotingSuspect Suspect;

    private void Start()
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (enabled)
        {
            itemBeingDragged = Instantiate(gameObject, gameObject.transform.parent);
            itemBeingDragged.name = gameObject.name;
            startPosition = itemBeingDragged.transform.position;
            startParent = gameObject.transform.parent;
            //itemBeingDragged.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var color = gameObject.GetComponent<Image>().color;
            color.a = 0.5f;
            gameObject.GetComponent<Image>().color = color;
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
            Destroy(itemBeingDragged);
            //itemBeingDragged.GetComponent<CanvasGroup>().blocksRaycasts = true;
            if (itemBeingDragged.transform.parent == startParent)
            {
                itemBeingDragged.transform.position = startPosition;
            }
            itemBeingDragged = null;

            var color = gameObject.GetComponent<Image>().color;
            color.a = 1.0f;
            gameObject.GetComponent<Image>().color = color;
        }
    }
}
