using System.IO;
using UnityEngine;

public class FilePayoutMethodsRepository : IPayoutMethodsRepository
{
    private readonly string _path;

    public FilePayoutMethodsRepository(string fileName = "payout_methods.json")
    {
        _path = Path.Combine(Application.persistentDataPath, fileName);
    }

    public PayoutMethodsState Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var json = File.ReadAllText(_path);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<PayoutMethodsState>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(PayoutMethodsState state)
    {
        try
        {
            var json = JsonUtility.ToJson(state, prettyPrint: true);
            File.WriteAllText(_path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Save payout methods failed: {e.Message}");
        }
    }
}
