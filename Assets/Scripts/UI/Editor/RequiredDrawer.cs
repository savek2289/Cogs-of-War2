#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SlimeJump.Attributes; // ← Добавили using!

[CustomPropertyDrawer(typeof(RequiredAttribute))]
public class RequiredDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RequiredAttribute requiredAttr = (RequiredAttribute)attribute;

        bool isEmpty = IsPropertyEmpty(property);

        if (isEmpty)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

            EditorGUI.PropertyField(position, property, label, true);

            GUI.backgroundColor = originalColor;

            Rect warningRect = new Rect(position.x + position.width - 20, position.y, 20, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(warningRect, new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml").image));
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool IsPropertyEmpty(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue == null;
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(property.stringValue);
            case SerializedPropertyType.Generic:
                if (property.isArray)
                    return property.arraySize == 0;
                return false;
            default:
                return false;
        }
    }
}

[InitializeOnLoad]
public static class RequiredFieldValidator
{
    static RequiredFieldValidator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (!ValidateRequiredFields())
            {
                EditorApplication.isPlaying = false;
            }
        }
    }

    private static bool ValidateRequiredFields()
    {
        List<string> errors = new List<string>();

        MonoBehaviour[] allObjects = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour obj in allObjects)
        {
            if (obj == null) continue;

            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                RequiredAttribute requiredAttr = field.GetCustomAttribute<RequiredAttribute>();

                if (requiredAttr != null)
                {
                    object value = field.GetValue(obj);
                    bool isEmpty = false;

                    if (value == null || value.Equals(null))
                    {
                        isEmpty = true;
                    }
                    else if (value is string str && string.IsNullOrEmpty(str))
                    {
                        isEmpty = true;
                    }
                    else if (value is UnityEngine.Object unityObj && unityObj == null)
                    {
                        isEmpty = true;
                    }
                    else if (value is System.Collections.ICollection collection && collection.Count == 0)
                    {
                        isEmpty = true;
                    }

                    if (isEmpty)
                    {
                        string errorMsg = requiredAttr.Message ?? $"Required field '{field.Name}' is not assigned";
                        string fullError = $"[{obj.gameObject.name}] {type.Name}.{field.Name}: {errorMsg}";
                        errors.Add(fullError);
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            string errorMessage = "Cannot enter Play Mode! Required fields are missing:\n\n";
            errorMessage += string.Join("\n", errors);

            EditorUtility.DisplayDialog("Required Fields Missing", errorMessage, "OK");

            foreach (string error in errors)
            {
                Debug.LogError($"[Required Field] {error}");
            }

            return false;
        }

        return true;
    }

    [MenuItem("Tools/Validate Required Fields")]
    private static void ValidateFromMenu()
    {
        if (ValidateRequiredFields())
        {
            EditorUtility.DisplayDialog("Validation Success", "All required fields are properly assigned!", "OK");
        }
    }
}
#endif