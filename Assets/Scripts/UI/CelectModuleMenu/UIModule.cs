using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIModule : MonoBehaviour
{
    [SerializeField] PlayerCharacteristics characteristics;

    [Space(5)]

    [SerializeField] private string categoryName;
    [SerializeField] private GameObject module;

    [Space(5)]

    [SerializeField] private List<Values> values;
    [SerializeField] private bool cancelCategory; // true = модуль отменяет выбор в своей категории

    [System.Serializable]
    public class Values
    {
        [SerializeField] private string name;
        [Space(10)]
        [SerializeField] private float addedValue;

        public string Name => name;
        public float AddedValue => addedValue;
    }

    public bool CancelCategory => cancelCategory;

    public List<Values> GetValues() => values;

    public string GetCategoryName() => categoryName;

    public GameObject GetModule() => module;

    public float GetValueForCharacteristic(string characteristicName)
    {
        foreach (var value in values)
        {
            if (value.Name == characteristicName)
                return value.AddedValue;
        }
        return 0f;
    }

    public bool AffectsCharacteristic(string characteristicName)
    {
        foreach (var value in values)
        {
            if (value.Name == characteristicName)
                return true;
        }
        return false;
    }

    public void ApplyModuleEffects()
    {
        if (characteristics == null)
        {
            Debug.LogError("PlayerCharacteristics не назначен в UIModule!");
            return;
        }

        foreach (var value in values)
        {
            int intValue = Mathf.RoundToInt(value.AddedValue);
            characteristics.SetChanges(value.Name, intValue);
        }
    }

    public void RevertModuleEffects()
    {
        if (characteristics == null)
        {
            Debug.LogError("PlayerCharacteristics не назначен в UIModule!");
            return;
        }

        foreach (var value in values)
        {
            int intValue = Mathf.RoundToInt(value.AddedValue);
            characteristics.SetChanges(value.Name, -intValue);
        }
    }

    private void Start()
    {
        if (characteristics == null)
            characteristics = PlayerCharacteristics.Instance;
    }
}