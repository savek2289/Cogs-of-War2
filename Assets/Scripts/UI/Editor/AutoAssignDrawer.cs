#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SlimeJump.Attributes; // ← Добавили using!

[CustomPropertyDrawer(typeof(AutoAssignAttribute))]
public class AutoAssignDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        AutoAssignAttribute autoAssign = (AutoAssignAttribute)attribute;

        EditorGUI.PropertyField(position = new Rect(position.x, position.y, position.width - 45, position.height), property, label, true);

        Rect buttonRect = new Rect(position.x + position.width, position.y, 45, EditorGUIUtility.singleLineHeight);

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (GUI.Button(buttonRect, "Auto", EditorStyles.miniButton))
            {
                AssignComponent(property, autoAssign);
            }
        }
    }

    private void AssignComponent(SerializedProperty property, AutoAssignAttribute autoAssign)
    {
        MonoBehaviour targetObject = property.serializedObject.targetObject as MonoBehaviour;
        if (targetObject == null) return;

        Type fieldType = fieldInfo.FieldType;
        UnityEngine.Object foundComponent = FindComponent(targetObject, fieldType, autoAssign.Source);

        if (foundComponent != null)
        {
            property.objectReferenceValue = foundComponent;
            property.serializedObject.ApplyModifiedProperties();
            Debug.Log($"AutoAssign: Assigned {fieldType.Name} to {property.name} on {targetObject.name}");
        }
        else if (autoAssign.Required)
        {
            Debug.LogWarning($"AutoAssign: Could not find {fieldType.Name} for {property.name} on {targetObject.name}");
        }
    }

    private UnityEngine.Object FindComponent(MonoBehaviour targetObject, Type componentType, AutoAssignSource source)
    {
        switch (source)
        {
            case AutoAssignSource.Self:
                return targetObject.GetComponent(componentType);

            case AutoAssignSource.Children:
                return targetObject.GetComponentInChildren(componentType, true);

            case AutoAssignSource.Parent:
                return targetObject.GetComponentInParent(componentType);

            case AutoAssignSource.Scene:
                return UnityEngine.Object.FindObjectOfType(componentType);

            case AutoAssignSource.Anywhere:
                UnityEngine.Object component = targetObject.GetComponent(componentType);
                if (component != null) return component;

                component = targetObject.GetComponentInChildren(componentType, true);
                if (component != null) return component;

                component = targetObject.GetComponentInParent(componentType);
                if (component != null) return component;

                return UnityEngine.Object.FindObjectOfType(componentType);

            default:
                return null;
        }
    }
}

[InitializeOnLoad]
public static class AutoAssignProcessor
{
    static AutoAssignProcessor()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged() { }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() { }

    public static void ProcessAutoAssign(MonoBehaviour target)
    {
        Type type = target.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            AutoAssignAttribute autoAssign = field.GetCustomAttribute<AutoAssignAttribute>();

            if (autoAssign != null && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                object currentValue = field.GetValue(target);

                if (currentValue == null || currentValue.Equals(null))
                {
                    UnityEngine.Object foundComponent = FindComponentForField(target, field.FieldType, autoAssign.Source);

                    if (foundComponent != null)
                    {
                        field.SetValue(target, foundComponent);
                        EditorUtility.SetDirty(target);
                        Debug.Log($"AutoAssign: Assigned {field.FieldType.Name} to {field.Name} on {target.name}");
                    }
                    else if (autoAssign.Required)
                    {
                        Debug.LogWarning($"AutoAssign: Could not find {field.FieldType.Name} for {field.Name} on {target.name}", target);
                    }
                }
            }
        }
    }

    private static UnityEngine.Object FindComponentForField(MonoBehaviour target, Type componentType, AutoAssignSource source)
    {
        switch (source)
        {
            case AutoAssignSource.Self:
                return target.GetComponent(componentType);

            case AutoAssignSource.Children:
                return target.GetComponentInChildren(componentType, true);

            case AutoAssignSource.Parent:
                return target.GetComponentInParent(componentType);

            case AutoAssignSource.Scene:
                return UnityEngine.Object.FindObjectOfType(componentType);

            case AutoAssignSource.Anywhere:
                UnityEngine.Object component = target.GetComponent(componentType);
                if (component != null) return component;

                component = target.GetComponentInChildren(componentType, true);
                if (component != null) return component;

                component = target.GetComponentInParent(componentType);
                if (component != null) return component;

                return UnityEngine.Object.FindObjectOfType(componentType);

            default:
                return null;
        }
    }

    [MenuItem("Tools/Auto-Assign All Components in Scene")]
    private static void AutoAssignAllInScene()
    {
        MonoBehaviour[] allObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
        int count = 0;

        foreach (MonoBehaviour obj in allObjects)
        {
            ProcessAutoAssign(obj);
            count++;
        }

        Debug.Log($"AutoAssign: Processed {count} objects in scene");
    }
}

public abstract class AutoAssignBehaviour : MonoBehaviour
{
    protected virtual void Reset()
    {
#if UNITY_EDITOR
        AutoAssignProcessor.ProcessAutoAssign(this);
#endif
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        AutoAssignProcessor.ProcessAutoAssign(this);
#endif
    }
}
#endif