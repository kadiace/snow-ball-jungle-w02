
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LabController : FacilityInteractionController
{
    private InputAction _confirmAction;
    private InputAction _cancelAction;

    private Material _lightOnMaterial;
    private Material _lightOffMaterial;

    private ResearchType _currentSelectedResearchType;
    private Text _description;
    private Text _wood;
    private Text _iron;
    private Button _confirm;
    private Dictionary<ResearchType, Button> _buttons = new();

    [SerializeField] private Renderer[] _lightRenderers;

    protected override void Awake()
    {
        base.Awake();
        var buttons = _canvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (!Enum.TryParse(button.gameObject.name, out ResearchType researchType))
                continue;
            if (Managers.Game.Researches.Contains(researchType))
                continue;
            button.transform.Find("Text").GetComponent<Text>().text = Managers.Game.ResearchTitles[researchType];
            button.onClick.AddListener(() => { SetSelectedResearch(researchType); });
            button.interactable = !Managers.Game.Researches.Contains(researchType);
            _buttons[researchType] = button;
        }
        _description = _canvas.transform.Find("Panel/DetailPanel/DescriptionPanel/Description").GetComponent<Text>();
        _wood = _canvas.transform.Find("Panel/DetailPanel/PurchasePanel/Wood").GetComponent<Text>();
        _iron = _canvas.transform.Find("Panel/DetailPanel/PurchasePanel/Iron").GetComponent<Text>();
        _confirm = _canvas.transform.Find("Panel/DetailPanel/PurchasePanel/Confirm").GetComponent<Button>();

        _confirm.onClick.AddListener(Research);

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnConfirmPerformed;

        _cancelAction = InputSystem.actions.FindAction("Cancel");
        _cancelAction.performed += OnCancelPerformed;

        Transform light = transform.Find("Light");

        _lightOnMaterial = Resources.Load<Material>("Materials/LightOn");
        _lightOffMaterial = Resources.Load<Material>("Materials/LightOff");

        EventSystem.current.SetSelectedGameObject(_buttons.Values.First().gameObject);
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

    private void OnConfirmPerformed(InputAction.CallbackContext context)
    {
        if (!_canvas.activeSelf)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            EventSystem.current.SetSelectedGameObject(_buttons.Values.First().gameObject);
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

    private void Research()
    {
        (int woodCost, int ironCost) = Managers.Game.RearchCosts[_currentSelectedResearchType];
        Managers.Game.PopWood(woodCost);
        Managers.Game.PopIron(ironCost);

        Managers.Game.Researches.Add(_currentSelectedResearchType);

        _buttons[_currentSelectedResearchType].interactable = false;
        _confirm.interactable = false;
        _buttons.Remove(_currentSelectedResearchType);
    }

    private void SetSelectedResearch(ResearchType researchType)
    {
        string description = Managers.Game.ResearchDescriptions[researchType];
        (int woodCost, int ironCost) = Managers.Game.RearchCosts[researchType];

        _description.text = description;
        _wood.text = $"필요한 나무: {woodCost}";
        _iron.text = $"필요한 철: {ironCost}";

        bool hasEnoughWood = Managers.Game.Woods.Count >= woodCost;
        bool hasEnoughIron = Managers.Game.Irons.Count >= ironCost;

        _wood.color = hasEnoughWood
            ? Color.black
            : Color.red;

        _iron.color = hasEnoughIron
            ? Color.black
            : Color.red;

        _confirm.interactable = hasEnoughWood && hasEnoughIron;
        if (researchType == ResearchType.SelfGenerate2 && !Managers.Game.Researches.Contains(ResearchType.SelfGenerate)
            || researchType == ResearchType.WheelPower2 && !Managers.Game.Researches.Contains(ResearchType.WheelPower))
        {
            _confirm.interactable = false;
            _description.text += "\n선행 연구가 존재해 연구를 진행할 수 없습니다.";
        }
        _currentSelectedResearchType = researchType;
    }
}