using System.IO;
using UnityEngine;

public static class JsonManager
{
    public static void SaveObjectToJson<T>(T data, string fileName)
    {
        if (data == null)
        {
            Debug.LogError("Kaydedilecek veri null!");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        string path = GetFilePath(fileName);
        File.WriteAllText(path, json);

        Debug.Log($"Veri JSON olarak kaydedildi: {path}");
    }

    public static T LoadObjectFromJson<T>(string fileName)
    {
        string path = GetFilePath(fileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            T data = JsonUtility.FromJson<T>(json);
            Debug.Log($"Veri JSON'dan yüklendi: {path}");
            return data;
        }
        else
        {
            Debug.LogError($"JSON dosyasý bulunamadý: {path}");
            return default;
        }
    }
    private static string GetFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
