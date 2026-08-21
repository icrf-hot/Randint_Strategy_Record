using UnityEngine;
using UnityEngine.InputSystem;

public class OperatorFocusManager : MonoBehaviour
{
    public static OperatorFocusManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;

    [SerializeField] private float focusTimeScale = 0.2f;
    [SerializeField] private float normalTimeScale = 1f;

    private OperatorClickHandler currentOperator;

    private Vector3 originalCameraPosition;

    [SerializeField] private Transform frontFocusPoint;
    [SerializeField] private Transform middleFocusPoint;
    [SerializeField] private Transform backFocusPoint;

    [Header("Camera Move")]
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 targetCameraPosition;

    [Header("Camera Zoom")]
    [SerializeField] private float frontOrthographicSize = 3.5f;

    [SerializeField] private float middleOrthographicSize = 4.0f;

    [SerializeField] private float backOrthographicSize = 4.5f;

    [SerializeField] private float zoomSpeed = 5f;

    private float originalOrthographicSize;
    private float targetOrthographicSize;

    [Header("Camera Rotation")]

    [SerializeField] private float frontXRotation = 15f;
    [SerializeField] private float frontYRotation = 0f;
    [SerializeField] private float middleXRotation = 8f;
    [SerializeField] private float middleYRotation = 8f;
    [SerializeField] private float backXRotation = 0f;
    [SerializeField] private float backYRotation = 0f;

    [SerializeField] private float rotationSpeed = 5f;

    private Quaternion originalRotation;
    private Quaternion targetRotation;


    [SerializeField]
    private MonoBehaviour operatorInfoUI;

    private IOperatorInfoUI infoUI;

    private void Awake()
    {
        Instance = this;

        originalCameraPosition = mainCamera.transform.position;
        targetCameraPosition = originalCameraPosition;

        originalOrthographicSize = mainCamera.orthographicSize;
        targetOrthographicSize = originalOrthographicSize;

        originalRotation = mainCamera.transform.rotation;
        targetRotation = originalRotation;

        infoUI = operatorInfoUI as IOperatorInfoUI;

        if (infoUI == null)
        {
            Debug.LogError("IOperatorInfoUI가 연결되지 않았습니다.");
            enabled = false;
            return;
        }
    }



    private void Update()
    {
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetCameraPosition,
            moveSpeed * Time.unscaledDeltaTime);

        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize,
            targetOrthographicSize,
            zoomSpeed * Time.unscaledDeltaTime);

        mainCamera.transform.rotation = Quaternion.Lerp(
            mainCamera.transform.rotation,
            targetRotation,
            rotationSpeed * Time.unscaledDeltaTime);

        if (currentOperator == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitFocus();
        }
    }

    public void EnterFocus(OperatorClickHandler handler)
    {
        if (handler == null)
            return;

        if (currentOperator == handler)
        {
            ExitFocus();
            return;
        }

        bool changeOperator = currentOperator != null;

        currentOperator = handler;

        Time.timeScale = focusTimeScale;

        Operator op =
        handler.GetComponent<Operator>();

        EnemyTargetingManager.Instance
            .BeginTargeting(op);

        if (changeOperator)
            infoUI.ChangeOperator(handler);
        else
            infoUI.Show(handler);

        Transform focusPoint = GetFocusPoint(handler.Position);

        if (focusPoint != null)
        {
            targetCameraPosition =
                new Vector3(
                    focusPoint.position.x,
                    focusPoint.position.y,
                    originalCameraPosition.z);

            targetOrthographicSize = GetOrthographicSize(handler.Position);

            targetRotation = Quaternion.Euler(
                GetCameraRotationX(handler.Position),
                GetCameraRotationY(handler.Position),
                originalRotation.eulerAngles.z);
        }
    }

    public void ExitFocus()
    {
        currentOperator = null;

        Time.timeScale = normalTimeScale;

        targetCameraPosition = originalCameraPosition;
        targetOrthographicSize = originalOrthographicSize;

        targetRotation = originalRotation;

        if (infoUI != null)
        {
            infoUI.Hide();
        }

        EnemyTargetingManager.Instance.EndTargeting();
    }

    private Transform GetFocusPoint(OperatorPosition position)
    {
        switch (position)
        {
            case OperatorPosition.Front:
                return frontFocusPoint;

            case OperatorPosition.Middle:
                return middleFocusPoint;

            case OperatorPosition.Back:
                return backFocusPoint;
        }

        return null;
    }

    private float GetOrthographicSize(OperatorPosition position)
    {
        switch (position)
        {
            case OperatorPosition.Front:
                return frontOrthographicSize;

            case OperatorPosition.Middle:
                return middleOrthographicSize;

            case OperatorPosition.Back:
                return backOrthographicSize;
        }

        return originalOrthographicSize;
    }

    private float GetCameraRotationX(OperatorPosition position)
    {
        switch (position)
        {
            case OperatorPosition.Front:
                return frontXRotation;

            case OperatorPosition.Middle:
                return middleXRotation;

            case OperatorPosition.Back:
                return backXRotation;
        }

        return originalRotation.eulerAngles.x;
    }
    private float GetCameraRotationY(OperatorPosition position)
    {
        switch (position)
        {
            case OperatorPosition.Front:
                return frontYRotation;

            case OperatorPosition.Middle:
                return middleYRotation;

            case OperatorPosition.Back:
                return backYRotation;
        }

        return originalRotation.eulerAngles.y;
    }
}