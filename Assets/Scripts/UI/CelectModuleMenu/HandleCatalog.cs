using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SlimeJump.Attributes;

public class HandleCatalog : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] List<GameObject> items;

    [Space(5)]

    [SerializeField, ReadOnly] private ModuleCategories currentCategory = ModuleCategories.None;

    public enum ModuleCategories
    { 
        None,
        Head,
        Body
    }

    public void SetModuleCategoryName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        switch (name)
        {
            case "Head":
                currentCategory = ModuleCategories.Head;
                break;
            case "Body":
                currentCategory = ModuleCategories.Body;
                break;
            default:
                currentCategory = ModuleCategories.None;
                break;
        }

    }

    public void SetChanges()
    {
        title.text = currentCategory.ToString();

        for (int i = 0; i < items.Count; i++)
        {
            items[i].TryGetComponent(out Button button);
            items[i].TryGetComponent(out ScrollViewHoverHandler handler);

            button.enabled = false;
            handler.enabled = false;
        } 
    }

    public void ResetCatalog()
    {
        title.text = "Modules";
    }
}
