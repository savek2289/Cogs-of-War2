using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIModule : MonoBehaviour
{
    [SerializeField] PlayerCharacteristics characteristics;

    [SerializeField] private int addingHp;
    [SerializeField] private int addingDamage;

    public int AddingHp => addingHp;
    public int AddingDamage => addingDamage;

    public Dictionary<string, object> GetValues()
    {
        Dictionary<string, object> values = new();
        values.Add("Hp", addingHp);
        values.Add("Damage", addingDamage);

        return values;
    }
}
