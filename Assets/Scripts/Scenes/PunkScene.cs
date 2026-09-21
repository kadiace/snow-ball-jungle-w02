using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    INITIAL,
    TOWER,
    TEMPERATURE,
    DANGER,
    GAMEOVER,
}

public class PunkScene : MonoBehaviour
{
    private readonly Color COLOR_ON = new Color32(46, 204, 113, 255);
    private readonly Color COLOR_OFF = new Color32(231, 76, 60, 255);
    private readonly Color COLOR_ENERGY = new Color32(102, 213, 255, 255);
    private readonly Color COLOR_EMERGENCY = new Color32(255, 102, 102, 255);

    static string GUIDE_INITIAL = @"종자 보관소에 전력 공급이 끊겨 복구 프로토콜이 동작했고
    그 결과 당신이 깨어났습니다.
    
    위기가 찾아오기 전까지 다양한 방법으로 전력을 확보하고
    종자 보관소의 온도 유지 장치가 계속 가동될 수 있도록 전력을 관리해야 합니다.
    
    전력은 수급량에 따라 실시간으로 변하며
    날씨가 추워질수록 온도 유지 장치의 전력 소비량은 늘어납니다.";

    static string GUIDE_TOWER = @"축하합니다! 첫 에너지 중개소 활성화에 성공했습니다.
    
    하지만 활성화되었다고 안심할 순 없습니다.
    에너지 중개소는 추위, 시간에 따라 실시간으로 내구도가 감소하며,
    내구도가 전부 감소하면 다시 비활성화됩니다.
    
    비활성화되기 전에 자원을 이용해 내구도를 수리할 수 있습니다.";

    static string GUIDE_TEMPERATURE = @"기온이 감소했습니다.
    
    종자 보관소 온도 유지 장치의 전력 소모량이 증가하고,
    에너지 중개소의 내구도가 더 빠르게 감소합니다.
    
    전력을 유지할 방법을 찾아야 합니다.";

    static string GUIDE_DANGER = @"전력을 모두 소모했습니다.
    30초 안으로 전력 공급원을 찾지 못하면
    종자 보관소가 복구할 수 없는 피해를 입습니다.";

    static string GUIDE_GAMEOVER = @"게임 오버!
    종자 보관소를 지키는데 실패했습니다.
    다시 시도하시겠습니까?";

    static Dictionary<GameState, string> _guideTexts = new()
    {
      {GameState.INITIAL, GUIDE_INITIAL},
      {GameState.TOWER, GUIDE_TOWER},
      {GameState.TEMPERATURE, GUIDE_TEMPERATURE},
      {GameState.DANGER, GUIDE_DANGER},
      {GameState.GAMEOVER, GUIDE_GAMEOVER},
    };

    [SerializeField] private GameObject _directionalLight;
    [SerializeField] private GameObject _lab;
    [SerializeField] private float _gameOverTime = 30f;

    private float _gameOverTimer;
    private bool _isGameOver;

    static private bool _hasTemperature;
    static private bool _hasDanger;

    private GameObject _guideUI;
    private GameObject _gameUI;
    private Image _activePanel;
    private Text _activeText;
    private Text _energyText;
    private Slider _energe;
    private Text _woodText;
    private Text _ironText;
    private Text _consumeText;
    private Text _consumeSignText;
    private Text _currentDayText;
    private Text _currentDegreeText;
    private Text _nextDayText;
    private Text _nextDegreeText;
    private Text _timeText;
    private Text _sizeText;
    private InputAction _confirmAction;
    private InputAction _cancelAction;

    private Quaternion _initialRotation;

    void Awake()
    {
        Managers.Game.Scene = this;

        _gameOverTimer = _gameOverTime;

        _guideUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/GuideCanvas"));
        _guideUI.SetActive(false);

        var button = _guideUI.GetComponentInChildren<Button>();
        button.onClick.AddListener(OnGuideUIButtonClicked);

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnConfirmPerformed;

        _cancelAction = InputSystem.actions.FindAction("Cancel");
        _cancelAction.performed += OnCancelPerformed;

        InitGameUI();
        _initialRotation = _directionalLight.transform.rotation;
    }

    void Start()
    {
        Managers.Game.OpenGuideUI(GameState.INITIAL);
    }

    void Update()
    {
        CheckGameOver();
        Managers.Game.ElapsedTime += Time.deltaTime;
        RotateSun();
        ApplyEnergyDelta();
        SetGameUI();
    }

    private void OnDestroy()
    {
        _confirmAction.performed -= OnConfirmPerformed;
        _cancelAction.performed -= OnCancelPerformed;
    }

