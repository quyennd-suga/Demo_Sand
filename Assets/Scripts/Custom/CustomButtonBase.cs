using UnityEngine;
using UnityEngine.EventSystems;

public abstract class CustomButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // Set Cursor
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // Play audio
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // Set Cursor
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        // Set Cursor
    }
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        // Set Cursor
    }
    //private void OnDisable()
    //{
    //    transform.localScale = Vector3.one;
    //}
}
