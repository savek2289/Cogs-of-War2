using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddingModules : MonoBehaviour
{
    public static AddingModules Instance { get; private set; }

    [SerializeField] private List<GameObject> allModules;
    [SerializeField] private List<GameObject> parents;

    private Dictionary<string, GameObject> activeModules = new();
    private Dictionary<string, GameObject> moduleParents = new();

    private void Awake()
    {
        Instance = this;
        InitializeModuleParents();
    }

    private void InitializeModuleParents()
    {
        // Здесь должна быть ваша существующая инициализация словарей
        // (как в вашем исходном коде)
        activeModules.Add("Body", null);
        activeModules.Add("Elbow", null);
        activeModules.Add("Forearm", null);
        activeModules.Add("Brush", null);
        activeModules.Add("Pelvis", null);
        activeModules.Add("Foot", null);
        activeModules.Add("Calf", null);
        activeModules.Add("Hip", null);

        moduleParents.Add("Body", parents[0]);
        moduleParents.Add("RightElbow", parents[1]);
        moduleParents.Add("LeftElbow", parents[2]);
        moduleParents.Add("RightForearm", parents[2]);
        moduleParents.Add("LeftForearm", parents[3]);
        moduleParents.Add("RightBrush", parents[4]);
        moduleParents.Add("LeftBrush", parents[5]);
        moduleParents.Add("Pelvis", parents[6]);
        moduleParents.Add("RightFoot", parents[7]);
        moduleParents.Add("LeftFoot", parents[8]);
        moduleParents.Add("RightCalf", parents[9]);
        moduleParents.Add("LeftCalf", parents[10]);
        moduleParents.Add("RightHip", parents[11]);
        moduleParents.Add("LeftHip", parents[12]);
    }

    // Ваш существующий метод (исправлен)
    public void SetModule(UIModule uiModuleScript)
    {
        if (uiModuleScript == null) return;

        GameObject module = uiModuleScript.GetModule();
        string category = uiModuleScript.GetCategoryName();

        if (!moduleParents.TryGetValue(category, out GameObject parent))
        {
            Debug.LogWarning($"Category {category} not found in moduleParents!");
            return;
        }

        // Удаляем существующий дочерний объект
        if (parent.transform.childCount > 0)
            DestroyImmediate(parent.transform.GetChild(0).gameObject);

        // Всегда удаляем старый ключ из словаря, даже если module == null
        activeModules.Remove(category);

        if (module != null)
        {
            activeModules[category] = module; // безопасное присваивание
            Instantiate(module, parent.transform);
        }
    }

    // Новый метод для восстановления по префабу (без UIModule)
    public void SetModuleByPrefab(string category, GameObject modulePrefab)
    {
        if (string.IsNullOrEmpty(category) || modulePrefab == null) return;
        if (!moduleParents.TryGetValue(category, out GameObject parent)) return;

        if (parent.transform.childCount > 0)
            DestroyImmediate(parent.transform.GetChild(0).gameObject);

        activeModules.Remove(category);
        activeModules[category] = modulePrefab;
        Instantiate(modulePrefab, parent.transform);
    }

    // Очистка всех визуальных модулей (исправлена)
    private void ClearAllVisuals()
    {
        // Создаём копию ключей, чтобы безопасно итерировать
        List<string> categories = new List<string>(activeModules.Keys);
        foreach (string category in categories)
        {
            if (moduleParents.TryGetValue(category, out GameObject parent) && parent != null)
            {
                if (parent.transform.childCount > 0)
                    DestroyImmediate(parent.transform.GetChild(0).gameObject);
            }
            activeModules.Remove(category);
        }

        // Дополнительная очистка для категорий, где child есть, но ключ отсутствует
        foreach (var kv in moduleParents)
        {
            if (kv.Value != null && kv.Value.transform.childCount > 0)
                DestroyImmediate(kv.Value.transform.GetChild(0).gameObject);
        }
    }

    // Восстановление из сохранения
    public void RestoreActiveModules(List<SaveData.ActiveModule> snapshot)
    {
        ClearAllVisuals();

        foreach (var entry in snapshot)
        {
            GameObject modulePrefab = FindModulePrefabByName(entry.moduleName);
            if (modulePrefab != null)
                SetModuleByPrefab(entry.categoryName, modulePrefab);
            else
                Debug.LogWarning($"Module prefab {entry.moduleName} not found in allModules list.");
        }
    }

    private GameObject FindModulePrefabByName(string moduleName)
    {
        return allModules.Find(m => m != null && m.name == moduleName);
    }

    // Снимок текущих активных модулей для сохранения
    public List<SaveData.ActiveModule> GetActiveModulesSnapshot()
    {
        List<SaveData.ActiveModule> snapshot = new List<SaveData.ActiveModule>();
        foreach (var kv in activeModules)
        {
            if (kv.Value != null)
            {
                snapshot.Add(new SaveData.ActiveModule
                {
                    categoryName = kv.Key,
                    moduleName = kv.Value.name
                });
            }
        }
        return snapshot;
    }
}