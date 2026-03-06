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
        [Space(10)]
        [SerializeField] Slider whiteSlider;
        [SerializeField] private Slider colorSlider;
        [SerializeField] private TextMeshProUGUI value;

        public string Name => name;
        public Slider WhiteSlider => whiteSlider;
        public Slider ColorSlider => colorSlider;
        public TextMeshProUGUI Value => value;
    }

    [SerializeField] private List<Characteristic> characteristicsData;
    [Space(10)]
    [SerializeField] private List<UIModule> modules;
    [Space(5)]
    [SerializeField] private GameObject celectFrame;

    private Dictionary<string, Characteristic> characteristicsMap;
    private int? celectedModule = null;
    private int currentHpValue;
    private int currentDamageValue;
    private int maxHpValue;
    private int maxDamageValue;

    private bool lockApplyChanges = false;
    private bool needToReset = false;

    private const int baseHpValue = 100;
    private const int baseDamageValue = 10;

    private void Awake()
    {
        Instance = this;

        if (characteristicsData == null) return;

        characteristicsMap = new Dictionary<string, Characteristic>();
        foreach (var characteristic in characteristicsData)
        {
            if (characteristic == null) continue;
            if (!characteristicsMap.ContainsKey(characteristic.Name))
                characteristicsMap.Add(characteristic.Name, characteristic);
            else
                Debug.LogWarning($"Duplicate characteristic name: {characteristic.Name}");
        }

        maxHpValue = baseHpValue;
        maxDamageValue = baseDamageValue;

        foreach (var module in modules)
        {
            Dictionary<string, object> values = module.GetValues();
            int moduleHpValue = values["Hp"].ConvertTo<int>();
            if ((baseHpValue + moduleHpValue) > maxHpValue)
                maxHpValue = baseHpValue + moduleHpValue;

            int moduleDamageValue = values["Damage"].ConvertTo<int>();
            if ((baseDamageValue + moduleDamageValue) > maxDamageValue)
                maxDamageValue = baseDamageValue + moduleDamageValue;
        }

        characteristicsMap["Hp"].Value.text = baseHpValue.ToString();
        characteristicsMap["Hp"].WhiteSlider.maxValue = maxHpValue;
        characteristicsMap["Hp"].WhiteSlider.value = baseHpValue;
        characteristicsMap["Hp"].ColorSlider.maxValue = maxHpValue;
        characteristicsMap["Hp"].ColorSlider.value = baseHpValue;

        characteristicsMap["Damage"].Value.text = baseDamageValue.ToString();
        characteristicsMap["Damage"].WhiteSlider.maxValue = maxDamageValue;
        characteristicsMap["Damage"].WhiteSlider.value = baseDamageValue;
        characteristicsMap["Damage"].ColorSlider.maxValue = maxDamageValue;
        characteristicsMap["Damage"].ColorSlider.value = baseDamageValue;

        currentHpValue = baseHpValue;
        currentDamageValue = baseDamageValue;
    }

    public void SetChanges(string parametrName, int value)
    {
        if (characteristicsMap == null) return;
        if (characteristicsMap.TryGetValue(parametrName, out var characteristic))
        {
            StartCoroutine(SetChangesCoroutine(
                characteristic.WhiteSlider,
                characteristic.ColorSlider,
                characteristic.Value,
                value
            ));
        }
    }

    private IEnumerator SetChangesCoroutine(Slider whiteSlider, Slider colorSlider, TextMeshProUGUI text, int value, float duration = 0.5f)
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
            characteristicsMap["Hp"].Value.text = baseHpValue.ToString();
            characteristicsMap["Hp"].WhiteSlider.value = baseHpValue;
            characteristicsMap["Hp"].ColorSlider.value = baseHpValue;

            characteristicsMap["Damage"].Value.text = baseDamageValue.ToString();
            characteristicsMap["Damage"].WhiteSlider.value = baseDamageValue;
            characteristicsMap["Damage"].ColorSlider.value = baseDamageValue;

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
            string paramName = pair.Key;
            Characteristic characteristic = pair.Value;

            Image colorImage = characteristic.ColorSlider.fillRect.GetComponent<Image>();
            Color currentColor = colorImage.color;

            // ќпредел€ем, нужно ли что-то анимировать
            if (currentColor == Color.green)
            {
                // ”величивали Ц белый слайдер догон€ет цветной
                float targetValue = characteristic.ColorSlider.value;
                activeCoroutines.Add(StartCoroutine(ApplySingleCharacteristic(
                    characteristic.WhiteSlider, targetValue, colorImage, currentColor)));
            }
            else if (currentColor == Color.red)
            {
                // ”меньшали Ц цветной слайдер догон€ет белый
                float targetValue = characteristic.WhiteSlider.value;
                activeCoroutines.Add(StartCoroutine(ApplySingleCharacteristic(
                    characteristic.ColorSlider, targetValue, colorImage, currentColor)));
            }
            // ≈сли цвет уже белый Ц ничего не делаем
        }

        // ∆дЄм завершени€ всех параллельных анимаций
        foreach (var coroutine in activeCoroutines)
            yield return coroutine;

        // ќбновл€ем текущие значени€ (можно вз€ть из белого слайдера, так как теперь они синхронны)
        if (characteristicsMap.TryGetValue("Hp", out var hpChar))
            currentHpValue = (int)hpChar.WhiteSlider.value;
        if (characteristicsMap.TryGetValue("Damage", out var dmgChar))
            currentDamageValue = (int)dmgChar.WhiteSlider.value;

        lockApplyChanges = false;
    }

    private IEnumerator ApplySingleCharacteristic(Slider movingSlider, float targetValue, Image colorImage, Color startColor)
    {
        float startSliderValue = movingSlider.value;
        Color targetColor = Color.white;
        float duration = 0.5f; // можно вынести в параметр или константу
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

    public void SetNeedToReset(bool parametr) => needToReset = parametr;

    public void SetCelectedModule(ModuleButton targetModule)
    {
        if (targetModule == null || !targetModule.TryGetComponent<UIModule>(out UIModule targetModuleScript) || celectFrame == null) return;

        if (!celectFrame.activeInHierarchy)
            celectFrame.SetActive(true);

        if (celectedModule != null)
        {
            if (celectedModule.HasValue && celectedModule.Value < modules.Count)
            {
                modules[celectedModule.Value].TryGetComponent<ModuleButton>(out ModuleButton modulebutton);
                if (modulebutton != null)
                    modulebutton.enabled = true;
            }
        }

        targetModule.enabled = false;
        celectedModule = modules.IndexOf(targetModuleScript);

        if (celectedModule == -1)
        {
            Debug.LogWarning("ћодуль не найден в списке modules!");
            celectedModule = null;
            return;
        }

        RectTransform targetModuleTransform = targetModule.GetComponent<RectTransform>();
        celectFrame.GetComponent<RectTransform>().anchoredPosition = new Vector3(
            targetModuleTransform.anchoredPosition.x,
            targetModuleTransform.anchoredPosition.y - 1);
    }
}