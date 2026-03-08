using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SlimeJump.Attributes
{
    /// <summary>
    /// Атрибут для группировки полей в складываемые секции
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FoldoutAttribute : PropertyAttribute
    {
        public string Title { get; private set; }
        public bool StartExpanded { get; private set; }

        public FoldoutAttribute(string title, bool startExpanded = true)
        {
            Title = title;
            StartExpanded = startExpanded;
        }
    }

    /// <summary>
    /// Атрибут для обязательных полей
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequiredAttribute : PropertyAttribute
    {
        public string Message { get; private set; }

        public RequiredAttribute(string message = null)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Режим работы ReadOnly атрибута
    /// </summary>
    public enum ReadOnlyMode
    {
        Always,         // Всегда только для чтения
        PlayMode,       // Только для чтения в Play Mode
        EditMode        // Только для чтения в Edit Mode
    }

    /// <summary>
    /// Атрибут для полей только для чтения
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyMode Mode { get; private set; }

        /// <summary>
        /// Делает поле только для чтения
        /// </summary>
        /// <param name="mode">Когда поле должно быть ReadOnly</param>
        public ReadOnlyAttribute(ReadOnlyMode mode = ReadOnlyMode.Always)
        {
            Mode = mode;
        }
    }

    /// <summary>
    /// Источник поиска компонента для автоназначения
    /// </summary>
    public enum AutoAssignSource
    {
        Self,
        Children,
        Parent,
        Scene,
        Anywhere
    }

    /// <summary>
    /// Атрибут для автоматического назначения компонентов
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AutoAssignAttribute : PropertyAttribute
    {
        public AutoAssignSource Source { get; private set; }
        public bool Required { get; private set; }

        public AutoAssignAttribute(AutoAssignSource source = AutoAssignSource.Self, bool required = true)
        {
            Source = source;
            Required = required;
        }
    }

    /// <summary>
    /// Добавляет кнопку для проигрывания AudioClip
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SoundPreviewAttribute : PropertyAttribute
    {
        public float Volume { get; private set; }

        public SoundPreviewAttribute(float volume = 1f)
        {
            Volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Пресеты для AnimationCurve
    /// </summary>
    public enum CurvePreset
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Constant,
        Bounce,
        Spring,
        Custom
    }

    /// <summary>
    /// Добавляет быстрые пресеты для AnimationCurve
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class CurveAttribute : PropertyAttribute
    {
        public CurvePreset Preset { get; private set; }
        public Color CurveColor { get; private set; }

        public CurveAttribute(CurvePreset preset = CurvePreset.Custom)
        {
            Preset = preset;
            CurveColor = Color.green;
        }

        public CurveAttribute(CurvePreset preset, float r, float g, float b)
        {
            Preset = preset;
            CurveColor = new Color(r, g, b);
        }
    }

    /// <summary>
    /// Отображает Vector2 как диапазон Min-Max
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MinMaxAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public bool ShowValues { get; private set; }

        public MinMaxAttribute(float min, float max, bool showValues = true)
        {
            Min = min;
            Max = max;
            ShowValues = showValues;
        }
    }
}

#if UNITY_EDITOR

namespace SlimeJump.Attributes
{
    using UnityEditor;
    using System.Reflection;
    using System.Collections.Generic;

    [CustomPropertyDrawer(typeof(AutoAssignAttribute))]
    public class AutoAssignDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AutoAssignAttribute autoAssign = (AutoAssignAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                float buttonWidth = 50f;
                float spacing = 5f;

                Rect fieldRect = new Rect(position.x, position.y, position.width - buttonWidth - spacing, position.height);
                Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth * (fieldRect.width / position.width);

                EditorGUI.PropertyField(fieldRect, property, label, true);

                EditorGUIUtility.labelWidth = originalLabelWidth;

                if (GUI.Button(buttonRect, "Auto", EditorStyles.miniButton))
                {
                    AssignComponent(property, autoAssign);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
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
                Undo.RecordObject(property.serializedObject.targetObject, "Auto Assign Component");
                property.objectReferenceValue = foundComponent;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
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
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnHierarchyChanged() { }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in allBehaviours)
                {
                    ProcessAutoAssignRuntime(behaviour);
                }
            }
        }

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

        public static void ProcessAutoAssignRuntime(MonoBehaviour target)
        {
            if (target == null) return;

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
                        }
                        else if (autoAssign.Required)
                        {
                            Debug.LogWarning($"[AutoAssign] Could not find {field.FieldType.Name} for {field.Name} on {target.name}", target);
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

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ReadOnlyAttribute readOnlyAttr = (ReadOnlyAttribute)attribute;

            bool shouldBeReadOnly = false;

            switch (readOnlyAttr.Mode)
            {
                case ReadOnlyMode.Always:
                    shouldBeReadOnly = true;
                    break;

                case ReadOnlyMode.PlayMode:
                    shouldBeReadOnly = EditorApplication.isPlaying;
                    break;

                case ReadOnlyMode.EditMode:
                    shouldBeReadOnly = !EditorApplication.isPlaying;
                    break;
            }

            bool wasEnabled = GUI.enabled;
            if (shouldBeReadOnly)
            {
                GUI.enabled = false;
            }

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif

namespace SlimeJump.Attributes
{
    /// <summary>
    /// Базовый класс с автоматической обработкой AutoAssign
    /// </summary>
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

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            AutoAssignProcessor.ProcessAutoAssignRuntime(this);
#endif
        }
    }
}