using System.IO;
using UnityEngine;

public class ScriptableObjectToJson : MonoBehaviour
{
    [Header("Kaydedilecek Scriptable Object")]
    public EnemyStats enemyStats;

    [Header("Kaydetme ve Yükleme Ayarlarý")]
    public string fileName = "EnemyStats.json";
    public void SaveToJson()
    {
        if (enemyStats == null)
        {
            Debug.LogError("EnemyStats referansý atanmadý!");
            return;
        }

        string json = JsonUtility.ToJson(enemyStats, true);

        string path = GetFilePath();
        File.WriteAllText(path, json);

        Debug.Log($"JSON olarak kaydedildi: {path}");
    }

    public void LoadFromJson()
    {
        if (enemyStats == null)
        {
            enemyStats = ScriptableObject.CreateInstance<EnemyStats>();
        }

        string path = GetFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, enemyStats);
            Debug.Log($"JSON'dan yüklendi: {path}");
        }
        else
        {
            Debug.LogError($"JSON dosyasý bulunamadý: {path}");
        }
    }

    private string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
