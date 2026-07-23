using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensX = 400f;
    public float sensY = 400f;

    [Header("Target Follow")]
    public Transform player; // Drag your Player GameObject here
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, 0f); // Default eye-level height
    public float slideCameraHeight = 0.75f; // Lowered camera height during a slide
    public float heightLerpSpeed = 10f;

    private float normalCameraHeight;
    private float targetCameraHeight;

    private float xRotation; // Up/Down Pitch
    private float yRotation; // Left/Right Yaw
    private float currentTilt;
    private float targetTilt;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        normalCameraHeight = cameraOffset.y;
        targetCameraHeight = normalCameraHeight;

        if (player != null)
            yRotation = player.eulerAngles.y;
    }

    private void Update()
    {
        // 1. Get Mouse Input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        // 2. Rotate Player body left/right along with camera horizontal turn
        if (player != null)
            player.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 3. Smoothly transition wall-run camera tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 10f);

        // 4. Smoothly transition camera height (standing vs sliding)
        cameraOffset.y = Mathf.Lerp(cameraOffset.y, targetCameraHeight, Time.deltaTime * heightLerpSpeed);

        // 5. Apply complete Camera Rotation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, currentTilt);
    }

    private void LateUpdate()
    {
        // Follow player position without parenting hierarchy changes
        if (player != null)
        {
            transform.position = player.position + cameraOffset;
        }
    }

    // Called by PlayerMovement for Wall-Running side tilt
    public void SetTilt(float tilt)
    {
        targetTilt = tilt;
    }

    // Called by PlayerMovement for Sliding height drops
    public void SetSlide(bool isSliding)
    {
        targetCameraHeight = isSliding ? slideCameraHeight : normalCameraHeight;
    }
}