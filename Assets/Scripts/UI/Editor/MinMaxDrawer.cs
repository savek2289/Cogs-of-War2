using UnityEditor;
using UnityEngine;
using SlimeJump.Attributes;

/// <summary>
/// Drawer для MinMax - отображает Vector2 как Min-Max слайдер
/// </summary>
[CustomPropertyDrawer(typeof(MinMaxAttribute))]
public class MinMaxDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxAttribute minMax = (MinMaxAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.Vector2)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.LabelField(position, label.text, "[MinMax] работает только с Vector2!");
            return;
        }

        Vector2 range = property.vector2Value;
        float minValue = range.x;
        float maxValue = range.y;

        // Начинаем property
        EditorGUI.BeginProperty(position, label, property);

        // Рисуем label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Область для значений и слайдера
        float totalWidth = position.width;
        float labelWidth = 30f;
        float spacing = 5f;

        Rect minLabelRect = new Rect(position.x, position.y, labelWidth, position.height);
        Rect sliderRect = new Rect(position.x + labelWidth + spacing, position.y,
                                   totalWidth - 2 * (labelWidth + spacing), position.height);
        Rect maxLabelRect = new Rect(position.x + totalWidth - labelWidth, position.y, labelWidth, position.height);

        // Отображаем текущие значения или поля ввода
        if (minMax.ShowValues)
        {
            // Поля для ввода min и max
            EditorGUI.BeginChangeCheck();
            minValue = EditorGUI.FloatField(minLabelRect, minValue);
            maxValue = EditorGUI.FloatField(maxLabelRect, maxValue);

            if (EditorGUI.EndChangeCheck())
            {
                minValue = Mathf.Clamp(minValue, minMax.Min, maxValue);
                maxValue = Mathf.Clamp(maxValue, minValue, minMax.Max);
            }
        }
        else
        {
            // Просто отображаем значения
            EditorGUI.LabelField(minLabelRect, minValue.ToString("F1"), EditorStyles.miniLabel);
            EditorGUI.LabelField(maxLabelRect, maxValue.ToString("F1"), EditorStyles.miniLabel);
        }

        // MinMax слайдер
        EditorGUI.BeginChangeCheck();
        EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, minMax.Min, minMax.Max);

        if (EditorGUI.EndChangeCheck())
        {
            property.vector2Value = new Vector2(minValue, maxValue);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}