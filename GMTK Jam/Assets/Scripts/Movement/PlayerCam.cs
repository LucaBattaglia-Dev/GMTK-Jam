using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensX = 400f;
    public float sensY = 400f;

    [Header("Target Follow")]
    public Transform player; // Drag your Player GameObject here
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, 0f);
    public float slideCameraHeight = 0.75f;
    public float heightLerpSpeed = 10f;

    [Header("Pause & Cursor State")]
    public bool isCursorUnlocked = false;

    private float normalCameraHeight;
    private float targetCameraHeight;

    private float xRotation;
    private float yRotation;
    private float currentTilt;
    private float targetTilt;

    private void Start()
    {
        LockCursor();

        normalCameraHeight = cameraOffset.y;
        targetCameraHeight = normalCameraHeight;

        if (player != null)
            yRotation = player.eulerAngles.y;
    }

    private void Update()
    {
        // Only rotate camera if the cursor is locked (game is unpaused)
        if (!isCursorUnlocked)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            if (player != null)
                player.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 10f);
        cameraOffset.y = Mathf.Lerp(cameraOffset.y, targetCameraHeight, Time.deltaTime * heightLerpSpeed);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, currentTilt);
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + cameraOffset;
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorUnlocked = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorUnlocked = true;
    }

    public void SetTilt(float tilt)
    {
        targetTilt = tilt;
    }

    public void SetSlide(bool isSliding)
    {
        targetCameraHeight = isSliding ? slideCameraHeight : normalCameraHeight;
    }
}