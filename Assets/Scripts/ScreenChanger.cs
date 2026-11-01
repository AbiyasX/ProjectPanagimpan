using UnityEngine;

public class ScreenChanger : MonoBehaviour
{
    public GameObject[] screens;

    public int CurrentIndex { get; private set; }
    public bool IsVisible { get; private set; }
    public int ScreenCount => screens != null ? screens.Length : 0;

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        Apply();
    }

    public bool NextScreen()
    {
        HideAllScreens();

        CurrentIndex++;
        bool wrapped = CurrentIndex >= ScreenCount;
        if (wrapped)
            CurrentIndex = 0;

        Apply();
        return wrapped;
    }

    public void HideAll()
    {
        IsVisible = false;
        HideAllScreens();
    }

    void HideAllScreens()
    {
        if (screens == null) return;
        foreach (GameObject screen in screens)
            if (screen != null)
                screen.SetActive(false);
    }

    void Apply()
    {
        if (ScreenCount == 0) return;
        GameObject current = screens[CurrentIndex];
        if (current != null)
            current.SetActive(IsVisible);
    }
}