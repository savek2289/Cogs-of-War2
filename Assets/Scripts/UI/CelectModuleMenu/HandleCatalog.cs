using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HandleCatalog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private List<GameObject> items;

    [Space(5)]
    [SerializeField] private ModuleCategories currentCategory = ModuleCategories.None;

    private enum DisplayMode
    {
        Subcategories,
        Modules
    }
    private DisplayMode currentMode = DisplayMode.Modules;

    public enum ModuleCategories
    {
        None,
        Head,
        Body,
        RightHand,
        LeftHand,
        RightElbow,
        LeftElbow,
        RightForearm,
        LeftForearm,
        RightBrush,
        LeftBrush,
        Pelvis,
        RightLeg,
        LeftLeg,
        RightHip,
        LeftHip,
        RightCalf,
        LeftCalf,
        RightFoot,
        LeftFoot
    }

    public void SetModuleCategoryName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            currentCategory = ModuleCategories.None;
            return;
        }

        if (System.Enum.TryParse(name, out ModuleCategories category))
            currentCategory = category;
        else
            currentCategory = ModuleCategories.None;
    }

    public void SetModeToSubcategories()
    {
        currentMode = DisplayMode.Subcategories;
    }

    public void SetModeToModules()
    {
        currentMode = DisplayMode.Modules;
    }

    public void Apply()
    {
        if (currentCategory == ModuleCategories.None) return;

        if (currentMode == DisplayMode.Subcategories)
        {
            // Проверяем, является ли текущая категория одной из конечностей, имеющих подкатегории
            if (IsLimbCategory(currentCategory))
            {
                ShowSubcategories(currentCategory);
            }
            else
            {
                Debug.LogWarning($"Режим подкатегорий выбран для категории {currentCategory}, у которой нет подкатегорий. Ничего не отображается.");
                HideAllItems();
                title.text = currentCategory.ToString();
            }
        }
        else
        {
            ShowModuleItems();
        }
    }

    private bool IsLimbCategory(ModuleCategories cat)
    {
        return cat == ModuleCategories.RightHand || cat == ModuleCategories.LeftHand ||
               cat == ModuleCategories.RightLeg || cat == ModuleCategories.LeftLeg;
    }

    private void ShowSubcategories(ModuleCategories limbCategory)
    {
        if (!IsLimbCategory(limbCategory))
        {
            Debug.LogWarning("ShowSubcategories вызван для категории без подкатегорий: " + limbCategory);
            return;
        }

        title.text = limbCategory.ToString();

        HideAllItems();

        HashSet<string> subNames = GetSubcategoryNames(limbCategory);
        foreach (var item in items)
        {
            if (item == null) continue;
            if (subNames.Contains(item.name))
            {
                item.SetActive(true);
                SetItemVisuals(item, true);
                ShowModules(item, false);
            }
        }
    }

    private void ShowModuleItems()
    {
        if (currentCategory == ModuleCategories.None) return;

        title.text = currentCategory.ToString();

        HideAllItems();

        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.name == currentCategory.ToString())
            {
                item.SetActive(true);
                SetItemVisuals(item, false);
                ShowModules(item, true);
                break;
            }
        }
    }

    public void ResetCatalog()
    {
        title.text = "Modules";
        currentMode = DisplayMode.Modules;

        // Список имён подкатегорий, которые должны быть скрыты при сбросе
        HashSet<string> hiddenItemNames = new HashSet<string>
        {
            "LeftElbow", "RightElbow",
            "LeftForearm", "RightForearm",
            "LeftBrush", "RightBrush",
            "LeftHip", "RightHip",
            "LeftCalf", "RightCalf",
            "LeftFoot", "RightFoot"
        };

        foreach (var item in items)
        {
            if (item == null) continue;

            SetItemVisuals(item, true);
            ShowModules(item, false);
            item.SetActive(!hiddenItemNames.Contains(item.name));
        }
    }

    private void Start()
    {
        ResetCatalog();
    }

    private void HideAllItems()
    {
        foreach (var item in items)
            if (item != null) item.SetActive(false);
    }

    private HashSet<string> GetSubcategoryNames(ModuleCategories limbCategory)
    {
        var set = new HashSet<string>();
        switch (limbCategory)
        {
            case ModuleCategories.RightHand:
                set.Add("RightElbow");
                set.Add("RightForearm");
                set.Add("RightBrush");
                break;
            case ModuleCategories.LeftHand:
                set.Add("LeftElbow");
                set.Add("LeftForearm");
                set.Add("LeftBrush");
                break;
            case ModuleCategories.RightLeg:
                set.Add("RightHip");
                set.Add("RightCalf");
                set.Add("RightFoot");
                break;
            case ModuleCategories.LeftLeg:
                set.Add("LeftHip");
                set.Add("LeftCalf");
                set.Add("LeftFoot");
                break;
            default:
                Debug.LogWarning($"GetSubcategoryNames вызван для неподдерживаемой категории: {limbCategory}");
                break;
        }
        return set;
    }

    private void SetItemVisuals(GameObject item, bool enabled)
    {
        if (item == null) return;

        if (item.TryGetComponent(out Button button))
            button.enabled = enabled;

        if (item.TryGetComponent(out ScrollViewHoverHandler handler))
            handler.enabled = enabled;

        if (item.TryGetComponent(out Image image))
            image.enabled = enabled;

        if (item.transform.childCount >= 1)
            item.transform.GetChild(0).gameObject.SetActive(enabled);

        if (item.transform.childCount >= 2)
            item.transform.GetChild(1).gameObject.SetActive(enabled);
    }

    private void ShowModules(GameObject item, bool show)
    {
        if (item == null || item.transform.childCount < 3) return;

        GameObject modulesContainer = item.transform.GetChild(2).gameObject;
        if (modulesContainer == null) return;

        modulesContainer.SetActive(show);
    }
}