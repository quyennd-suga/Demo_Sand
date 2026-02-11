using UnityEngine;

public class UIUsageExample : MonoBehaviour
{
    private void Start()
    {
        // Ví dụ 1: Hiển thị screen bằng generic type
        UIManager.Instance.ShowScreen<HomeScreen>();
        UIManager.Instance.ShowPopup<PopupShop>(onComplete: (popup) =>
        {
            UIManager.Instance.HidePopup<PopupShop>();
        });
        UIManager.Instance.ShowPopup<PopupCommingSoon>(onComplete: (popup) =>
        {
            UIManager.Instance.HidePopup<PopupCommingSoon>();
        });
        // UIManager.Instance.ShowScreen<HomeScreen>("UI/Screens/HomeScreen");

        //UIManager.Instance.ShowScreen<HomeScreen>(addToHistory: true, onComplete: (screen) =>
        //{
        //    Debug.Log("HomeScreen đã load xong");
        //    // Làm gì đó với screen
        //});

        //  UIManager.Instance.ShowPopup<SettingsPopup>();

        //UIManager.Instance.ShowPopup<SettingsPopup>(onComplete: (popup) =>
        //{
        //    Debug.Log("SettingsPopup đã load xong");
        //});
        // UIManager.Instance.HidePopup<SettingsPopup>();
        // UIManager.Instance.HideAllPopups();
        //   UIManager.Instance.BackToPreviousScreen();

        //  HomeScreen homeScreen = UIManager.Instance.GetScreen<HomeScreen>();
        // SettingsPopup settingsPopup = UIManager.Instance.GetPopup<SettingsPopup>();

        // UIManager.Instance.SetUseAddressable(false); // Dùng Resources
        // UIManager.Instance.SetUseAddressable(true);  // Dùng Addressable
    }
}
