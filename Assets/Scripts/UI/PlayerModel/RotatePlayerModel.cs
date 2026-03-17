using UnityEngine;

public class MouseRotator : MonoBehaviour
{
    [SerializeField, Tooltip("Sensitivity of mouse movement.")]
    private float sensitivity = 5f;

    [SerializeField, Tooltip("Invert vertical rotation.")]
    private bool invertY = false;

    [SerializeField, Tooltip("Minimum vertical angle.")]
    private float minY = -80f;

    [SerializeField, Tooltip("Maximum vertical angle.")]
    private float maxY = 80f;

    [Header("Rotation Activation Area")]
    [SerializeField, Tooltip("Left margin (in pixels) where rotation can start.")]
    private float leftMargin = 100f;

    [SerializeField, Tooltip("Right margin (in pixels) where rotation can start.")]
    private float rightMargin = 100f;

    [Header("Zoom Settings")]
    [SerializeField, Tooltip("Zoom sensitivity (scroll wheel).")]
    private float zoomSpeed = 1f;

    [SerializeField, Tooltip("Minimum zoom scale factor.")]
    private float minZoom = 0.5f;

    [SerializeField, Tooltip("Maximum zoom scale factor.")]
    private float maxZoom = 2f;

    [Header("Debug")]
    [SerializeField, Tooltip("Show active area overlay.")]
    private bool debugMode = false;

    private float rotationX;
    private float rotationY;
    private bool isRotating = false;

    private Vector3 originalScale;
    private float zoomMultiplier = 1f;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

        originalScale = transform.localScale;
    }

    private void Update()
    {
        // --- Mouse wheel zoom (always active) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            zoomMultiplier += scroll * zoomSpeed;
            zoomMultiplier = Mathf.Clamp(zoomMultiplier, minZoom, maxZoom);
            transform.localScale = originalScale * zoomMultiplier;
        }

        // --- Rotation activation check ---
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (mousePos.x >= leftMargin && mousePos.x <= Screen.width - rightMargin)
            {
                isRotating = true;
            }
            else
            {
                isRotating = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }

        if (!isRotating) return;

        // --- Rotation logic ---
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        if (invertY) mouseY = -mouseY;

        rotationX += mouseX;
        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }

    // Visualise active area in the editor when debug mode is enabled
    private void OnGUI()
    {
        if (!Application.isEditor || !debugMode) return;

        Rect zoneRect = new Rect(leftMargin, 0, Screen.width - leftMargin - rightMargin, Screen.height);

        // Semi-transparent fill
        GUI.color = new Color(0, 1, 0, 0.2f);
        GUI.DrawTexture(zoneRect, Texture2D.whiteTexture);

        // Borders
        GUI.color = Color.green;
        // Left border
        GUI.DrawTexture(new Rect(leftMargin - 2, 0, 2, Screen.height), Texture2D.whiteTexture);
        // Right border
        GUI.DrawTexture(new Rect(Screen.width - rightMargin, 0, 2, Screen.height), Texture2D.whiteTexture);
    }
}