using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ResearchType
{
    SelfGenerate,
    SelfGenerate2,
    BatteryMax,
    TowerEfficiency,
    WheelPower,
    WheelPower2,
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
        {4, -10},
        {5, -20},
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
        {0, -2},
        {-2, -3},
        {-5, -3},
        {-10, -5},
        {-20, -10},
        {-30, -15},
        {-40, -20},
    };

    private readonly Dictionary<ResearchType, string> _researchTitles = new()
    {
        {ResearchType.SelfGenerate, "자가 발전"},
        {ResearchType.SelfGenerate2, "자가 발전 2"},
        {ResearchType.BatteryMax, "대용량 배터리"},
        {ResearchType.TowerEfficiency, "발전소 재설계"},
        {ResearchType.TowerWire, "신속한 복귀"},
        {ResearchType.WheelPower, "휠 오브 페인"},
        {ResearchType.WheelPower2, "휠 오브 페인 2"},
        {ResearchType.MoveFast, "신속한 이동"},
    };

    private readonly Dictionary<ResearchType, string> _researchDescriptions = new()
    {
        {ResearchType.SelfGenerate, "종자 보관소에 태양열 발전기를 달아 자체 전력을 생산합니다. 시간 당 5 생산"},
        {ResearchType.SelfGenerate2, "태양열 발전기를 강화합니다. 시간 당 10 생산"},
        {ResearchType.BatteryMax, "종자 보관소의 전력 보관 배터리 성능을 강화합니다. 100 > 200"},
        {ResearchType.TowerEfficiency, "발전소의 내구도 감소율을 절반으로 줄입니다. 시간 당 5 > 2.5"},
        {ResearchType.TowerWire, "발전소에 연결된 와이어를 타고 종자 보관소로 빠르게 복귀합니다. 발전소 근처에서 눌러 발동하고, 활성한 발전소에서만 사용할 수 있습니다."},
        {ResearchType.WheelPower, "바퀴를 굴려 얻는 전력량이 증가합니다. 시간 당 10 > 15"},
        {ResearchType.WheelPower2, "바퀴를 굴려 얻는 전력량이 증가합니다. 시간 당 15 > 20"},
        {ResearchType.MoveFast, "이동 속도가 현재의 2배가 됩니다."},
    };

    private readonly Dictionary<ResearchType, (int, int)> _researchCosts = new()
    {
        {ResearchType.SelfGenerate, (5, 5)},
        {ResearchType.SelfGenerate2, (10, 10)},
        {ResearchType.BatteryMax, (10, 10)},
        {ResearchType.TowerEfficiency, (15, 15)},
        {ResearchType.TowerWire, (15, 15)},
        {ResearchType.WheelPower, (5, 5)},
        {ResearchType.WheelPower2, (10, 10)},
        {ResearchType.MoveFast, (5, 5)},
    };
    private List<ResearchType> _researches = new();

    private ResourcesData _resources;
    private float _elapsedTime;
    private float _labConsume => _labConsumes[CurrentDegree];
    private float _energyGain
    {
        get
        {
            float wheelPower = Managers.Game.Researches.Contains(ResearchType.WheelPower) ?
                Managers.Game.Researches.Contains(ResearchType.WheelPower2) ? 15 : 10 : 5;
            float baseGain = Wheels.Count * wheelPower + ActivatedTowers.Count * 5;
            float researchGain = Managers.Game.Researches.Contains(ResearchType.SelfGenerate) ?
                Managers.Game.Researches.Contains(ResearchType.SelfGenerate2) ? 10 : 5 : 0;

            return baseGain + researchGain;
        }
    }

    public List<TowerController> Towers { get; private set; }

    public ResourcesData ResourcesData { get { return _resources; } set { _resources = value; } }
    public List<GameObject> Woods { get; private set; }
    public List<GameObject> Irons { get; private set; }
    public List<Wheel> Wheels { get; private set; }
    public List<TowerController> ActivatedTowers { get; private set; }

    public readonly float SecondsPerDay = 24;
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

    public Dictionary<ResearchType, string> ResearchTitles => _researchTitles;
    public Dictionary<ResearchType, string> ResearchDescriptions => _researchDescriptions;
    public Dictionary<ResearchType, (int, int)> RearchCosts => _researchCosts;
    public List<ResearchType> Researches => _researches;

    public void Init()
    {
        _resources = new()
        {
            CurrentEnergy = 100,
            MaxEnergy = 200,
            Scale = 6,
            Mass = 3,
        };
        Woods = new()
        {
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
        };
        Irons = new()
        {
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
            new GameObject(),
        };
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
