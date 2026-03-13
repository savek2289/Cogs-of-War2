using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacteristics : MonoBehaviour
{
    public static PlayerCharacteristics Instance { get; private set; }

    [System.Serializable]
    public class Characteristic
    {
        [SerializeField] private string name;
        [SerializeField] private int baseValue;          // базовое значение, настраиваетс€ в инспекторе
        [Space(10)]
        [SerializeField] Slider whiteSlider;
        [SerializeField] private Slider colorSlider;
        [SerializeField] private TextMeshProUGUI value;

        public string Name => name;
        public int BaseValue => baseValue;
        public Slider WhiteSlider => whiteSlider;
        public Slider ColorSlider => colorSlider;
        public TextMeshProUGUI Value => value;
    }

    [System.Serializable]
    public class ModuleCategory
    {
        [SerializeField] private string categoryName;
        [SerializeField] private List<UIModule> modules;

        public string CategoryName => categoryName;
        public List<UIModule> Modules => modules;
    }

    [SerializeField] private List<Characteristic> characteristicsData;
    [Space(10)]
    [SerializeField] private List<ModuleCategory> allCategories;
    [Space(5)]
    [SerializeField] private GameObject celectFrame;

    private Dictionary<string, Characteristic> characteristicsMap;
    private Dictionary<string, int> currentCharacteristicValues;   // подтверждЄнные значени€
    private Dictionary<string, int> baseCharacteristicValues;       // базовые (из инспектора)
    private Dictionary<string, int> maxCharacteristicValues;        // максимально достижимые

    private List<UIModule> allModulesList;          // плоский список всех модулей
    private int? celectedModuleIndex = null;
    private bool lockApplyChanges = false;
    private bool needToReset = false;

    private void Awake()
    {
        Instance = this;

        if (characteristicsData == null) return;

        // »нициализаци€ словарей
        characteristicsMap = new Dictionary<string, Characteristic>();
        currentCharacteristicValues = new Dictionary<string, int>();
        baseCharacteristicValues = new Dictionary<string, int>();
        maxCharacteristicValues = new Dictionary<string, int>();

        // «аполн€ем characteristicsMap из characteristicsData
        foreach (var characteristic in characteristicsData)
        {
            if (characteristic == null) continue;
            if (!characteristicsMap.ContainsKey(characteristic.Name))
                characteristicsMap.Add(characteristic.Name, characteristic);
            else
                Debug.LogWarning($"Duplicate characteristic name: {characteristic.Name}");
        }

        // —обираем все модули в плоский список
        allModulesList = new List<UIModule>();
        foreach (var category in allCategories)
            foreach (var module in category.Modules)
                if (module != null && !allModulesList.Contains(module))
                    allModulesList.Add(module);

        // »нициализируем базовые и текущие значени€ из инспектора
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            int baseVal = ch.BaseValue;

            baseCharacteristicValues[name] = baseVal;
            currentCharacteristicValues[name] = baseVal;
            maxCharacteristicValues[name] = baseVal; // пока равно базе
        }

        // ¬ычисл€ем максимальные значени€ дл€ каждой характеристики,
        // перебира€ категории и выбира€ лучший модуль в каждой категории.
        foreach (var category in allCategories)
        {
            // ƒл€ каждой характеристики запомним максимальный бонус в этой категории
            Dictionary<string, int> maxBonusInCategory = new Dictionary<string, int>();

            foreach (var module in category.Modules)
            {
                List<UIModule.Values> moduleValues = module.GetValues();
                foreach (var mv in moduleValues)
                {
                    string charName = mv.Name;
                    if (!characteristicsMap.ContainsKey(charName))
                    {
                        Debug.LogWarning($"ћодуль {module.name} содержит характеристику '{charName}', которой нет в characteristicsData!");
                        continue;
                    }

                    int intValue = Mathf.RoundToInt(mv.AddedValue);
                    if (maxBonusInCategory.ContainsKey(charName))
                    {
                        if (intValue > maxBonusInCategory[charName])
                            maxBonusInCategory[charName] = intValue;
                    }
                    else
                    {
                        maxBonusInCategory[charName] = intValue;
                    }
                }
            }

            // ƒобавл€ем найденные максимумы к общему максимуму
            foreach (var kvp in maxBonusInCategory)
            {
                maxCharacteristicValues[kvp.Key] += kvp.Value;
            }
        }

        // Ќастраиваем слайдеры в соответствии с полученными значени€ми
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            int baseVal = baseCharacteristicValues[name];
            int maxVal = maxCharacteristicValues[name];

            ch.WhiteSlider.maxValue = maxVal;
            ch.ColorSlider.maxValue = maxVal;
            ch.WhiteSlider.value = baseVal;
            ch.ColorSlider.value = baseVal;
            ch.Value.text = baseVal.ToString();
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
        }
    }

    public void SetChanges(string parametrName, int value)
    {
        if (characteristicsMap == null || !characteristicsMap.ContainsKey(parametrName)) return;

        StartCoroutine(SetChangesCoroutine(
            characteristicsMap[parametrName].WhiteSlider,
            characteristicsMap[parametrName].ColorSlider,
            characteristicsMap[parametrName].Value,
            parametrName,
            value
        ));
    }

    private IEnumerator SetChangesCoroutine(Slider whiteSlider, Slider colorSlider, TextMeshProUGUI text, string paramName, int value, float duration = 0.5f)
    {
        lockApplyChanges = true;

        bool? isPositive = value > 0 ? true : (value < 0 ? false : null);
        if (isPositive == null)
        {
            lockApplyChanges = false;
            yield break;
        }

        if (needToReset)
        {
            // —брос всех характеристик к подтверждЄнным значени€м
            foreach (var pair in characteristicsMap)
            {
                string name = pair.Key;
                Characteristic ch = pair.Value;
                int curVal = currentCharacteristicValues[name];

                ch.Value.text = curVal.ToString();
                ch.WhiteSlider.value = curVal;
                ch.ColorSlider.value = curVal;
                ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
            }
            needToReset = false;
        }

        colorSlider.gameObject.SetActive(true);
        Image colorImage = colorSlider.fillRect.GetComponent<Image>();

        int startTextValue = Convert.ToInt32(text.text);
        int endTextValue = startTextValue + value;

        float startWhite = whiteSlider.value;
        float startColor = colorSlider.value;
        float endWhite, endColor;

        if (isPositive == true)
        {
            endWhite = startWhite;
            endColor = startColor + value;
        }
        else
        {
            endWhite = startWhite + value;
            endColor = startColor;
        }

        // Ќе даЄм выйти за пределы слайдера
        endWhite = Mathf.Clamp(endWhite, whiteSlider.minValue, whiteSlider.maxValue);
        endColor = Mathf.Clamp(endColor, colorSlider.minValue, colorSlider.maxValue);

        Color startImageColor = colorImage.color;
        Color targetColor = isPositive == true ? Color.green : Color.red;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            whiteSlider.value = Mathf.Lerp(startWhite, endWhite, t);
            colorSlider.value = Mathf.Lerp(startColor, endColor, t);
            colorImage.color = Color.Lerp(startImageColor, targetColor, t);

            int currentText = Mathf.RoundToInt(Mathf.Lerp(startTextValue, endTextValue, t));
            text.text = currentText.ToString();

            yield return null;
        }

        whiteSlider.value = endWhite;
        colorSlider.value = endColor;
        colorImage.color = targetColor;
        text.text = endTextValue.ToString();

        lockApplyChanges = false;
    }

    public void ApplyChanges()
    {
        StartCoroutine(ApplyAllChanges());
    }

    private IEnumerator ApplyAllChanges()
    {
        while (lockApplyChanges)
            yield return null;

        lockApplyChanges = true;

        List<Coroutine> activeCoroutines = new List<Coroutine>();

        foreach (var pair in characteristicsMap)
        {
            Characteristic ch = pair.Value;
            Image colorImage = ch.ColorSlider.fillRect.GetComponent<Image>();
            Color currentColor = colorImage.color;

            if (currentColor == Color.green)
            {
                float targetValue = ch.ColorSlider.value;
                activeCoroutines.Add(StartCoroutine(ApplySingleCharacteristic(ch.WhiteSlider, targetValue, colorImage, currentColor)));
            }
            else if (currentColor == Color.red)
            {
                float targetValue = ch.WhiteSlider.value;
                activeCoroutines.Add(StartCoroutine(ApplySingleCharacteristic(ch.ColorSlider, targetValue, colorImage, currentColor)));
            }
        }

        foreach (var coroutine in activeCoroutines)
            yield return coroutine;

        // —охран€ем подтверждЄнные значени€
        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            int finalValue = (int)ch.WhiteSlider.value;
            currentCharacteristicValues[name] = finalValue;
            ch.Value.text = finalValue.ToString();
        }

        lockApplyChanges = false;
        Debug.Log("’арактеристики сохранены: " + string.Join(", ", currentCharacteristicValues));
    }

    private IEnumerator ApplySingleCharacteristic(Slider movingSlider, float targetValue, Image colorImage, Color startColor)
    {
        float startSliderValue = movingSlider.value;
        Color targetColor = Color.white;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            movingSlider.value = Mathf.Lerp(startSliderValue, targetValue, t);
            colorImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        movingSlider.value = targetValue;
        colorImage.color = targetColor;
    }

    public int GetCurrentValue(string characteristicName)
    {
        if (currentCharacteristicValues != null && currentCharacteristicValues.ContainsKey(characteristicName))
            return currentCharacteristicValues[characteristicName];

        Debug.LogWarning($"’арактеристика {characteristicName} не найдена");
        return 0;
    }

    public int GetBaseValue(string characteristicName)
    {
        if (baseCharacteristicValues != null && baseCharacteristicValues.ContainsKey(characteristicName))
            return baseCharacteristicValues[characteristicName];
        return 0;
    }

    public bool HasCharacteristic(string characteristicName)
    {
        return characteristicsMap != null && characteristicsMap.ContainsKey(characteristicName);
    }

    public void SetNeedToReset(bool value) => needToReset = value;

    public void SetCelectedModule(ModuleButton targetModule)
    {
        if (targetModule == null || !targetModule.TryGetComponent<UIModule>(out UIModule targetModuleScript) || celectFrame == null)
            return;

        if (!celectFrame.activeInHierarchy)
            celectFrame.SetActive(true);

        // ¬озвращаем обычное состо€ние предыдущему модулю
        if (celectedModuleIndex != null && celectedModuleIndex.Value < allModulesList.Count)
        {
            UIModule prevModule = allModulesList[celectedModuleIndex.Value];
            if (prevModule != null && prevModule.TryGetComponent<ModuleButton>(out ModuleButton prevButton))
                prevButton.enabled = true;
        }

        targetModule.enabled = false;  // блокируем повторное нажатие на этот же модуль

        int newIndex = allModulesList.IndexOf(targetModuleScript);
        if (newIndex == -1)
        {
            Debug.LogWarning("ћодуль не найден в общем списке модулей!");
            celectedModuleIndex = null;
            return;
        }
        celectedModuleIndex = newIndex;

        // ѕозиционируем рамку выделени€
        RectTransform targetModuleTransform = targetModule.GetComponent<RectTransform>();
        celectFrame.GetComponent<RectTransform>().anchoredPosition = new Vector3(
            targetModuleTransform.anchoredPosition.x,
            targetModuleTransform.anchoredPosition.y - 1);

        // ѕримен€ем эффекты нового модул€ (предпросмотр)
        ApplyModuleEffects(targetModuleScript);
    }

    private void ApplyModuleEffects(UIModule module)
    {
        List<UIModule.Values> moduleValues = module.GetValues();
        foreach (var mv in moduleValues)
        {
            string charName = mv.Name;
            if (characteristicsMap.ContainsKey(charName))
            {
                int intValue = Mathf.RoundToInt(mv.AddedValue);
                SetChanges(charName, intValue);
            }
            else
            {
                Debug.LogWarning($"ћодуль {module.name} пытаетс€ изменить характеристику '{charName}', которой нет в characteristicsData!");
            }
        }
    }
}