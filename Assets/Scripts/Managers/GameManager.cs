using System.Collections.Generic;
using UnityEngine;

public class ResourcesData
{
    public int Tree;
    public int Metal;
    public bool IsActive;
    public float CurrentEnergy;
    public float MaxEnergy;
    public float Scale;
    public float Mass;
}

public class GameManager
{
    private readonly Dictionary<int, int> _temperatures = new()
    {
        {1, -10},
        {2, -10},
        {3, -10},
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

    private readonly Dictionary<int, int> _consumes = new()
    {
        {-10, -5},
        {-20, -10},
        {-30, -15},
        {-40, -20},
    };

    private ResourcesData _resources;
    private float _elapsedTime;
    private float _secondsPerDay = 60f;
    public float _consume => _consumes[CurrentDegree];

    public ResourcesData ResourcesData { get { return _resources; } set { _resources = value; } }
    public float ElapsedTime { get { return _elapsedTime; } set { _elapsedTime = value; } }
    public int CurrentDay =>
        Mathf.FloorToInt((float)(_elapsedTime / _secondsPerDay)) + 1;
    public int CurrentDegree => _temperatures[CurrentDay];
    public int NextDegree => _temperatures[CurrentDay + 1];
    public float ElapsedInDayProgress => _elapsedTime % _secondsPerDay / _secondsPerDay;
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
    public float EnergyGain;
    public float EnergyDelta => _consume + EnergyGain;

    public void Init()
    {
        _resources = new()
        {
            Tree = 0,
            Metal = 0,
            IsActive = true,
            CurrentEnergy = 50,
            MaxEnergy = 100,
            Scale = 6,
            Mass = 3,
        };
        _elapsedTime = 0f;
    }

    public void Clear()
    {
        _resources = new()
        {
            Tree = 0,
            Metal = 0,
            IsActive = true,
            CurrentEnergy = 50,
            MaxEnergy = 100,
            Scale = 6,
            Mass = 3
        };
        _elapsedTime = 0f;
    }
}
