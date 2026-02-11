using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeScreen : ScreenUI
{
    public void OnClickSetting()
    {
        UIManager.Instance.ShowPopup<SettingsPopup>();
    }
    public void OnClickPlayGame()
    {

    }
}