using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacteristics : MonoBehaviour
{
    public static PlayerCharacteristics Instance { get; private set; }

    [System.Serializable]
    public class Characteristic
    {
        [SerializeField] private string name;
        [SerializeField] private float baseValue;
        [SerializeField] private float maxValue;
        [Space(10)]
        [SerializeField] private Slider whiteSlider;
        [SerializeField] private Slider colorSlider;
        [SerializeField] private TextMeshProUGUI value;

        public string Name => name;
        public float BaseValue => baseValue;
        public float MaxValue => maxValue;
        public Slider WhiteSlider => whiteSlider;
        public Slider ColorSlider => colorSlider;
        public TextMeshProUGUI Value => value;
    }

    [System.Serializable]
    public class ModuleCategory
    {
        [SerializeField] private string categoryName;
        [SerializeField] private List<UIModule> modules;
        [SerializeField] private GameObject categoryCelectFrame;

        public string CategoryName => categoryName;
        public List<UIModule> Modules => modules;
        public GameObject CelectFrame => categoryCelectFrame;
    }

    [SerializeField] private List<Characteristic> characteristicsData;
    [Space(10)]
    [SerializeField] private List<ModuleCategory> allCategories;

    // Данные
    private Dictionary<string, Characteristic> characteristicsMap;
    private Dictionary<string, float> baseCharacteristicValues;
    private Dictionary<string, float> confirmedValues;     // финальные
    private Dictionary<string, float> previewValues;       // целевые значения предпросмотра (для текста и для одного из слайдеров)
    private Dictionary<string, float> currentDisplayValues; // текущие значения для анимации текста

    // Управление модулями
    private List<UIModule> allModulesList;
    private int? selectedModuleIndex = null;
    private ModuleCategory lastSelectedCategory = null;
    private Dictionary<ModuleCategory, UIModule> selectedModulesByCategory;

    // Плавность
    private Coroutine currentAnimation;

    private void Awake()
    {
        Instance = this;

        if (characteristicsData == null) return;

        characteristicsMap = new Dictionary<string, Characteristic>();
        baseCharacteristicValues = new Dictionary<string, float>();
        confirmedValues = new Dictionary<string, float>();
        previewValues = new Dictionary<string, float>();
        currentDisplayValues = new Dictionary<string, float>();

        foreach (var ch in characteristicsData)
        {
            if (ch == null) continue;
            if (!characteristicsMap.ContainsKey(ch.Name))
                characteristicsMap.Add(ch.Name, ch);
            else
                Debug.LogWarning($"Duplicate characteristic name: {ch.Name}");
        }

        allModulesList = new List<UIModule>();
        foreach (var cat in allCategories)
            foreach (var mod in cat.Modules)
                if (mod != null && !allModulesList.Contains(mod))
                    allModulesList.Add(mod);

        // Инициализация
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float baseVal = ch.BaseValue;
            baseCharacteristicValues[name] = baseVal;
            confirmedValues[name] = baseVal;
            previewValues[name] = baseVal;
            currentDisplayValues[name] = baseVal;
            ch.WhiteSlider.maxValue = ch.MaxValue;
            ch.WhiteSlider.value = baseVal;
            ch.ColorSlider.maxValue = ch.MaxValue;
            ch.ColorSlider.value = baseVal;
            ch.Value.text = baseVal.ToString("0.#");
            ch.Value.color = Color.white;
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }

        HideAllFrames();
        selectedModulesByCategory = new Dictionary<ModuleCategory, UIModule>();
    }

    private void Start()
    {
        if (SaveSystem.HasSaveFile())
        {
            SaveSystem.LoadGame(this, AddingModules.Instance);
        }
    }

    private void HideAllFrames()
    {
        foreach (var cat in allCategories)
            if (cat.CelectFrame != null)
                cat.CelectFrame.SetActive(false);
    }

    // ================== Предпросмотр ==================

    public void PreviewModule(UIModule newModule)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Определяем категорию
        ModuleCategory newCategory = null;
        foreach (var cat in allCategories)
        {
            if (cat.Modules.Contains(newModule))
            {
                newCategory = cat;
                break;
            }
        }
        if (newCategory == null)
        {
            Debug.LogWarning($"Модуль {newModule.name} не принадлежит ни одной категории!");
            return;
        }

        // Получаем старый модуль в этой категории
        selectedModulesByCategory.TryGetValue(newCategory, out UIModule oldModule);

        // Строим временные бонусы
        var tempBonuses = new Dictionary<string, Dictionary<string, float>>();

        foreach (var kv in selectedModulesByCategory)
        {
            ModuleCategory cat = kv.Key;
            UIModule mod = kv.Value;
            if (cat == newCategory) continue;
            if (mod == null || mod.CancelCategory) continue;

            var bonuses = new Dictionary<string, float>();
            foreach (var val in mod.GetValues())
            {
                if (!characteristicsMap.ContainsKey(val.Name)) continue;
                bonuses[val.Name] = val.AddedValue;
            }
            tempBonuses[cat.CategoryName] = bonuses;
        }

        // Добавляем новый модуль (если не cancel)
        if (!newModule.CancelCategory)
        {
            var newBonuses = new Dictionary<string, float>();
            foreach (var val in newModule.GetValues())
            {
                if (!characteristicsMap.ContainsKey(val.Name)) continue;
                newBonuses[val.Name] = val.AddedValue;
            }
            tempBonuses[newCategory.CategoryName] = newBonuses;
        }

        // Вычисляем preview значения
        foreach (var pair in baseCharacteristicValues)
            previewValues[pair.Key] = pair.Value;

        foreach (var catBonuses in tempBonuses)
        {
            foreach (var bonus in catBonuses.Value)
            {
                string charName = bonus.Key;
                previewValues[charName] = Mathf.Max(0, previewValues[charName] + bonus.Value);
            }
        }

        // Ограничиваем максимумом
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            previewValues[name] = Mathf.Clamp(previewValues[name], 0, pair.Value.MaxValue);
        }

        // Формируем цели для белого, цветного слайдеров и текста
        var whiteTargets = new Dictionary<string, float>();
        var colorTargets = new Dictionary<string, float>();
        var textTargets = new Dictionary<string, float>();  // всегда preview
        var textColors = new Dictionary<string, Color>();
        var sliderColors = new Dictionary<string, Color>();

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            float confirmed = confirmedValues[name];
            float preview = previewValues[name];

            textTargets[name] = preview;  // текст всегда показывает preview

            if (preview > confirmed + 0.001f) // увеличение
            {
                whiteTargets[name] = confirmed;       // белый остаётся
                colorTargets[name] = preview;         // цветной идёт вверх
                textColors[name] = Color.green;
                sliderColors[name] = Color.green;
            }
            else if (preview < confirmed - 0.001f) // уменьшение
            {
                whiteTargets[name] = preview;         // белый идёт вниз
                colorTargets[name] = confirmed;       // цветной остаётся
                textColors[name] = Color.red;
                sliderColors[name] = Color.red;
            }
            else // равно
            {
                whiteTargets[name] = confirmed;
                colorTargets[name] = confirmed;
                textColors[name] = Color.white;
                sliderColors[name] = Color.white;
            }
        }

        // Запускаем анимацию
        currentAnimation = StartCoroutine(AnimatePreview(whiteTargets, colorTargets, textTargets, textColors, sliderColors, 0.3f));
    }

    // ================== Подтверждение ==================

    public void ApplyChanges(UIModule module)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Обновляем словарь подтверждённых модулей
        ModuleCategory category = null;
        foreach (var cat in allCategories)
        {
            if (cat.Modules.Contains(module))
            {
                category = cat;
                break;
            }
        }
        if (category != null)
        {
            if (selectedModulesByCategory.ContainsKey(category))
                selectedModulesByCategory[category] = module;
            else
                selectedModulesByCategory.Add(category, module);
        }

        // Пересчитываем confirmed значения
        foreach (var pair in baseCharacteristicValues)
            confirmedValues[pair.Key] = pair.Value;

        foreach (var kv in selectedModulesByCategory)
        {
            UIModule mod = kv.Value;
            if (mod == null || mod.CancelCategory) continue;
            foreach (var val in mod.GetValues())
            {
                if (!characteristicsMap.ContainsKey(val.Name)) continue;
                confirmedValues[val.Name] += val.AddedValue;
            }
        }

        // Ограничиваем максимумом
        foreach (var pair in characteristicsMap)
            confirmedValues[pair.Key] = Mathf.Clamp(confirmedValues[pair.Key], 0, pair.Value.MaxValue);

        // Копируем preview
        foreach (var pair in confirmedValues)
            previewValues[pair.Key] = pair.Value;

        // Анимируем оба слайдера к confirmed, цветной становится белым
        var whiteColors = new Dictionary<string, Color>();
        foreach (var pair in characteristicsMap)
            whiteColors[pair.Key] = Color.white;

        currentAnimation = StartCoroutine(AnimateConfirm(confirmedValues, whiteColors, 0.5f));

        Debug.Log("Характеристики подтверждены: " + string.Join(", ", confirmedValues));
    }

    // ================== Отмена предпросмотра ==================

    public void CancelPreview()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Возвращаем preview к confirmed
        foreach (var pair in confirmedValues)
            previewValues[pair.Key] = pair.Value;

        // Анимируем оба слайдера к confirmed, цветной становится белым
        var whiteColors = new Dictionary<string, Color>();
        foreach (var pair in characteristicsMap)
            whiteColors[pair.Key] = Color.white;

        currentAnimation = StartCoroutine(AnimateCancel(confirmedValues, whiteColors, 0.3f));
    }

    // ================== Анимации ==================

    /// <summary> Анимация предпросмотра: разные цели для белого, цветного слайдеров и текста </summary>
    private IEnumerator AnimatePreview(Dictionary<string, float> whiteTargets, Dictionary<string, float> colorTargets,
                                        Dictionary<string, float> textTargets, Dictionary<string, Color> textColors,
                                        Dictionary<string, Color> sliderColors, float duration)
    {
        // Начальные значения
        var startTextValues = new Dictionary<string, float>(currentDisplayValues);
        var startTextColors = new Dictionary<string, Color>();
        var startWhiteSliders = new Dictionary<string, float>();
        var startColorSliders = new Dictionary<string, float>();
        var startColorSliderColors = new Dictionary<string, Color>();
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            startTextColors[name] = ch.Value.color;
            startWhiteSliders[name] = ch.WhiteSlider.value;
            startColorSliders[name] = ch.ColorSlider.value;
            startColorSliderColors[name] = ch.ColorSlider.fillRect.GetComponent<Image>().color;
        }

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            foreach (var pair in characteristicsMap)
            {
                string name = pair.Key;
                Characteristic ch = pair.Value;

                // Текст – анимируем к textTargets
                float newTextValue = Mathf.Lerp(startTextValues[name], textTargets[name], t);
                currentDisplayValues[name] = newTextValue;
                ch.Value.text = newTextValue.ToString("0.#");
                ch.Value.color = Color.Lerp(startTextColors[name], textColors[name], t);

                // Белый слайдер
                float newWhite = Mathf.Lerp(startWhiteSliders[name], whiteTargets[name], t);
                ch.WhiteSlider.value = newWhite;

                // Цветной слайдер
                float newColor = Mathf.Lerp(startColorSliders[name], colorTargets[name], t);
                ch.ColorSlider.value = newColor;
                ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.Lerp(startColorSliderColors[name], sliderColors[name], t);
            }
            yield return null;
        }

        // Финал
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            currentDisplayValues[name] = textTargets[name];
            ch.Value.text = textTargets[name].ToString("0.#");
            ch.Value.color = textColors[name];
            ch.WhiteSlider.value = whiteTargets[name];
            ch.ColorSlider.value = colorTargets[name];
            ch.ColorSlider.fillRect.GetComponent<Image>().color = sliderColors[name];
        }
        currentAnimation = null;
    }

    /// <summary> Анимация подтверждения: оба слайдера к confirmed, цветной белеет </summary>
    private IEnumerator AnimateConfirm(Dictionary<string, float> targetValues, Dictionary<string, Color> targetColors, float duration)
    {
        // Начальные значения
        var startTextValues = new Dictionary<string, float>(currentDisplayValues);
        var startTextColors = new Dictionary<string, Color>();
        var startWhiteSliders = new Dictionary<string, float>();
        var startColorSliders = new Dictionary<string, float>();
        var startColorSliderColors = new Dictionary<string, Color>();
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            startTextColors[name] = ch.Value.color;
            startWhiteSliders[name] = ch.WhiteSlider.value;
            startColorSliders[name] = ch.ColorSlider.value;
            startColorSliderColors[name] = ch.ColorSlider.fillRect.GetComponent<Image>().color;
        }

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            foreach (var pair in characteristicsMap)
            {
                string name = pair.Key;
                Characteristic ch = pair.Value;

                float newTextValue = Mathf.Lerp(startTextValues[name], targetValues[name], t);
                currentDisplayValues[name] = newTextValue;
                ch.Value.text = newTextValue.ToString("0.#");
                ch.Value.color = Color.Lerp(startTextColors[name], targetColors[name], t);

                float newWhite = Mathf.Lerp(startWhiteSliders[name], targetValues[name], t);
                ch.WhiteSlider.value = newWhite;

                float newColor = Mathf.Lerp(startColorSliders[name], targetValues[name], t);
                ch.ColorSlider.value = newColor;
                ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.Lerp(startColorSliderColors[name], targetColors[name], t);
            }
            yield return null;
        }

        // Финал
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            currentDisplayValues[name] = targetValues[name];
            ch.Value.text = targetValues[name].ToString("0.#");
            ch.Value.color = targetColors[name];
            ch.WhiteSlider.value = targetValues[name];
            ch.ColorSlider.value = targetValues[name];
            ch.ColorSlider.fillRect.GetComponent<Image>().color = targetColors[name];
        }
        currentAnimation = null;
    }

    /// <summary> Анимация отмены: оба слайдера к confirmed, цветной белеет </summary>
    private IEnumerator AnimateCancel(Dictionary<string, float> targetValues, Dictionary<string, Color> targetColors, float duration)
    {
        yield return StartCoroutine(AnimateConfirm(targetValues, targetColors, duration));
    }

    // ================== Управление рамками и кнопками ==================

    public void SetCelectedModule(ModuleButton targetModule)
    {
        if (targetModule == null || !targetModule.TryGetComponent<UIModule>(out UIModule module))
            return;

        ModuleCategory category = null;
        foreach (var cat in allCategories)
        {
            if (cat.Modules.Contains(module))
            {
                category = cat;
                break;
            }
        }

        if (category == null)
        {
            Debug.LogWarning("Модуль не принадлежит ни одной категории!");
            return;
        }

        if (category.CelectFrame == null)
        {
            Debug.LogWarning($"У категории {category.CategoryName} нет рамки выделения");
            return;
        }

        if (lastSelectedCategory != null && lastSelectedCategory.CelectFrame != null)
            lastSelectedCategory.CelectFrame.SetActive(false);
        category.CelectFrame.SetActive(true);
        RectTransform moduleRect = targetModule.GetComponent<RectTransform>();
        RectTransform frameRect = category.CelectFrame.GetComponent<RectTransform>();
        frameRect.anchoredPosition = new Vector2(moduleRect.anchoredPosition.x, moduleRect.anchoredPosition.y - 1);

        if (selectedModuleIndex != null && selectedModuleIndex.Value < allModulesList.Count)
        {
            var prevModule = allModulesList[selectedModuleIndex.Value];
            if (prevModule != null && prevModule.TryGetComponent<ModuleButton>(out ModuleButton prevBtn))
                prevBtn.enabled = true;
        }

        targetModule.enabled = false;
        selectedModuleIndex = allModulesList.IndexOf(module);
        lastSelectedCategory = category;
    }

    public void ClearSelection()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        selectedModulesByCategory.Clear();
        selectedModuleIndex = null;
        lastSelectedCategory = null;

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            float baseVal = baseCharacteristicValues[name];
            confirmedValues[name] = baseVal;
            previewValues[name] = baseVal;
            currentDisplayValues[name] = baseVal;
            Characteristic ch = pair.Value;
            ch.WhiteSlider.value = baseVal;
            ch.ColorSlider.value = baseVal;
            ch.Value.text = baseVal.ToString("0.#");
            ch.Value.color = Color.white;
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }

        HideAllFrames();
    }

    // ================== Методы для мгновенного получения сохранённых характеристик ==================

    public float GetConfirmedValue(string characteristicName)
    {
        if (confirmedValues != null && confirmedValues.ContainsKey(characteristicName))
            return confirmedValues[characteristicName];
        Debug.LogWarning($"Характеристика {characteristicName} не найдена");
        return 0;
    }

    public Dictionary<string, float> GetAllConfirmedValues()
    {
        if (confirmedValues == null)
            return new Dictionary<string, float>();
        return new Dictionary<string, float>(confirmedValues);
    }

    // ================== Сохранение и загрузка ==================

    /// <summary>
    /// Формирует объект SaveData из текущего состояния.
    /// </summary>
    public SaveData GetSaveData()
    {
        SaveData data = new SaveData();

        // Сохраняем подтверждённые значения характеристик
        foreach (var kv in confirmedValues)
        {
            data.characteristicValues.Add(new SaveData.CharValuePair { name = kv.Key, value = kv.Value });
        }

        // Сохраняем выбранные модули по категориям
        foreach (var kv in selectedModulesByCategory)
        {
            ModuleCategory category = kv.Key;
            UIModule module = kv.Value;
            if (module != null)
            {
                data.selectedModules.Add(new SaveData.ModulePair
                {
                    categoryName = category.CategoryName,
                    moduleName = module.name
                });
            }
        }

        return data;
    }

    /// <summary>
    /// Восстанавливает состояние из объекта SaveData.
    /// </summary>
    public void LoadFromSaveData(SaveData data)
    {
        // Останавливаем текущие анимации
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Сбрасываем текущий выбор
        selectedModulesByCategory.Clear();
        selectedModuleIndex = null;
        lastSelectedCategory = null;

        // Восстанавливаем подтверждённые значения характеристик
        foreach (var pair in data.characteristicValues)
        {
            if (characteristicsMap.ContainsKey(pair.name))
            {
                confirmedValues[pair.name] = Mathf.Clamp(pair.value, 0, characteristicsMap[pair.name].MaxValue);
                previewValues[pair.name] = confirmedValues[pair.name];
            }
            else
            {
                Debug.LogWarning($"Characteristic {pair.name} not found in current game data.");
            }
        }

        // Восстанавливаем выбранные модули
        foreach (var modPair in data.selectedModules)
        {
            ModuleCategory category = FindCategoryByName(modPair.categoryName);
            if (category != null)
            {
                UIModule module = FindModuleInCategory(category, modPair.moduleName);
                if (module != null)
                {
                    selectedModulesByCategory[category] = module;
                }
                else
                {
                    Debug.LogWarning($"Module '{modPair.moduleName}' not found in category '{modPair.categoryName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Category '{modPair.categoryName}' not found.");
            }
        }

        // Пересчитываем confirmedValues на основе загруженных модулей (на случай несоответствия)
        RecalculateConfirmedFromModules();

        // Синхронизируем preview с confirmed
        foreach (var pair in confirmedValues)
            previewValues[pair.Key] = pair.Value;

        // Обновляем UI (слайдеры, текст)
        UpdateUIImmediate();

        // Скрываем все рамки – они будут активированы при первом клике на модуль
        HideAllFrames();

        // Включаем все кнопки модулей (чтобы сбросить блокировку)
        foreach (var mod in allModulesList)
        {
            if (mod != null && mod.TryGetComponent<ModuleButton>(out var btn))
                btn.enabled = true;
        }

        // Если после загрузки есть выбранные модули, можно обновить рамку для последнего выбранного (опционально)
        if (selectedModulesByCategory.Count > 0)
        {
            // Возьмём первый попавшийся модуль и установим рамку (можно усовершенствовать)
            foreach (var kv in selectedModulesByCategory)
            {
                UIModule mod = kv.Value;
                if (mod != null && mod.TryGetComponent<ModuleButton>(out var btn))
                {
                    SetCelectedModule(btn);
                    break;
                }
            }
        }

        Debug.Log("Data loaded from save.");
    }

    private ModuleCategory FindCategoryByName(string categoryName)
    {
        foreach (var cat in allCategories)
        {
            if (cat.CategoryName == categoryName)
                return cat;
        }
        return null;
    }

    private UIModule FindModuleInCategory(ModuleCategory category, string moduleName)
    {
        foreach (var mod in category.Modules)
        {
            if (mod != null && mod.name == moduleName)
                return mod;
        }
        return null;
    }

    private void RecalculateConfirmedFromModules()
    {
        // Сбрасываем на базовые значения
        foreach (var pair in baseCharacteristicValues)
            confirmedValues[pair.Key] = pair.Value;

        // Суммируем бонусы выбранных модулей
        foreach (var kv in selectedModulesByCategory)
        {
            UIModule mod = kv.Value;
            if (mod == null || mod.CancelCategory) continue;
            foreach (var val in mod.GetValues())
            {
                if (!characteristicsMap.ContainsKey(val.Name)) continue;
                confirmedValues[val.Name] += val.AddedValue;
            }
        }

        // Ограничиваем максимумом
        foreach (var pair in characteristicsMap)
            confirmedValues[pair.Key] = Mathf.Clamp(confirmedValues[pair.Key], 0, pair.Value.MaxValue);
    }

    // ================== Вспомогательные методы ==================

    public float GetCurrentValue(string characteristicName)
    {
        if (confirmedValues != null && confirmedValues.ContainsKey(characteristicName))
            return confirmedValues[characteristicName];
        Debug.LogWarning($"Характеристика {characteristicName} не найдена");
        return 0;
    }

    public bool HasCharacteristic(string name)
    {
        return characteristicsMap != null && characteristicsMap.ContainsKey(name);
    }

    public void LoadStatsOnly(SaveData data)
    {
        // Останавливаем анимации
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Восстанавливаем значения характеристик
        foreach (var pair in data.characteristicValues)
        {
            if (characteristicsMap.ContainsKey(pair.name))
            {
                confirmedValues[pair.name] = Mathf.Clamp(pair.value, 0, characteristicsMap[pair.name].MaxValue);
                previewValues[pair.name] = confirmedValues[pair.name];
            }
        }

        // Пересчитываем confirmedValues (на случай, если модули тоже изменились)
        // Но если мы загружаем только статистику, возможно, не нужно пересчитывать.
        // Оставляем как есть.

        // Синхронизируем preview с confirmed
        foreach (var pair in confirmedValues)
            previewValues[pair.Key] = pair.Value;

        // Обновляем UI
        UpdateUIImmediate(); // реализуйте этот метод (он просто обновляет слайдеры и текст)

        // Сбрасываем рамки и кнопки, но не трогаем визуальные модули
        HideAllFrames();
        foreach (var mod in allModulesList)
            if (mod != null && mod.TryGetComponent<ModuleButton>(out var btn))
                btn.enabled = true;
    }

    public void SaveGame()
    {
        if (PlayerCharacteristics.Instance != null)
            SaveSystem.SaveGame(PlayerCharacteristics.Instance, AddingModules.Instance);
        else
            Debug.LogError("PlayerCharacteristics.Instance is null!");
    }

    // Заглушки для совместимости
    public void StopAllPreviews() { if (currentAnimation != null) StopCoroutine(currentAnimation); }
    public void SetToPreviewTargets() { }
    public void SetChanges(string parameterName, int value) { }
    public void SetNeedToReset(bool value) { }
    public void RevertToCurrent() { }

    public IEnumerator ApplyChangesCoroutine()
    {
        yield break;
    }

    /// <summary>
    /// Мгновенно обновляет UI (слайдеры, текст, цвета) до подтверждённых значений без анимации.
    /// </summary>
    private void UpdateUIImmediate()
    {
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float confirmed = confirmedValues[name];
            ch.WhiteSlider.value = confirmed;
            ch.ColorSlider.value = confirmed;
            ch.Value.text = confirmed.ToString("0.#");
            ch.Value.color = Color.white;
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }
    }
}