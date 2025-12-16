using UnityEngine;
using UnityEngine.InputSystem;

public class TabUiController : MonoBehaviour
{
    public GameObject tabMenuPanel;

    void Start()
    {
        if (tabMenuPanel != null)
            tabMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.IsGameStarted)
        {
            if (tabMenuPanel != null)
                tabMenuPanel.SetActive(false);
            return;
        }

        bool tabHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed;

        if (tabMenuPanel != null)
            tabMenuPanel.SetActive(tabHeld);

        Cursor.lockState = tabHeld ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = tabHeld;
    }

}
