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
        [SerializeField] private int baseValue;
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
        [SerializeField] private GameObject categoryCelectFrame;

        public string CategoryName => categoryName;
        public List<UIModule> Modules => modules;
        public GameObject CelectFrame => categoryCelectFrame;
    }

    [SerializeField] private List<Characteristic> characteristicsData;
    [Space(10)]
    [SerializeField] private List<ModuleCategory> allCategories;

    private Dictionary<string, Characteristic> characteristicsMap;
    private Dictionary<string, int> currentCharacteristicValues;
    private Dictionary<string, int> baseCharacteristicValues;
    private Dictionary<string, int> maxCharacteristicValues;

    private List<UIModule> allModulesList;
    private int? selectedModuleIndex = null;
    private ModuleCategory lastSelectedCategory = null;

    private bool needToReset = false;
    private Coroutine revertCoroutine = null;
    private bool isReverting = false;

    private void Awake()
    {
        Instance = this;

        if (characteristicsData == null) return;

        characteristicsMap = new Dictionary<string, Characteristic>();
        currentCharacteristicValues = new Dictionary<string, int>();
        baseCharacteristicValues = new Dictionary<string, int>();
        maxCharacteristicValues = new Dictionary<string, int>();

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
            int baseVal = ch.BaseValue;
            baseCharacteristicValues[name] = baseVal;
            currentCharacteristicValues[name] = baseVal;
        }

        // Проверка синхронизации словарей (отладка)
        foreach (var key in characteristicsMap.Keys)
        {
            if (!baseCharacteristicValues.ContainsKey(key))
                Debug.LogError($"Характеристика '{key}' присутствует в characteristicsMap, но отсутствует в baseCharacteristicValues!");
        }

        maxCharacteristicValues = ComputeMaxCharacteristicValues();

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

        HideAllFrames();
    }

    private Dictionary<string, int> ComputeMaxCharacteristicValues()
    {
        var maxValues = new Dictionary<string, int>(baseCharacteristicValues);

        var categories = new List<List<UIModule>>();
        foreach (var cat in allCategories)
        {
            var validModules = new List<UIModule>();
            foreach (var mod in cat.Modules)
            {
                if (mod != null && !mod.CancelCategory)
                    validModules.Add(mod);
            }
            validModules.Insert(0, null);
            categories.Add(validModules);
        }

        var tempBonuses = new Dictionary<string, int>();
        ExploreCombinations(0, tempBonuses, categories, maxValues);

        return maxValues;
    }

    private void ExploreCombinations(int catIndex, Dictionary<string, int> tempBonuses, List<List<UIModule>> categories, Dictionary<string, int> maxValues)
    {
        if (catIndex >= categories.Count)
        {
            foreach (var kv in tempBonuses)
            {
                string charName = kv.Key;

                if (!baseCharacteristicValues.TryGetValue(charName, out int baseVal))
                {
                    Debug.LogWarning($"Характеристика '{charName}' отсутствует в baseCharacteristicValues, используется 0.");
                    baseVal = 0;
                }

                int total = baseVal + kv.Value;

                if (maxValues.TryGetValue(charName, out int currentMax))
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

                    int intBonus = Mathf.RoundToInt(val.AddedValue);
                    if (tempBonuses.ContainsKey(charName))
                        tempBonuses[charName] += intBonus;
                    else
                        tempBonuses[charName] = intBonus;
                }
            }

            ExploreCombinations(catIndex + 1, tempBonuses, categories, maxValues);

            if (module != null)
            {
                foreach (var val in module.GetValues())
                {
                    string charName = val.Name;
                    if (!characteristicsMap.ContainsKey(charName)) continue;

                    int intBonus = Mathf.RoundToInt(val.AddedValue);
                    tempBonuses[charName] -= intBonus;
                    if (tempBonuses[charName] == 0)
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

    private IEnumerator RevertAllToCurrentSmooth(float duration = 0.3f)
    {
        isReverting = true;

        var startWhite = new Dictionary<string, float>();
        var startColor = new Dictionary<string, float>();
        var startColorImg = new Dictionary<string, Color>();
        var targetValues = new Dictionary<string, int>();

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            startWhite[name] = ch.WhiteSlider.value;
            startColor[name] = ch.ColorSlider.value;
            startColorImg[name] = ch.ColorSlider.fillRect.GetComponent<Image>().color;
            targetValues[name] = currentCharacteristicValues[name];
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
                float val = Mathf.Lerp(startWhite[name], targetValues[name], t);
                ch.WhiteSlider.value = val;
                ch.ColorSlider.value = val;
                ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.Lerp(startColorImg[name], Color.white, t);
                ch.Value.text = Mathf.RoundToInt(val).ToString();
            }
            yield return null;
        }

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            int final = targetValues[name];
            ch.WhiteSlider.value = final;
            ch.ColorSlider.value = final;
            ch.ColorSlider.fillRect.GetComponent<Image>().color = Color.white;
            ch.Value.text = final.ToString();
        }

        isReverting = false;
        revertCoroutine = null;
    }

    private IEnumerator ChangeCharacteristic(Slider white, Slider color, TextMeshProUGUI text, int delta, float duration = 0.5f)
    {
        if (delta == 0)
            yield break;

        bool isPositive = delta > 0;
        Image colorImage = color.fillRect.GetComponent<Image>();

        int startText = Convert.ToInt32(text.text);
        int endText = startText + delta;

        float startWhite = white.value;
        float startColor = color.value;
        float endWhite, endColor;

        if (isPositive)
        {
            endWhite = startWhite;
            endColor = startColor + delta;
        }
        else
        {
            endWhite = startWhite + delta;
            endColor = startColor;
        }

        endWhite = Mathf.Clamp(endWhite, white.minValue, white.maxValue);
        endColor = Mathf.Clamp(endColor, color.minValue, color.maxValue);

        Color startImgColor = colorImage.color;
        Color targetColor = isPositive ? Color.green : Color.red;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            white.value = Mathf.Lerp(startWhite, endWhite, t);
            color.value = Mathf.Lerp(startColor, endColor, t);
            colorImage.color = Color.Lerp(startImgColor, targetColor, t);

            int curText = Mathf.RoundToInt(Mathf.Lerp(startText, endText, t));
            text.text = curText.ToString();

            yield return null;
        }

        white.value = endWhite;
        color.value = endColor;
        colorImage.color = targetColor;
        text.text = endText.ToString();
    }

    // ================== Публичные методы ==================

    public void StopAllPreviews()
    {
        StopAllCoroutines();
        isReverting = false;
        revertCoroutine = null;
    }

    public void SetChanges(string parameterName, int value)
    {
        if (!characteristicsMap.ContainsKey(parameterName)) return;

        if (isReverting)
        {
            StartCoroutine(SetChangesAfterRevert(parameterName, value));
            return;
        }

        if (needToReset)
        {
            needToReset = false;
            if (revertCoroutine != null)
                StopCoroutine(revertCoroutine);
            revertCoroutine = StartCoroutine(RevertAndThenChange(parameterName, value));
        }
        else
        {
            StartCoroutine(ChangeCharacteristic(
                characteristicsMap[parameterName].WhiteSlider,
                characteristicsMap[parameterName].ColorSlider,
                characteristicsMap[parameterName].Value,
                value));
        }
    }

    private IEnumerator RevertAndThenChange(string parameterName, int value)
    {
        yield return RevertAllToCurrentSmooth(0.3f);
        StartCoroutine(ChangeCharacteristic(
            characteristicsMap[parameterName].WhiteSlider,
            characteristicsMap[parameterName].ColorSlider,
            characteristicsMap[parameterName].Value,
            value));
    }

    private IEnumerator SetChangesAfterRevert(string parameterName, int value)
    {
        while (isReverting)
            yield return null;
        SetChanges(parameterName, value);
    }

    public void SetNeedToReset(bool value)
    {
        needToReset = value;
    }

    public IEnumerator ApplyChangesCoroutine()
    {
        yield return null;

        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (var pair in characteristicsMap)
        {
            Characteristic ch = pair.Value;
            Image colorImg = ch.ColorSlider.fillRect.GetComponent<Image>();
            Color curColor = colorImg.color;

            if (curColor == Color.green)
            {
                coroutines.Add(StartCoroutine(SmoothMove(ch.WhiteSlider, ch.ColorSlider.value, colorImg, curColor)));
            }
            else if (curColor == Color.red)
            {
                coroutines.Add(StartCoroutine(SmoothMove(ch.ColorSlider, ch.WhiteSlider.value, colorImg, curColor)));
            }
        }

        foreach (var c in coroutines)
            yield return c;

        foreach (var pair in characteristicsMap)
        {
            string name = pair.Key;
            Characteristic ch = pair.Value;
            int final = (int)ch.WhiteSlider.value;
            currentCharacteristicValues[name] = final;
            ch.Value.text = final.ToString();
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

    public int GetCurrentValue(string characteristicName)
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
    }

    public void RevertToCurrent()
    {
        if (revertCoroutine != null)
            StopCoroutine(revertCoroutine);
        revertCoroutine = StartCoroutine(RevertAllToCurrentSmooth(0.3f));
    }
}