    private void InitGameUI()
    {
        _gameUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/GameCanvas"));

        _activePanel = _gameUI.transform.Find("Panel/LeftPanel/OnoffPanel").GetComponent<Image>();
        _activeText = _gameUI.transform.Find("Panel/LeftPanel/OnoffPanel/Onoff").GetComponent<Text>();
        _energyText = _gameUI.transform.Find("Panel/LeftPanel/EnergyPanel/EnergyText").GetComponent<Text>();
        _energe = _gameUI.transform.Find("Panel/LeftPanel/EnergyPanel/Energy").GetComponent<Slider>();

        _woodText = _gameUI.transform.Find("Panel/LeftPanel/WoodPanel/Wood").GetComponent<Text>();
        _ironText = _gameUI.transform.Find("Panel/LeftPanel/IronPanel/Iron").GetComponent<Text>();
        _consumeText = _gameUI.transform.Find("Panel/LeftPanel/EnergyConsumePanel/EnergyConsume").GetComponent<Text>();
        _consumeSignText = _gameUI.transform.Find("Panel/LeftPanel/EnergyConsumePanel/EnergySign").GetComponent<Text>();

        _currentDayText = _gameUI.transform.Find("Panel/CurrentDayPanel/CurrentDay").GetComponent<Text>();
        _currentDegreeText = _gameUI.transform.Find("Panel/CurrentDayPanel/CurrentDegree").GetComponent<Text>();
        _nextDayText = _gameUI.transform.Find("Panel/NextDayPanel/NextDay").GetComponent<Text>();
        _nextDegreeText = _gameUI.transform.Find("Panel/NextDayPanel/NextDegree").GetComponent<Text>();

        _timeText = _gameUI.transform.Find("Panel/RightPanel/TimePanel/Time").GetComponent<Text>();
        _sizeText = _gameUI.transform.Find("Panel/RightPanel/SizePanel/Size").GetComponent<Text>();
    }

    private void RotateSun()
    {
        float angle = Managers.Game.ElapsedInDayProgress * 360f;
        _directionalLight.transform.rotation =
            _initialRotation * Quaternion.AngleAxis(angle, Vector3.up);
    }

    private void ApplyEnergyDelta()
    {
        float secondsPerHour = Managers.Game.SecondsPerDay / 24f;

        Managers.Game.ResourcesData.CurrentEnergy += Managers.Game.EnergyDelta * (Time.deltaTime / secondsPerHour);
        Managers.Game.ResourcesData.CurrentEnergy = Mathf.Clamp(Managers.Game.ResourcesData.CurrentEnergy, 0f, Managers.Game.ResourcesData.MaxEnergy);
    }

    private void SetGameUI()
    {
        if (Managers.Game.CurrentDegree == -20f && !_hasTemperature)
        {
            _hasTemperature = true;
            Managers.Game.OpenGuideUI(GameState.TEMPERATURE);
        }
        _currentDayText.text = $"Day {Managers.Game.CurrentDay}";
        _currentDegreeText.text = $"{Managers.Game.CurrentDegree} °C";

        if (Managers.Game.ResourcesData.CurrentEnergy > 0)
        {
            _activePanel.color = COLOR_ON;
            _activeText.text = "ON";
            _energyText.text = "전력: ";
            _energe.fillRect.GetComponent<Image>().color = COLOR_ENERGY;
            _energe.value = Managers.Game.ResourcesData.CurrentEnergy / Managers.Game.ResourcesData.MaxEnergy;
            _gameOverTimer = _gameOverTime;
        }
        else
        {
            if (!_hasDanger)
            {
                _hasDanger = true;
                Managers.Game.OpenGuideUI(GameState.DANGER);
            }
            _activePanel.color = COLOR_OFF;
            _activeText.text = "OFF";
            _energyText.text = "정지: ";
            _energe.fillRect.GetComponent<Image>().color = COLOR_EMERGENCY;
            _energe.value = _gameOverTimer / _gameOverTime;
            _gameOverTimer -= Time.deltaTime;
        }

        _nextDayText.text = $"Day {Managers.Game.CurrentDay + 1}";
        _nextDegreeText.text = $"{Managers.Game.NextDegree} °C";

        _woodText.text = $"나무: {Managers.Game.Woods.Count}";
        _ironText.text = $"철: {Managers.Game.Irons.Count}";

        _consumeText.text = $"{Mathf.Abs(Managers.Game.EnergyDelta)}";
        _consumeSignText.text = Managers.Game.EnergyDelta switch
        {
            > 0 => "▲",
            < 0 => "▼",
            _ => "-"
        };
        _consumeSignText.color = Managers.Game.EnergyDelta switch
        {
            > 0 => COLOR_ON,
            < 0 => COLOR_OFF,
            _ => new Color(0, 0, 0, 255)
        };

        _timeText.text = $"{Managers.Game.CurrentTime}";
        _sizeText.text = $"눈덩이 무게: {Managers.Game.ResourcesData.Mass:F2}";
    }

    private void CheckGameOver()
    {
        _isGameOver = _gameOverTimer <= 0f;
        if (_isGameOver)
            Managers.Game.OpenGuideUI(GameState.GAMEOVER);
    }

    public void OpenGuideUI(GameState gameState)
    {
        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Text text = _guideUI.GetComponentInChildren<Text>();
        text.text = _guideTexts[gameState];

        _guideUI.SetActive(true);
        Managers.UI.ActiveCanvases.Add(_guideUI);
    }

    private void CloseGuideUI()
    {
        Managers.UI.ActiveCanvases.Remove(_guideUI);

        if (Managers.UI.ActiveCanvases.Count <= 0)
        {
            GameInputController.Instance.SetInputMode(InputMode.Player);
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        _guideUI.SetActive(false);
    }

    private void OnConfirmPerformed(InputAction.CallbackContext context)
    {
        if (!_guideUI.activeSelf)
            return;

        OnGuideUIButtonClicked();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (!_guideUI.activeSelf)
            return;

        OnGuideUIButtonClicked();
    }

    private void OnGuideUIButtonClicked()
    {
        if (_isGameOver)
            Managers.Game.ReloadScene();
        else
            CloseGuideUI();
    }
}
