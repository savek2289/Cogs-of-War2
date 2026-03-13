using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIModule : MonoBehaviour
{
    [SerializeField] PlayerCharacteristics characteristics;

    [SerializeField] private List<Values> values;

    [System.Serializable]
    public class Values
    {
        [SerializeField] private string name;
        [Space(10)]
        [SerializeField] private float addedValue;

        public string Name => name;
        public float AddedValue => addedValue;
    }

    public List<Values> GetValues() => values;

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