using System.Collections.Generic;
using UnityEngine;

public class GameManager
{
    private GameObject _prizeUI;
    private GameObject _gameUI;

    public GameObject PrizeUI { get { return _prizeUI; } }
    public GameObject GameUI { get { return _gameUI; } }

    public void Init()
    {
        _prizeUI = Resources.Load<GameObject>("Prefabs/UIs/PrizeCanvas");
        _gameUI = Resources.Load<GameObject>("Prefabs/UIs/DateCanvas");
    }

    public void Clear()
    {

    }
}
