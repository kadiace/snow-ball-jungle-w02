using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PunkScene : MonoBehaviour
{
    private readonly Color COLOR_ON = new Color32(46, 204, 113, 255);
    private readonly Color COLOR_OFF = new Color32(231, 76, 60, 255);

    [SerializeField] private GameObject _directionalLight;
    [SerializeField] private float _secondsPerDay = 60f;

    private GameObject _prizeUI;
    private GameObject _gameUI;
    private Image _activePanel;
    private Text _activeText;
    private Slider _energe;
    private Text _treeText;
    private Text _ironText;
    private Text _consumeText;
    private Text _consumeSignText;
    private Text _currentDayText;
    private Text _currentDegreeText;
    private Text _nextDayText;
    private Text _nextDegreeText;
    private Text _timeText;
    private Text _sizeText;

    private Quaternion _initialRotation;

    void Awake()
    {
        _prizeUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/PrizeCanvas"));
        _prizeUI.SetActive(false);

        InitGameUI();

        _initialRotation = _directionalLight.transform.rotation;
    }

    void Update()
    {
        Managers.Game.ElapsedTime += Time.deltaTime;
        RotateSun();
        SetGameUI();
    }

    private void InitGameUI()
    {
        _gameUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/GameCanvas"));

        _activePanel = _gameUI.transform.Find("Panel/LeftPanel/OnoffPanel").GetComponent<Image>();
        _activeText = _gameUI.transform.Find("Panel/LeftPanel/OnoffPanel/Onoff").GetComponent<Text>();
        _energe = _gameUI.transform.Find("Panel/LeftPanel/EnergyPanel/Energy").GetComponent<Slider>();

        _treeText = _gameUI.transform.Find("Panel/LeftPanel/TreePanel/Tree").GetComponent<Text>();
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

    private void SetGameUI()
    {
        _currentDayText.text = $"Day {Managers.Game.CurrentDay}";
        _currentDegreeText.text = $"{Managers.Game.CurrentDegree} °C";

        if (Managers.Game.ResourcesData.IsActive)
        {
            _activePanel.color = COLOR_ON;
            _activeText.text = "ON";
        }
        else
        {
            _activePanel.color = COLOR_OFF;
            _activeText.text = "OFF";
        }
        _energe.value = Managers.Game.ResourcesData.CurrentEnergy / Managers.Game.ResourcesData.MaxEnergy;

        _nextDayText.text = $"Day {Managers.Game.CurrentDay + 1}";
        _nextDegreeText.text = $"{Managers.Game.NextDegree} °C";

        _treeText.text = $"나무: {Managers.Game.ResourcesData.Tree}";
        _ironText.text = $"철: {Managers.Game.ResourcesData.Metal}";

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
        _sizeText.text = $"눈덩이 무게: {Managers.Game.ResourcesData.Mass}";
    }
}
