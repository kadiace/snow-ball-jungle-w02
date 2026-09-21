
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum LabButton
{
    Activate,
    Repair
}

public class LabController : FacilityInteractionController
{
    private InputAction _confirmAction;
    private InputAction _cancelAction;

    private Material _lightOnMaterial;
    private Material _lightOffMaterial;

    [SerializeField] private Renderer[] _lightRenderers;

    protected override void Awake()
    {
        base.Awake();
        var buttons = _canvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            switch (button.name)
            {
                case nameof(LabButton.Activate):
                    button.onClick.AddListener(OnActivateButtonClicked);
                    break;
                case nameof(LabButton.Repair):
                    button.onClick.AddListener(OnRepairButtonClicked);
                    break;
            }
        }

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnConfirmPerformed;

        _cancelAction = InputSystem.actions.FindAction("Cancel");
        _cancelAction.performed += OnCancelPerformed;

        Transform light = transform.Find("Light");

        _lightOnMaterial = Resources.Load<Material>("Materials/LightOn");
        _lightOffMaterial = Resources.Load<Material>("Materials/LightOff");
    }

    void Update()
    {
        foreach (var renderer in _lightRenderers)
            renderer.sharedMaterial = Managers.Game.ResourcesData.CurrentEnergy > 0 ? _lightOnMaterial : _lightOffMaterial;
    }

    void OnDestroy()
    {
        _confirmAction.performed -= OnConfirmPerformed;
        _cancelAction.performed -= OnCancelPerformed;
    }

    private void OnActivateButtonClicked()
    {

    }

    private void OnRepairButtonClicked()
    {

    }

    private void OnConfirmPerformed(InputAction.CallbackContext context)
    {
        if (!_canvas.activeSelf)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            // EventSystem.current.SetSelectedGameObject(_defaultButton.gameObject);
            return;
        }

        Button button = selectedObject.GetComponent<Button>();
        button.onClick.Invoke();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (!_canvas.activeSelf)
            return;

        CloseCanvas();
    }

    public override void OpenCanvas()
    {
        base.OpenCanvas();
    }
}