using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float sprintMultiplier = 3f;
    public float lookSpeed = 1f; 
    private float pitch = 0f;
    private float yaw = 0f; 

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * lookSpeed;
            pitch -= mouseDelta.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        float currentSpeed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed) currentSpeed *= sprintMultiplier;

        Vector3 moveDirection = new Vector3();

        if (Keyboard.current.wKey.isPressed) moveDirection += transform.forward;
        if (Keyboard.current.sKey.isPressed) moveDirection -= transform.forward;
        if (Keyboard.current.aKey.isPressed) moveDirection -= transform.right;
        if (Keyboard.current.dKey.isPressed) moveDirection += transform.right;
        
        if (Keyboard.current.eKey.isPressed) moveDirection += Vector3.up;
        if (Keyboard.current.qKey.isPressed) moveDirection -= Vector3.up;

        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
    }
}