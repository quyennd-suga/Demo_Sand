using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : PopupUI
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // Cache các manager references ở đây nếu cần
        // gameManager = GameManager.Instance;
        // audioManager = AudioManager.Instance;
    }

    protected override void OnShow()
    {
        base.OnShow();
        Debug.Log("Settings Popup hiển thị");
    }
    public void OnClickNoads()
    {

    }   
    public void OnClickQuitLevel()
    {

    }    

}
