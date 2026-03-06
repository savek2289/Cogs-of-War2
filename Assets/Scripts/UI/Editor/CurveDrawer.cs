using UnityEditor;
using UnityEngine;
using SlimeJump.Attributes;

/// <summary>
/// Drawer для Curve - добавляет быстрые пресеты для AnimationCurve
/// </summary>
[CustomPropertyDrawer(typeof(CurveAttribute))]
public class CurveDrawer : PropertyDrawer
{
    private const float PRESET_BUTTON_WIDTH = 80f;
    private const float SPACING = 5f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        CurveAttribute curveAttr = (CurveAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.AnimationCurve)
        {
            EditorGUI.PropertyField(position, property, label);
            Rect warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(warningRect, "[Curve] работает только с AnimationCurve!", MessageType.Warning);
            return;
        }

        // Разделяем на: поле кривой + кнопка пресета
        Rect curveRect = new Rect(position.x, position.y, position.width - PRESET_BUTTON_WIDTH - SPACING, position.height);
        Rect buttonRect = new Rect(position.x + position.width - PRESET_BUTTON_WIDTH, position.y, PRESET_BUTTON_WIDTH, position.height);

        // Рисуем поле AnimationCurve
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(curveRect, property, label);

        // Кнопка для применения пресета
        if (curveAttr.Preset != CurvePreset.Custom)
        {
            if (GUI.Button(buttonRect, new GUIContent(curveAttr.Preset.ToString(), "Применить пресет кривой")))
            {
                property.animationCurveValue = GetPresetCurve(curveAttr.Preset);
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        else
        {
            // Показываем dropdown с выбором пресета
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent("Пресеты", "Выбрать пресет"), FocusType.Keyboard))
            {
                ShowPresetMenu(property, buttonRect);
            }
        }
    }

    private void ShowPresetMenu(SerializedProperty property, Rect buttonRect)
    {
        GenericMenu menu = new GenericMenu();

        foreach (CurvePreset preset in System.Enum.GetValues(typeof(CurvePreset)))
        {
            if (preset == CurvePreset.Custom) continue;

            menu.AddItem(
                new GUIContent(preset.ToString()),
                false,
                () =>
                {
                    property.animationCurveValue = GetPresetCurve(preset);
                    property.serializedObject.ApplyModifiedProperties();
                }
            );
        }

        menu.DropDown(buttonRect);
    }

    private AnimationCurve GetPresetCurve(CurvePreset preset)
    {
        switch (preset)
        {
            case CurvePreset.Linear:
                return AnimationCurve.Linear(0, 0, 1, 1);

            case CurvePreset.EaseIn:
                return AnimationCurve.EaseInOut(0, 0, 1, 1);

            case CurvePreset.EaseOut:
                var easeOut = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0)
                );
                SmoothTangents(easeOut);
                return easeOut;

            case CurvePreset.EaseInOut:
                return AnimationCurve.EaseInOut(0, 0, 1, 1);

            case CurvePreset.Constant:
                return AnimationCurve.Constant(0, 1, 1);

            case CurvePreset.Bounce:
                return new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.3f, 1.1f),
                    new Keyframe(0.5f, 0.9f),
                    new Keyframe(0.7f, 1.05f),
                    new Keyframe(1, 1)
                );

            case CurvePreset.Spring:
                return new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.2f, 1.2f),
                    new Keyframe(0.4f, 0.8f),
                    new Keyframe(0.6f, 1.1f),
                    new Keyframe(0.8f, 0.95f),
                    new Keyframe(1, 1)
                );

            default:
                return AnimationCurve.Linear(0, 0, 1, 1);
        }
    }

    private void SmoothTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            curve.SmoothTangents(i, 0);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.AnimationCurve)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }

        return EditorGUIUtility.singleLineHeight;
    }
}