using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemUnlockEffect : MonoBehaviour
{
    //[SerializeField]
    //private Transform startPoint;
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    public Transform[] targetPoints;
    [SerializeField]
    private Image[] items;

    public void StartEffect(ItemType type)
    {
        Sprite sprite = DataContainer.Instance.itemData.GetItemUnit(type).smallIconSprite;
        for (int i = 0; i < items.Length; i++)
        {
            items[i].sprite = sprite;
            items[i].transform.localScale = Vector3.one;
            items[i].rectTransform.anchoredPosition3D = Vector3.zero;
            items[i].gameObject.SetActive(true);
        }
        StartCoroutine(EffectRoutine(type));
    }

    IEnumerator EffectRoutine(ItemType type)
    {
        int id = (int)type;
        if (id > 2)
            id = 2;
        Transform target = targetPoints[id];
        Vector3 targetScreenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, target.position);
        Vector3 localPos = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       items[0].transform.parent as RectTransform,
                                  targetScreenPos,
                                             mainCamera,
                                                        out Vector2 localPoint) ? localPoint : Vector3.zero;
        //Debug.Log("Target localPos: " + localPoint);
        Vector3 targetPos = localPoint;
        Vector3 targetScale = Vector3.one * 1.15f;
        for(int i = 0; i < items.Length; i++)
        {
            RectTransform itemTr = items[i].rectTransform;
            //itemTr.gameObject.SetActive(true);
            SoundManager.PlaySound(SoundType.UnlockItem);
            itemTr.anchoredPosition3D = Vector3.zero;
            itemTr.DOAnchorPos(targetPos, 0.6f).OnComplete(()=> {
                itemTr.gameObject.SetActive(false);
                
                target.DOScale(targetScale, 0.1f).OnComplete(() =>
                {
                    target.DOScale(Vector3.one, 0.1f);
                });
            });
            yield return new WaitForSeconds(0.15f);
        }
    }
}
