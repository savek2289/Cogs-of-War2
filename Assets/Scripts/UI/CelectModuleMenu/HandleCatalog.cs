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
            items[i].TryGetComponent(out Image image);
            GameObject child1 = items[i].transform.GetChild(0).gameObject;
            GameObject child2 = items[i].transform.GetChild(1).gameObject;
            GameObject modulesContainer = items[i].transform.GetChild(2).gameObject;

            if (child1 != null)
                child1.gameObject.SetActive(false);
            
            if (child2 != null)
                child2.gameObject.SetActive(false);

            button.enabled = false;
            handler.enabled = false;
            image.enabled = false;

            if (modulesContainer == null) return;

            for (int j = 1; j < modulesContainer.transform.childCount; j++)
            {
                GameObject module = modulesContainer.transform.GetChild(j).gameObject;
                module.SetActive(true);
            }
        } 
    }

    public void ResetCatalog()
    {
        title.text = "Modules";

        for (int i = 0; i < items.Count; i++)
        {
            items[i].TryGetComponent(out Button button);
            items[i].TryGetComponent(out ScrollViewHoverHandler handler);
            items[i].TryGetComponent(out Image image);
            GameObject child1 = items[i].transform.GetChild(0).gameObject;
            GameObject child2 = items[i].transform.GetChild(1).gameObject;
            GameObject modulesContainer = items[i].transform.GetChild(2).gameObject;

            if (child1 != null)
                child1.gameObject.SetActive(true);

            if (child2 != null)
                child2.gameObject.SetActive(true);

            button.enabled = true;
            handler.enabled = true;
            image.enabled = true;

            if (modulesContainer == null) return;

            for (int j = 0; j < modulesContainer.transform.childCount; j++)
            {
                GameObject module = modulesContainer.transform.GetChild(j).gameObject;
                module.SetActive(false);
            }
        }
    }
}
