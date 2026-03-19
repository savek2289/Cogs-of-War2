using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AddingModules : MonoBehaviour
{
    [SerializeField] List<GameObject> allModules;

    [SerializeField] List<GameObject> parents;

    private Dictionary<string, GameObject> activeModules = new();
    private Dictionary<string, GameObject> moduleParents = new();

    private void Awake()
    {
        activeModules.Add("Body", null);
        activeModules.Add("Elbow", null);
        activeModules.Add("Forearm", null);
        activeModules.Add("Brush", null);
        activeModules.Add("Pelvis", null);
        activeModules.Add("Foot", null);
        activeModules.Add("Calf", null);
        activeModules.Add("Hip", null);

        moduleParents.Add("Body", parents[0]);
        moduleParents.Add("RightElbow", parents[1]);
        moduleParents.Add("LeftElbow", parents[2]);
        moduleParents.Add("RightForearm", parents[2]);
        moduleParents.Add("LeftForearm", parents[3]);
        moduleParents.Add("RightBrush", parents[4]);
        moduleParents.Add("LeftBrush", parents[5]);
        moduleParents.Add("Pelvis", parents[6]);
        moduleParents.Add("RightFoot", parents[7]);
        moduleParents.Add("LeftFoot", parents[8]);
        moduleParents.Add("RightCalf", parents[9]);
        moduleParents.Add("LeftCalf", parents[10]);
        moduleParents.Add("RightHip", parents[11]);
        moduleParents.Add("LeftHip", parents[12]);
    }

    public void SetModule(UIModule uiModuleScript)
    {
        if (uiModuleScript == null) return;

        GameObject module = uiModuleScript.GetModule();
        string category = uiModuleScript.GetCategoryName();

        if (moduleParents[category].transform.childCount > 0)
        {
            DestroyImmediate(moduleParents[category].transform.GetChild(0).gameObject);
            activeModules.Remove(category);
        }

        if (module != null)
        {
            activeModules.Add(category, module);
            Instantiate(module, moduleParents[category].transform);
        }
    }

    private bool CheckExistModule(GameObject module)
    {
        for (int i = 0; i < allModules.Count; i++)
        {
            if(allModules[i] == module) return true;
        }

        return false;
    }
}
