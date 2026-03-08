using UnityEditor;
using UnityEngine;
using SlimeJump.Attributes;
using System.Reflection;

/// <summary>
/// Drawer для SoundPreview - добавляет кнопку Play для AudioClip
/// ИСПРАВЛЕНО: Теперь звук действительно проигрывается в редакторе
/// </summary>
[CustomPropertyDrawer(typeof(SoundPreviewAttribute))]
public class SoundPreviewDrawer : PropertyDrawer
{
    private static AudioClip currentlyPlayingClip;
    private static double clipStartTime;
    private static System.Reflection.MethodInfo playClipMethod;
    private static System.Reflection.MethodInfo stopAllClipsMethod;

    static SoundPreviewDrawer()
    {
        // Получаем доступ к внутренним методам Unity для проигрывания звука в Editor
        var unityEditorAssembly = typeof(AudioImporter).Assembly;
        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilClass != null)
        {
            playClipMethod = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            stopAllClipsMethod = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public
            );
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SoundPreviewAttribute soundAttr = (SoundPreviewAttribute)attribute;

        // Проверка типа свойства
        if (property.propertyType != SerializedPropertyType.ObjectReference ||
            (property.objectReferenceValue != null && !(property.objectReferenceValue is AudioClip)))
        {
            EditorGUI.PropertyField(position, property, label);
            Rect warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(warningRect, "[SoundPreview] работает только с AudioClip!", MessageType.Warning);
            return;
        }

        // Разделяем область: поле + кнопки
        float buttonWidth = 60f;
        float spacing = 5f;

        Rect propertyRect = new Rect(position.x, position.y, position.width - buttonWidth - spacing, position.height);
        Rect playButtonRect = new Rect(position.x + position.width - buttonWidth, position.y, 30f, position.height);
        Rect stopButtonRect = new Rect(position.x + position.width - 28f, position.y, 28f, position.height);

        // Рисуем поле AudioClip
        EditorGUI.PropertyField(propertyRect, property, label);

        AudioClip clip = property.objectReferenceValue as AudioClip;

        // Проверяем, играет ли сейчас этот клип
        bool isThisClipPlaying = currentlyPlayingClip == clip && IsClipPlaying();

        // Изменяем цвет кнопки Play если клип играет
        Color originalColor = GUI.backgroundColor;

        // Кнопка Play
        GUI.enabled = clip != null && !isThisClipPlaying;
        GUI.backgroundColor = isThisClipPlaying ? Color.green : originalColor;

        if (GUI.Button(playButtonRect, new GUIContent("▶", "Воспроизвести звук"), EditorStyles.miniButtonLeft))
        {
            PlayClip(clip, soundAttr.Volume);
        }

        GUI.backgroundColor = originalColor;
        GUI.enabled = true;

        // Кнопка Stop
        GUI.enabled = isThisClipPlaying;
        GUI.backgroundColor = isThisClipPlaying ? Color.red : originalColor;

        if (GUI.Button(stopButtonRect, new GUIContent("■", "Остановить звук"), EditorStyles.miniButtonRight))
        {
            StopClip();
        }

        GUI.backgroundColor = originalColor;
        GUI.enabled = true;

        // Автоостановка когда клип закончился
        if (isThisClipPlaying && !IsClipPlaying())
        {
            StopClip();
        }

        // Принудительно обновляем Inspector пока клип играет
        if (isThisClipPlaying)
        {
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            SceneView.RepaintAll();
        }
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;

        // Останавливаем предыдущий клип
        StopClip();

        // МЕТОД 1: Используем внутренний API Unity (рекомендуется)
        if (playClipMethod != null)
        {
            playClipMethod.Invoke(null, new object[] { clip, 0, false });
            currentlyPlayingClip = clip;
            clipStartTime = EditorApplication.timeSinceStartup;
        }
        // МЕТОД 2: Fallback - создаем временный AudioSource (если метод 1 не работает)
        else
        {
            GameObject tempGO = EditorUtility.CreateGameObjectWithHideFlags(
                "TempPreviewAudio",
                HideFlags.HideAndDontSave,
                typeof(AudioSource)
            );

            AudioSource source = tempGO.GetComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.playOnAwake = false;
            source.Play();

            currentlyPlayingClip = clip;
            clipStartTime = EditorApplication.timeSinceStartup;

            // Удаляем объект после окончания клипа
            float clipLength = clip.length;
            EditorApplication.delayCall += () =>
            {
                System.Threading.Tasks.Task.Delay((int)(clipLength * 1000)).ContinueWith(t =>
                {
                    if (tempGO != null)
                    {
                        Object.DestroyImmediate(tempGO);
                    }
                });
            };
        }
    }

    private void StopClip()
    {
        if (currentlyPlayingClip == null) return;

        // МЕТОД 1: Используем внутренний API Unity
        if (stopAllClipsMethod != null)
        {
            stopAllClipsMethod.Invoke(null, null);
        }

        // МЕТОД 2: Удаляем временный AudioSource (если использовался)
        GameObject tempGO = GameObject.Find("TempPreviewAudio");
        if (tempGO != null)
        {
            Object.DestroyImmediate(tempGO);
        }

        currentlyPlayingClip = null;
    }

    private bool IsClipPlaying()
    {
        if (currentlyPlayingClip == null) return false;

        double elapsed = EditorApplication.timeSinceStartup - clipStartTime;
        return elapsed < currentlyPlayingClip.length;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Добавляем дополнительную высоту для warning сообщения если тип неправильный
        if (property.propertyType != SerializedPropertyType.ObjectReference ||
            (property.objectReferenceValue != null && !(property.objectReferenceValue is AudioClip)))
        {
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }

        return EditorGUIUtility.singleLineHeight;
    }
}