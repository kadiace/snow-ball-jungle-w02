using UnityEngine;

public abstract class FacilityInteractionController : MonoBehaviour
{
    protected GameObject _canvas;

    protected virtual void Awake()
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

    public virtual void OpenCanvas()
    {
        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _canvas.SetActive(true);
    }

    protected void CloseCanvas()
    {
        GameInputController.Instance.SetInputMode(InputMode.Player);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _canvas.SetActive(false);
    }
}
