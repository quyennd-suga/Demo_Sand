using System;

public interface IUIElement
{
    bool IsActive { get; }
    void Show(Action onComplete = null);
    void Hide(Action onComplete = null);
}
