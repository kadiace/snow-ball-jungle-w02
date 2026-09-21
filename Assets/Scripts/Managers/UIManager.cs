using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    public List<GameObject> ActiveCanvases { get; private set; }

    public void Init()
    {
        ActiveCanvases = new();
    }

    public void Clear()
    {

    }
}