using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PassSystem : MonoBehaviour
{
    public static PassSystem Instance { get; private set; }

    [SerializeField] private GameObject passUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OpenPassScreen()
    {
        passUI.SetActive(true);
    }

    public void ClosePassScreen()
    {
        passUI.SetActive(false);
    }
}
