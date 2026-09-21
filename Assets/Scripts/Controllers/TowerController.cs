
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum TowerButton
{
    Activate,
    Repair
}

public class TowerController : FacilityInteractionController
{
    private Text _wood;
    private Text _iron;
    private GameObject _durationPanel;
    private Slider _duration;
    private Button _confirm;
    private Text _confirmText;

    private InputAction _confirmAction;
    private InputAction _cancelAction;

    private bool _isActivated;
    private int _actualRepairWoodCost;
    private int _actualRepairIronCost;

    static private bool _hasTower;

    private Renderer _lightRenderer;
    private Material _lightOnMaterial;
    private Material _lightOffMaterial;
    private LineRenderer _line;
    private Vector3 _startPoint;
    private Vector3 _endPoint = Vector3.up * 200;

    [Header("Activate")]
    [SerializeField] private int _activateWoodCost;
    [SerializeField] private int _activateIronCost;

    [Header("Repair")]
    [SerializeField] private int _repairWoodCost;
    [SerializeField] private int _repairIronCost;

    [Header("Duration")]
    [SerializeField] private float _durationDecreasePerHour = 3f;

    [Header("Line")]
    [SerializeField] private float _sagAmount = 2f;
    [SerializeField] private int _segments = 30;
    [SerializeField] private float _width = 0.1f;

    public float Duration { get; set; }
    public float MaxDuration { get; set; }

    protected override void Awake()
    {
        base.Awake();

        Managers.Game.Towers.Add(this);

        _wood = _canvas.transform.Find("Panel/ResourcePanel/TextPanel/Wood").GetComponent<Text>();
        _iron = _canvas.transform.Find("Panel/ResourcePanel/TextPanel/Iron").GetComponent<Text>();

        _durationPanel = _canvas.transform.Find("Panel/ResourcePanel/DurationPanel").gameObject;
        _duration = _canvas.transform.Find("Panel/ResourcePanel/DurationPanel/Duration").GetComponent<Slider>();

        _confirm = _canvas.transform.Find("Panel/ButtonPanel/Confirm").GetComponent<Button>();
        _confirm.onClick.AddListener(OnButtonClicked);
        _confirmText = _canvas.transform.Find("Panel/ButtonPanel/Confirm/Confirm").GetComponent<Text>();

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnConfirmPerformed;

        _cancelAction = InputSystem.actions.FindAction("Cancel");
        _cancelAction.performed += OnCancelPerformed;

        MaxDuration = 100;

        Transform light = transform.Find("Light");
        _lightRenderer = light.GetComponent<Renderer>();

        _lightOnMaterial = Resources.Load<Material>("Materials/LightOn");
        _lightOffMaterial = Resources.Load<Material>("Materials/LightOff");
        _line = GetComponent<LineRenderer>();

        _startPoint = transform.position + Vector3.up * 100;
    }
    private void Start()
    {
        _line.useWorldSpace = true;
        _line.material = _lightOffMaterial;
        _line.startWidth = _width;
        _line.endWidth = _width;

        int count = Mathf.Max(2, _segments);
        _line.positionCount = count + 1;

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            Vector3 point = Vector3.Lerp(_startPoint, _endPoint, t);
            float sag = 4f * _sagAmount * t * (1f - t);
            point += Vector3.down * sag;
            _line.SetPosition(i, point);
        }
    }

    private void OnDestroy()
    {
        _confirmAction.performed -= OnConfirmPerformed;
        _cancelAction.performed -= OnCancelPerformed;
    }

    void Update()
    {
        float secondsPerHour = Managers.Game.SecondsPerDay / 24f;

        Duration -= _durationDecreasePerHour * (Time.deltaTime / secondsPerHour);
        Duration = Mathf.Clamp(Duration, 0f, MaxDuration);

        if (Duration <= 0f)
            DeactivateTower();

        _lightRenderer.material.SetFloat("_Split", Duration / MaxDuration);
    }

    private void OnButtonClicked()
    {

        if (_isActivated)
        {
            Managers.Game.PopWood(_actualRepairWoodCost);
            Managers.Game.PopIron(_actualRepairIronCost);
            Duration = MaxDuration;
            SetUI();
        }
        else
        {
            if (!_hasTower)
            {
                Managers.Game.OpenGuideUI(GameState.TOWER);
                _hasTower = true;
            }
            Managers.Game.PopWood(_activateWoodCost);
            Managers.Game.PopIron(_activateIronCost);
            _isActivated = true;
            Duration = MaxDuration;
            Managers.Game.ActivatedTowers.Add(this);
            _lightRenderer.material.SetFloat("_Split", Duration / MaxDuration);
            _line.material = _lightOnMaterial;
            SetUI();
        }
    }

    private void OnConfirmPerformed(InputAction.CallbackContext context)
    {
        if (!_canvas.activeSelf)
            return;
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            EventSystem.current.SetSelectedGameObject(_confirm.gameObject);
            return;
        }

        Button button = selectedObject.GetComponent<Button>();
        if (!button.interactable)
            return;
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
        SetUI();
        base.OpenCanvas();
    }

    private void SetUI()
    {
        float durationRatio = Duration / MaxDuration;
        _duration.value = durationRatio;

        _actualRepairWoodCost = Mathf.CeilToInt(_repairWoodCost * (1 - durationRatio));
        _actualRepairIronCost = Mathf.CeilToInt(_repairIronCost * (1 - durationRatio));

        int woodCost;
        int ironCost;

        if (_isActivated)
        {
            _durationPanel.SetActive(true);

            woodCost = _actualRepairWoodCost;
            ironCost = _actualRepairIronCost;

            _confirmText.text = "수리하기";
        }
        else
        {
            _durationPanel.SetActive(false);

            woodCost = _activateWoodCost;
            ironCost = _activateIronCost;

            _confirmText.text = "활성화하기";
        }

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
    }

    private void DeactivateTower()
    {
        _isActivated = false;
        Managers.Game.ActivatedTowers.Remove(this);
        _line.material = _lightOffMaterial;
    }
}