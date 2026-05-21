using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class App
{
    private static GameObject _App { get; set; }

    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        try
        {
            _App = Object.Instantiate(Resources.Load("ConnectionManager") as GameObject);
            _App.name = "ConnectionManager";

            Object.DontDestroyOnLoad(_App);
        }
        catch
        {
            throw new ApplicationException("Connection Manager is not found");
        }
    }

    public static T GetModule<T>()
    {
        return _App ? _App.GetComponentInChildren<T>() : default;
    }
}