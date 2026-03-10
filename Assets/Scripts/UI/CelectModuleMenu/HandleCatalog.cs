using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SlimeJump.Attributes;

public class HandleCatalog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private List<GameObject> items;

    [Space(5)]
    [SerializeField, ReadOnly] private ModuleCategories currentCategory = ModuleCategories.None;

    // Режим отображения
    private enum DisplayMode
    {
        Subcategories,  // показывать подкатегории (для Hand/Leg)
        Modules         // показывать модули (для любой категории)
    }
    private DisplayMode currentMode = DisplayMode.Modules; // по умолчанию модули (после сброса будет ResetCatalog)

    public enum ModuleCategories
    {
        None,
        Head,
        Body,
        Hand,
        Elbow,
        Forearm,
        Brush,
        Pelvis,
        Leg,
        Hip,
        Calf,
        Foot
    }

    /// <summary>
    /// Устанавливает текущую категорию без применения изменений.
    /// </summary>
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

    /// <summary>
    /// Устанавливает режим показа подкатегорий.
    /// </summary>
    public void SetModeToSubcategories()
    {
        currentMode = DisplayMode.Subcategories;
    }

    /// <summary>
    /// Устанавливает режим показа модулей.
    /// </summary>
    public void SetModeToModules()
    {
        currentMode = DisplayMode.Modules;
    }

    /// <summary>
    /// Применяет изменения на основе текущей категории и установленного режима.
    /// </summary>
    public void Apply()
    {
        if (currentCategory == ModuleCategories.None) return;

        if (currentMode == DisplayMode.Subcategories)
        {
            // Режим подкатегорий – показываем подкатегории для текущей категории (если это Hand/Leg)
            if (currentCategory == ModuleCategories.Hand || currentCategory == ModuleCategories.Leg)
            {
                ShowSubcategories(currentCategory);
            }
            else
            {
                Debug.LogWarning($"Режим подкатегорий выбран для категории {currentCategory}, у которой нет подкатегорий. Ничего не отображается.");
                // Можно либо ничего не показывать, либо показать что-то по умолчанию.
                // По решению – просто скрываем всё.
                HideAllItems();
                title.text = currentCategory.ToString(); // но заголовок всё же обновим
            }
        }
        else // Modules
        {
            ShowModuleItems();
        }
    }

    /// <summary>
    /// Показывает подкатегории для указанной основной категории (Hand или Leg).
    /// </summary>
    private void ShowSubcategories(ModuleCategories mainCategory)
    {
        if (mainCategory != ModuleCategories.Hand && mainCategory != ModuleCategories.Leg)
        {
            Debug.LogWarning("ShowSubcategories вызван для категории без подкатегорий: " + mainCategory);
            return;
        }

        title.text = mainCategory.ToString();

        HideAllItems();

        HashSet<string> subNames = GetSubcategoryNames(mainCategory);
        foreach (var item in items)
        {
            if (item == null) continue;
            if (subNames.Contains(item.name))
            {
                item.SetActive(true);
                SetItemVisuals(item, true);      // визуалы кнопки включены
                ShowModules(item, false);        // модули скрыты
            }
        }
    }

    /// <summary>
    /// Показывает модули для текущей выбранной категории.
    /// </summary>
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
                SetItemVisuals(item, false);     // отключаем визуалы кнопки
                ShowModules(item, true);         // включаем модули
                break;
            }
        }
    }

    /// <summary>
    /// Возвращает каталог в исходное состояние (видны только основные категории, подкатегории скрыты).
    /// Также сбрасывает режим на Modules.
    /// </summary>
    public void ResetCatalog()
    {
        title.text = "Modules";
        currentMode = DisplayMode.Modules; // сбрасываем режим

        HashSet<string> hiddenItemNames = new HashSet<string>
        {
            "Elbow", "Forearm", "Brush", "Hip", "Calf", "Foot"
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

    // ------------------------------------------------------------
    // Вспомогательные методы
    // ------------------------------------------------------------

    private void HideAllItems()
    {
        foreach (var item in items)
            if (item != null) item.SetActive(false);
    }

    private HashSet<string> GetSubcategoryNames(ModuleCategories mainCategory)
    {
        var set = new HashSet<string>();
        if (mainCategory == ModuleCategories.Hand)
        {
            set.Add("Elbow");
            set.Add("Forearm");
            set.Add("Brush");
        }
        else if (mainCategory == ModuleCategories.Leg)
        {
            set.Add("Hip");
            set.Add("Calf");
            set.Add("Foot");
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

        // Первые два дочерних объекта: AnimatedImage и ModuleCategoryName
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