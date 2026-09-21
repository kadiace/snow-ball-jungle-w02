using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers _instance;

    private static Managers Instance
    {
        get
        {
            EnsureExists();
            return _instance;
        }
    }

    private readonly GameManager _gameManager = new();
    private readonly UIManager _uiManager = new();

    public static GameManager Game => Instance._gameManager;
    public static UIManager UI => Instance._uiManager;

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        Managers existing = FindAnyObjectByType<Managers>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }

        GameObject go = GameObject.Find("@App");
        if (go == null)
            go = new GameObject("@App");

        Managers managers = go.GetComponent<Managers>();
        if (managers == null)
            managers = go.AddComponent<Managers>();

        _instance = managers;

        Instantiate(Resources.Load<GameObject>("Prefabs/UIs/EventSystem"));
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        Game.Init();
        UI.Init();
    }

    public static void Clear()
    {
        Game.Clear();
        UI.Clear();

        GameObject go = GameObject.Find("@App");
        Destroy(go);
    }
}