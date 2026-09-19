using UnityEngine;
using UnityEngine.UI;

public class PunkScene : MonoBehaviour
{
    [SerializeField] private GameObject _directionalLight;

    [SerializeField] private float _secondsPerDay = 60f;

    private GameObject _prizeUI;
    private GameObject _gameUI;
    private Quaternion _initialRotation;
    private float _elapsedTime;
    private int _currentDay =>
        Mathf.FloorToInt((float)(_elapsedTime / _secondsPerDay)) + 1;
    private float _elapsedInDayProgress => _elapsedTime % _secondsPerDay / _secondsPerDay;
    private string _currentTime
    {
        get
        {
            double totalMinutes = _elapsedInDayProgress * 24 * 60;

            int hour = (int)(totalMinutes / 60);
            int minute = (int)totalMinutes % 60;

            return $"{hour:00}:{minute:00}";
        }
    }

    void Awake()
    {
        _prizeUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/PrizeCanvas"));
        _prizeUI.SetActive(false);

        _gameUI = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/GameCanvas"));
        _initialRotation = _directionalLight.transform.rotation;
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;
        RotateSun();
        SetGameUI();
    }

    private void RotateSun()
    {
        float angle = _elapsedInDayProgress * 360f;
        _directionalLight.transform.rotation =
            _initialRotation * Quaternion.AngleAxis(angle, Vector3.up);
    }

    private void SetGameUI()
    {
        Text currentDay = _gameUI.transform.Find("Panel/CurrentDayPanel/CurrentDay").GetComponent<Text>();
        currentDay.text = $"Day {_currentDay}";
        Text currentDegree = _gameUI.transform.Find("Panel/CurrentDayPanel/CurrentDegree").GetComponent<Text>();
        currentDegree.text = $"{20} °C";
        Text nextDay = _gameUI.transform.Find("Panel/NextDayPanel/NextDay").GetComponent<Text>();
        nextDay.text = $"Day {_currentDay + 1}";
        Text nextDegree = _gameUI.transform.Find("Panel/NextDayPanel/NextDegree").GetComponent<Text>();
        nextDegree.text = $"{20} °C";
        Text tree = _gameUI.transform.Find("Panel/LeftPanel/TreePanel/Tree").GetComponent<Text>();
        tree.text = $"나무: {Managers.Game.ResourcesData.Tree}";
        Text iron = _gameUI.transform.Find("Panel/LeftPanel/IronPanel/Iron").GetComponent<Text>();
        iron.text = $"철: {Managers.Game.ResourcesData.Iron}";
        Text refrigerant = _gameUI.transform.Find("Panel/LeftPanel/RefrigerantPanel/Refrigerant").GetComponent<Text>();
        refrigerant.text = $"냉매: {Managers.Game.ResourcesData.Refrigerant}";
        Text energy = _gameUI.transform.Find("Panel/LeftPanel/EnergyPanel/Energy").GetComponent<Text>();
        energy.text = $"전력: {Managers.Game.ResourcesData.Energy}";
        Text time = _gameUI.transform.Find("Panel/RightPanel/TimePanel/Time").GetComponent<Text>();
        time.text = $"{_currentTime}";
        Text size = _gameUI.transform.Find("Panel/RightPanel/SizePanel/Size").GetComponent<Text>();
        size.text = $"눈덩이 크기: {Managers.Game.ResourcesData.Size}";
    }
}