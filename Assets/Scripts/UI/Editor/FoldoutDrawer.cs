using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using SlimeJump.Attributes;

public class SlimeJumpEditor : Editor
{
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private List<FieldGroup> fieldGroups;

    private class FieldGroup
    {
        public string FoldoutTitle;
        public List<FieldInfo> Fields = new List<FieldInfo>();
    }

    protected virtual void OnEnable()
    {
        BuildFieldGroups();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty scriptProp = serializedObject.FindProperty("m_Script");
        if (scriptProp != null)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(scriptProp);
            }
        }

        DrawGroupedFields();
        DrawCustomInspector();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void DrawCustomInspector() { }

    private void BuildFieldGroups()
    {
        fieldGroups = new List<FieldGroup>();

        var allFields = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .Where(f => f.GetCustomAttribute<HideInInspector>() == null)
            .OrderBy(f => f.MetadataToken)
            .ToList();

        FieldGroup currentGroup = new FieldGroup();

        foreach (var field in allFields)
        {
            var foldoutAttr = field.GetCustomAttribute<FoldoutAttribute>();

            if (foldoutAttr != null)
            {
                if (currentGroup.Fields.Count > 0)
                {
                    fieldGroups.Add(currentGroup);
                }

                currentGroup = new FieldGroup
                {
                    FoldoutTitle = foldoutAttr.Title
                };
            }

            currentGroup.Fields.Add(field);
        }

        if (currentGroup.Fields.Count > 0)
        {
            fieldGroups.Add(currentGroup);
        }
    }

    private void DrawGroupedFields()
    {
        foreach (var group in fieldGroups)
        {
            if (group.FoldoutTitle != null)
            {
                string foldoutKey = GetFoldoutKey(group.FoldoutTitle);
                bool isExpanded = GetFoldoutState(foldoutKey);

                isExpanded = EditorGUILayout.Foldout(isExpanded, group.FoldoutTitle, true, EditorStyles.foldoutHeader);
                SetFoldoutState(foldoutKey, isExpanded);

                if (isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawFields(group.Fields);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                DrawFields(group.Fields);
            }
        }
    }

    private void DrawFields(List<FieldInfo> fields)
    {
        foreach (var field in fields)
        {
            var prop = serializedObject.FindProperty(field.Name);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }

    private string GetFoldoutKey(string title)
    {
        return $"Foldout_{target.GetType().Name}_{target.GetInstanceID()}_{title}";
    }

    private bool GetFoldoutState(string key)
    {
        if (!foldoutStates.ContainsKey(key))
        {
            foldoutStates[key] = EditorPrefs.GetBool(key, true);
        }
        return foldoutStates[key];
    }

    private void SetFoldoutState(string key, bool state)
    {
        foldoutStates[key] = state;
        EditorPrefs.SetBool(key, state);
    }
}

[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
[CanEditMultipleObjects]
public class AutoFoldoutEditor : SlimeJumpEditor
{
    private bool shouldUseFoldout = false;

    protected override void OnEnable()
    {
        base.OnEnable();

        var fields = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        shouldUseFoldout = fields.Any(f => f.GetCustomAttribute<FoldoutAttribute>() != null);
    }

    public override void OnInspectorGUI()
    {
        if (shouldUseFoldout)
        {
            base.OnInspectorGUI();
        }
        else
        {
            DrawDefaultInspector();
        }
    }
}

[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
[CanEditMultipleObjects]
public class AutoFoldoutScriptableEditor : SlimeJumpEditor
{
    private bool shouldUseFoldout = false;

    protected override void OnEnable()
    {
        base.OnEnable();

        shouldUseFoldout = target.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(f => f.GetCustomAttribute<FoldoutAttribute>() != null);
    }

    public override void OnInspectorGUI()
    {
        if (shouldUseFoldout)
        {
            base.OnInspectorGUI();
        }
        else
        {
            DrawDefaultInspector();
        }
    }
}