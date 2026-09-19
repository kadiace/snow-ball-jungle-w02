using System.Collections.Generic;
using UnityEngine;

public class ResourcesData
{
    public int Tree;
    public int Iron;
    public int Refrigerant;
    public float Energy;
    public float Size;
}

public class GameManager
{
    private ResourcesData _resources;
    public ResourcesData ResourcesData { get { return _resources; } set { _resources = value; } }

    public void Init()
    {
        _resources = new()
        {
            Tree = 0,
            Iron = 0,
            Refrigerant = 0,
            Energy = 0,
            Size = 6
        };
    }

    public void Clear()
    {
        _resources = new()
        {
            Tree = 0,
            Iron = 0,
            Refrigerant = 0,
            Energy = 0,
            Size = 6
        };
    }
}
