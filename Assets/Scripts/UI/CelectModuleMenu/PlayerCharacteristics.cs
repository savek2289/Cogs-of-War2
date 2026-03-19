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
        [SerializeField] private float baseValue; // изменено на float
        [Space(10)]
        [SerializeField] private Slider whiteSlider;
        [SerializeField] private Slider colorSlider;
        [SerializeField] private TextMeshProUGUI value;

        public string Name => name;
        public float BaseValue => baseValue;
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

    private Dictionary<string, Characteristic> characteristicsMap;
    private Dictionary<string, float> currentCharacteristicValues; // подтверждённые значения (float)
    private Dictionary<string, float> baseCharacteristicValues;
    private Dictionary<string, float> maxCharacteristicValues; // максимальные значения (float)

    private List<UIModule> allModulesList;
    private int? selectedModuleIndex = null;          // индекс последнего подтверждённого модуля (для кнопок)
    private ModuleCategory lastSelectedCategory = null;

    // Словарь выбранных модулей по категориям
    private Dictionary<ModuleCategory, UIModule> selectedModulesByCategory;

    // Последние рассчитанные цели для предпросмотра
    private Dictionary<string, (float targetWhite, float targetColor)> lastPreviewTargets;

    private bool needToReset = false;   // не используется в новой логике, оставлено для совместимости
    private Coroutine revertCoroutine = null;
    private bool isReverting = false;

    private void Awake()
    {
        Instance = this;

        if (characteristicsData == null) return;

        characteristicsMap = new Dictionary<string, Characteristic>();
        currentCharacteristicValues = new Dictionary<string, float>();
        baseCharacteristicValues = new Dictionary<string, float>();
        maxCharacteristicValues = new Dictionary<string, float>();

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

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float baseVal = ch.BaseValue;
            baseCharacteristicValues[name] = baseVal;
            currentCharacteristicValues[name] = baseVal;
        }

        // Проверка синхронизации
        foreach (var key in characteristicsMap.Keys)
        {
            if (!baseCharacteristicValues.ContainsKey(key))
                Debug.LogError($"Характеристика '{key}' отсутствует в baseCharacteristicValues!");
        }

        maxCharacteristicValues = ComputeMaxCharacteristicValues();

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float baseVal = baseCharacteristicValues[name];
            float maxVal = maxCharacteristicValues[name];

            ch.WhiteSlider.maxValue = maxVal;
            ch.ColorSlider.maxValue = maxVal;
            ch.WhiteSlider.value = baseVal;
            ch.ColorSlider.value = baseVal;
            ch.Value.text = Mathf.RoundToInt(baseVal).ToString();
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }

        HideAllFrames();

        // Инициализация словарей
        selectedModulesByCategory = new Dictionary<ModuleCategory, UIModule>();
        lastPreviewTargets = new Dictionary<string, (float, float)>();
    }

    /// <summary>
    /// Вычисляет максимально возможное значение для каждой характеристики,
    /// перебирая все допустимые комбинации модулей (не более одного из категории).
    /// </summary>
    private Dictionary<string, float> ComputeMaxCharacteristicValues()
    {
        var maxValues = new Dictionary<string, float>(baseCharacteristicValues);

        var categories = new List<List<UIModule>>();
        foreach (var cat in allCategories)
        {
            var validModules = new List<UIModule>();
            foreach (var mod in cat.Modules)
            {
                if (mod != null && !mod.CancelCategory)
                    validModules.Add(mod);
            }
            validModules.Insert(0, null); // возможность не выбирать модуль
            categories.Add(validModules);
        }

        var tempBonuses = new Dictionary<string, float>();
        ExploreCombinations(0, tempBonuses, categories, maxValues);

        return maxValues;
    }

    private void ExploreCombinations(int catIndex, Dictionary<string, float> tempBonuses,
                                     List<List<UIModule>> categories, Dictionary<string, float> maxValues)
    {
        if (catIndex >= categories.Count)
        {
            foreach (var kv in tempBonuses)
            {
                string charName = kv.Key;

                if (!baseCharacteristicValues.TryGetValue(charName, out float baseVal))
                {
                    Debug.LogWarning($"Характеристика '{charName}' отсутствует в baseCharacteristicValues, используется 0.");
                    baseVal = 0;
                }

                float total = baseVal + kv.Value;

                if (maxValues.TryGetValue(charName, out float currentMax))
                {
                    if (total > currentMax)
                        maxValues[charName] = total;
                }
                else
                {
                    maxValues[charName] = total;
                }
            }
            return;
        }

        foreach (var module in categories[catIndex])
        {
            if (module != null)
            {
                foreach (var val in module.GetValues())
                {
                    string charName = val.Name;
                    if (!characteristicsMap.ContainsKey(charName)) continue;

                    float bonus = val.AddedValue; // уже float
                    if (tempBonuses.ContainsKey(charName))
                        tempBonuses[charName] += bonus;
                    else
                        tempBonuses[charName] = bonus;
                }
            }

            ExploreCombinations(catIndex + 1, tempBonuses, categories, maxValues);

            if (module != null)
            {
                foreach (var val in module.GetValues())
                {
                    string charName = val.Name;
                    if (!characteristicsMap.ContainsKey(charName)) continue;

                    float bonus = val.AddedValue;
                    tempBonuses[charName] -= bonus;
                    if (Math.Abs(tempBonuses[charName]) < 0.001f)
                        tempBonuses.Remove(charName);
                }
            }
        }
    }

    private void HideAllFrames()
    {
        foreach (var cat in allCategories)
            if (cat.CelectFrame != null)
                cat.CelectFrame.SetActive(false);
    }

    // ================== Предварительный просмотр модуля ==================

    /// <summary>
    /// Плавно переводит все характеристики в состояние предварительного просмотра указанного модуля.
    /// Учитывает все ранее выбранные модули из разных категорий.
    /// </summary>
    public void PreviewModule(UIModule newModule)
    {
        StopAllCoroutines();
        isReverting = false;
        revertCoroutine = null;

        // Определяем категорию нового модуля
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

        // Получаем старый модуль в этой категории (если есть)
        selectedModulesByCategory.TryGetValue(newCategory, out UIModule oldModule);

        // Суммируем бонусы из всех категорий, кроме текущей
        Dictionary<string, float> bonusesFromOtherCategories = new Dictionary<string, float>();
        foreach (var kv in selectedModulesByCategory)
        {
            ModuleCategory cat = kv.Key;
            UIModule mod = kv.Value;
            if (cat == newCategory) continue;
            if (mod == null || mod.CancelCategory) continue;

            foreach (var val in mod.GetValues())
            {
                string charName = val.Name;
                if (!characteristicsMap.ContainsKey(charName)) continue;
                if (bonusesFromOtherCategories.ContainsKey(charName))
                    bonusesFromOtherCategories[charName] += val.AddedValue;
                else
                    bonusesFromOtherCategories[charName] = val.AddedValue;
            }
        }

        // Целевые значения для каждой характеристики
        Dictionary<string, (float targetWhite, float targetColor)> targets =
            new Dictionary<string, (float, float)>();

        foreach (var pair in characteristicsMap)
        {
            string charName = pair.Key;
            float baseVal = currentCharacteristicValues[charName];
            float otherBonus = bonusesFromOtherCategories.ContainsKey(charName) ? bonusesFromOtherCategories[charName] : 0f;

            // Бонус старого модуля в этой категории по данной характеристике
            float oldBonus = 0f;
            if (oldModule != null && !oldModule.CancelCategory)
            {
                foreach (var val in oldModule.GetValues())
                {
                    if (val.Name == charName)
                    {
                        oldBonus = val.AddedValue;
                        break;
                    }
                }
            }

            // Бонус нового модуля по данной характеристике
            float newBonus = 0f;
            if (!newModule.CancelCategory)
            {
                foreach (var val in newModule.GetValues())
                {
                    if (val.Name == charName)
                    {
                        newBonus = val.AddedValue;
                        break;
                    }
                }
            }

            float finalValue = baseVal + otherBonus + newBonus;
            float currentWithoutNewCategory = baseVal + otherBonus + oldBonus; // то, что сейчас подтверждено (с учётом старого модуля)

            // Определяем движение слайдеров
            float tw, tc;
            if (newBonus >= oldBonus)
            {
                // Увеличение или равно: белый остаётся на старом уровне, цветной идёт к финалу
                tw = currentWithoutNewCategory;
                tc = finalValue;
            }
            else
            {
                // Уменьшение: белый идёт к финалу, цветной остаётся на старом уровне
                tw = finalValue;
                tc = currentWithoutNewCategory;
            }

            targets[charName] = (tw, tc);
        }

        // Сохраняем цели для мгновенного восстановления при подтверждении
        lastPreviewTargets.Clear();
        foreach (var kv in targets)
            lastPreviewTargets.Add(kv.Key, kv.Value);

        StartCoroutine(AnimatePreview(targets, 0.3f));
    }

    private IEnumerator AnimatePreview(Dictionary<string, (float targetWhite, float targetColor)> targets, float duration)
    {
        // Запоминаем начальные значения слайдеров
        Dictionary<string, float> startWhite = new Dictionary<string, float>();
        Dictionary<string, float> startColor = new Dictionary<string, float>();
        foreach (var pair in characteristicsMap)
        {
            startWhite[pair.Key] = pair.Value.WhiteSlider.value;
            startColor[pair.Key] = pair.Value.ColorSlider.value;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            foreach (var pair in characteristicsMap)
            {
                string name = pair.Key;
                Characteristic ch = pair.Value;
                (float targetWhite, float targetColor) = targets[name];

                float newWhite = Mathf.Lerp(startWhite[name], targetWhite, t);
                float newColor = Mathf.Lerp(startColor[name], targetColor, t);

                ch.WhiteSlider.value = newWhite;
                ch.ColorSlider.value = newColor;

                // Определяем цвет на основе соотношения слайдеров
                Image colorImage = ch.ColorSlider.fillRect.GetComponent<Image>();
                if (newColor > newWhite + 0.01f)
                    colorImage.color = Color.green;
                else if (newColor < newWhite - 0.01f)
                    colorImage.color = Color.red;
                else
                    colorImage.color = Color.white;

                ch.Value.text = Mathf.RoundToInt(newColor).ToString();
            }
            yield return null;
        }

        // Финальная установка
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            (float targetWhite, float targetColor) = targets[name];

            ch.WhiteSlider.value = targetWhite;
            ch.ColorSlider.value = targetColor;

            Image colorImage = ch.ColorSlider.fillRect.GetComponent<Image>();
            if (targetColor > targetWhite + 0.01f)
                colorImage.color = Color.green;
            else if (targetColor < targetWhite - 0.01f)
                colorImage.color = Color.red;
            else
                colorImage.color = Color.white;

            ch.Value.text = Mathf.RoundToInt(targetColor).ToString();
        }
    }

    /// <summary>
    /// Мгновенно устанавливает слайдеры в последние рассчитанные цели предпросмотра.
    /// Вызывается перед подтверждением, чтобы гарантировать старт с правильных позиций.
    /// </summary>
    public void SetToPreviewTargets()
    {
        if (lastPreviewTargets == null || lastPreviewTargets.Count == 0) return;

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            if (lastPreviewTargets.TryGetValue(name, out var target))
            {
                ch.WhiteSlider.value = target.targetWhite;
                ch.ColorSlider.value = target.targetColor;
                ch.Value.text = Mathf.RoundToInt(target.targetColor).ToString();

                Image colorImage = ch.ColorSlider.fillRect.GetComponent<Image>();
                if (target.targetColor > target.targetWhite + 0.01f)
                    colorImage.color = Color.green;
                else if (target.targetColor < target.targetWhite - 0.01f)
                    colorImage.color = Color.red;
                else
                    colorImage.color = Color.white;
            }
        }
    }

    // ================== Публичные методы ==================

    public void StopAllPreviews()
    {
        StopAllCoroutines();
        isReverting = false;
        revertCoroutine = null;
    }

    public IEnumerator ApplyChangesCoroutine()
    {
        // Плавно сводим слайдеры (зелёный -> белый догоняет; красный -> цветной догоняет)
        List<Coroutine> coroutines = new List<Coroutine>();

        foreach (var pair in characteristicsMap)
        {
            Characteristic ch = pair.Value;
            Image colorImg = ch.ColorSlider.fillRect.GetComponent<Image>();
            Color curColor = colorImg.color;

            if (curColor == Color.green)
            {
                // Белый догоняет цветной
                coroutines.Add(StartCoroutine(SmoothMove(ch.WhiteSlider, ch.ColorSlider.value, colorImg, curColor)));
            }
            else if (curColor == Color.red)
            {
                // Цветной догоняет белый
                coroutines.Add(StartCoroutine(SmoothMove(ch.ColorSlider, ch.WhiteSlider.value, colorImg, curColor)));
            }
        }

        foreach (var c in coroutines)
            yield return c;

        // Фиксируем значения
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float final = ch.WhiteSlider.value; // после сведения оба равны
            currentCharacteristicValues[name] = final;
            ch.Value.text = Mathf.RoundToInt(final).ToString();
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }

        Debug.Log("Характеристики подтверждены: " + string.Join(", ", currentCharacteristicValues));
    }

    private IEnumerator SmoothMove(Slider movingSlider, float targetValue, Image colorImage, Color startColor)
    {
        float startVal = movingSlider.value;
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            movingSlider.value = Mathf.Lerp(startVal, targetValue, t);
            colorImage.color = Color.Lerp(startColor, Color.white, t);
            yield return null;
        }
        movingSlider.value = targetValue;
        colorImage.color = Color.white;
    }

    public float GetCurrentValue(string characteristicName)
    {
        if (currentCharacteristicValues != null && currentCharacteristicValues.ContainsKey(characteristicName))
            return currentCharacteristicValues[characteristicName];
        Debug.LogWarning($"Характеристика {characteristicName} не найдена");
        return 0;
    }

    public bool HasCharacteristic(string name)
    {
        return characteristicsMap != null && characteristicsMap.ContainsKey(name);
    }

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
        frameRect.anchoredPosition = new Vector2(
            moduleRect.anchoredPosition.x,
            moduleRect.anchoredPosition.y - 1
        );

        if (selectedModuleIndex != null && selectedModuleIndex.Value < allModulesList.Count)
        {
            var prevModule = allModulesList[selectedModuleIndex.Value];
            if (prevModule != null && prevModule.TryGetComponent<ModuleButton>(out ModuleButton prevBtn))
                prevBtn.enabled = true;
        }

        targetModule.enabled = false;
        selectedModuleIndex = allModulesList.IndexOf(module);
        lastSelectedCategory = category;

        // Обновляем словарь выбранных модулей
        if (selectedModulesByCategory.ContainsKey(category))
            selectedModulesByCategory[category] = module;
        else
            selectedModulesByCategory.Add(category, module);
    }

    /// <summary>
    /// Сбрасывает все выбранные модули и возвращает характеристики к базовым значениям.
    /// Вызывать при закрытии меню выбора модулей.
    /// </summary>
    public void ClearSelection()
    {
        selectedModulesByCategory.Clear();
        selectedModuleIndex = null;
        lastSelectedCategory = null;
        lastPreviewTargets.Clear();

        // Возвращаем все характеристики к базовым значениям
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            float baseVal = baseCharacteristicValues[name];
            ch.WhiteSlider.value = baseVal;
            ch.ColorSlider.value = baseVal;
            ch.Value.text = Mathf.RoundToInt(baseVal).ToString();
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
            currentCharacteristicValues[name] = baseVal;
        }

        // Скрываем все рамки
        HideAllFrames();
    }

    // Оставлено для обратной совместимости, но не используется в новой логике
    public void SetChanges(string parameterName, int value) { }
    public void SetNeedToReset(bool value) { needToReset = value; }
    public void RevertToCurrent() { }
}