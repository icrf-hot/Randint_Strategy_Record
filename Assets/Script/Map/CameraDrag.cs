using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDrag : MonoBehaviour
{
    private Camera cam;
    private Vector3 lastMouseScreenPos;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            lastMouseScreenPos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 currentScreenPos = Mouse.current.position.ReadValue();

            Vector3 lastWorld = cam.ScreenToWorldPoint(new Vector3(
                lastMouseScreenPos.x,
                lastMouseScreenPos.y,
                -cam.transform.position.z));

            Vector3 currentWorld = cam.ScreenToWorldPoint(new Vector3(
                currentScreenPos.x,
                currentScreenPos.y,
                -cam.transform.position.z));

            transform.position += lastWorld - currentWorld;

            lastMouseScreenPos = currentScreenPos;
        }
    }
}
