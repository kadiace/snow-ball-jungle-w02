using UnityEngine;

public class FacilityInteractionController : MonoBehaviour
{
    private GameObject _canvas;

    void Awake()
    {
        _canvas = transform.Find("Canvas").gameObject;
        _canvas.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        PlayerController player = other.GetComponent<PlayerController>();
        player.Facilities.Add(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        PlayerController player = other.GetComponent<PlayerController>();
        player.Facilities.Remove(this);
    }

    public void OpenCanvas()
    {
        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _canvas.SetActive(true);
    }

    private void CloseCanvas()
    {
        GameInputController.Instance.SetInputMode(InputMode.Player);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _canvas.SetActive(false);
    }
}
