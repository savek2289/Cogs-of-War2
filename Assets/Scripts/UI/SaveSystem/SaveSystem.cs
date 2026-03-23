using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "player_save.json");

    /// <summary>
    /// Сохраняет текущее состояние игрока в файл.
    /// </summary>
    public static void SaveGame(PlayerCharacteristics player)
    {
        if (player == null)
        {
            Debug.LogError("PlayerCharacteristics instance is null, cannot save.");
            return;
        }

        SaveData data = player.GetSaveData();
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"Game saved to {SaveFilePath}");
    }

    /// <summary>
    /// Загружает состояние игрока из файла.
    /// </summary>
    public static void LoadGame(PlayerCharacteristics player)
    {
        if (player == null)
        {
            Debug.LogError("PlayerCharacteristics instance is null, cannot load.");
            return;
        }

        if (!HasSaveFile())
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        player.LoadFromSaveData(data);
        Debug.Log("Game loaded.");
    }

    /// <summary>
    /// Проверяет наличие сохранения.
    /// </summary>
    public static bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// Удаляет файл сохранения.
    /// </summary>
    public static void DeleteSave()
    {
        if (HasSaveFile())
        {
            File.Delete(SaveFilePath);
            Debug.Log("Save file deleted.");
        }
    }

    public static void SaveGame(PlayerCharacteristics player, AddingModules visuals)
    {
        if (player == null) return;
        SaveData data = player.GetSaveData(); // предполагается, что он уже есть
        // Добавляем активные модули из AddingModules
        if (visuals != null)
            data.activeModules = visuals.GetActiveModulesSnapshot();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"Game saved to {SaveFilePath}");
    }

    // НОВЫЙ метод: загружает только характеристики (без визуала)
    public static void LoadGameStatsOnly(PlayerCharacteristics player)
    {
        if (!HasSaveFile() || player == null) return;
        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        player.LoadStatsOnly(data); // нужно добавить этот метод в PlayerCharacteristics
    }

    // НОВЫЙ метод: загружает только визуальные модули (без характеристик)
    public static void LoadGameVisualsOnly(PlayerCharacteristics player, AddingModules visuals)
    {
        if (!HasSaveFile() || visuals == null) return;
        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        visuals.RestoreActiveModules(data.activeModules);
    }

    // Полная загрузка (характеристики + визуалы)
    public static void LoadGame(PlayerCharacteristics player, AddingModules visuals)
    {
        if (!HasSaveFile() || player == null) return;
        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        player.LoadStatsOnly(data);            // загружаем характеристики
        if (visuals != null)
            visuals.RestoreActiveModules(data.activeModules); // загружаем визуалы
    }
}


[System.Serializable]
public class SaveData
{
    [System.Serializable]
    public class CharValuePair
    {
        public string name;
        public float value;
    }

    [System.Serializable]
    public class ModulePair
    {
        public string categoryName;
        public string moduleName; // имя модуля (gameObject.name)
    }

    [System.Serializable]
    public class ActiveModule
    {
        public string categoryName;  // имя категории (как в HandleCatalog.ModuleCategories)
        public string moduleName;    // имя префаба модуля (gameObject.name)
    }

    public List<CharValuePair> characteristicValues = new List<CharValuePair>();

    public List<ModulePair> selectedModules = new List<ModulePair>();

    public List<ActiveModule> activeModules = new List<ActiveModule>();
}