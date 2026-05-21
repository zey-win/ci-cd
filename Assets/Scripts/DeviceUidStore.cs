using System;
using System.IO;
using UnityEngine;

public static class DeviceUidStore
{
    [Serializable]
    private class Payload
    {
        public string uid;
        public long created_unix;
    }

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "device_uid.json");

    public static string GetOrCreate()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var p = JsonUtility.FromJson<Payload>(json);
                if (p != null && !string.IsNullOrEmpty(p.uid))
                    return p.uid;
            }

            var payload = new Payload
            {
                uid = Guid.NewGuid().ToString("N"),
                created_unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var outJson = JsonUtility.ToJson(payload);

            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, outJson);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);

            return payload.uid;
        }
        catch (Exception e)
        {
            Debug.LogWarning("DeviceUidStore failed, fallback to runtime uid: " + e);
            return Guid.NewGuid().ToString("N");
        }
    }
}
