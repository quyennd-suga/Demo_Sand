using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitDisplay : MonoBehaviour
{
    public void OnClick()
    {
        UIManager.Instance.ShowPopup<PopupFruitParty>();
    }
}
