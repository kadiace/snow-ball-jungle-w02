using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ResearchType
{
    SelfGenerate,
    SelfGenerate2,
    TowerMax,
    TowerEfficiency,
    TowerWire,
    MoveFast,

}

public class ResourcesData
{
    public float CurrentEnergy;
    public float MaxEnergy;
    public float Scale;
    public float Mass;
}

public class GameManager
{
    private readonly Dictionary<int, int> _temperatures = new()
    {
        {1, 0},
        {2, -2},
        {3, -5},
        {4, -20},
        {5, -10},
        {6, -20},
        {7, -10},
        {8, -10},
        {9, -20},
        {10, -30},
        {11, -20},
        {12, -10},
        {13, -20},
        {14, -30},
        {15, -40},
        {16, -40},
    };

    private readonly Dictionary<int, int> _labConsumes = new()
    {
        {0, -5},
        {-2, -5},
        {-5, -5},
        {-10, -5},
        {-20, -10},
        {-30, -15},
        {-40, -20},
    };

    private ResourcesData _resources;
    private float _elapsedTime;
    private float _labConsume => _labConsumes[CurrentDegree];
    private float _energyGain => Wheels.Count * 10 + ActivatedTowers.Count * 5;

    public List<TowerController> Towers { get; private set; }

    public ResourcesData ResourcesData { get { return _resources; } set { _resources = value; } }
    public List<GameObject> Woods { get; private set; }
    public List<GameObject> Irons { get; private set; }
    public List<Wheel> Wheels { get; private set; }
    public List<TowerController> ActivatedTowers { get; private set; }

    public readonly float SecondsPerDay = 60;
    public float ElapsedTime { get { return _elapsedTime; } set { _elapsedTime = value; } }
    public int CurrentDay =>
        Mathf.FloorToInt((float)(_elapsedTime / SecondsPerDay)) + 1;
    public int CurrentDegree => _temperatures[CurrentDay];
    public int NextDegree => _temperatures[CurrentDay + 1];
    public float ElapsedInDayProgress => _elapsedTime % SecondsPerDay / SecondsPerDay;
    public string CurrentTime
    {
        get
        {
            double totalMinutes = ElapsedInDayProgress * 24 * 60;

            int hour = (int)((totalMinutes / 60) + 6) % 24;
            int minute = (int)totalMinutes % 60;

            return $"{hour:00}:{minute:00}";
        }
    }
    public float EnergyDelta => _labConsume + _energyGain;
    public PunkScene Scene;

    public void Init()
    {
        _resources = new()
        {
            CurrentEnergy = 100,
            MaxEnergy = 200,
            Scale = 6,
            Mass = 3,
        };
        Woods = new();
        Irons = new();
        Wheels = new();
        Towers = new();
        ActivatedTowers = new();
        _elapsedTime = 0f;
    }

    public void Clear()
    {

    }

    public void PopWood(int count)
    {
        PopObjects(Woods, count);
    }

    public void PopIron(int count)
    {
        PopObjects(Irons, count);
    }

    private void PopObjects(List<GameObject> objects, int count)
    {
        count = Mathf.Min(count, objects.Count);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = objects[^1];
            objects.RemoveAt(objects.Count - 1);

            Object.Destroy(obj);
        }

    }

    public void OpenGuideUI(GameState gameState)
    {
        Scene.OpenGuideUI(gameState);
    }

    public void ReloadScene()
    {
        Managers.Clear();
        SceneManager.LoadScene(0);
    }
}
