using UnityEngine;
using UnityEngine.InputSystem;

public class MapOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 40f;
    [SerializeField] private float sensitivity = 0.15f;

    [Header("Pitch Limit")]
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    [Header("Smooth")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Input")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Linked Light")]
    [SerializeField] private Transform linkedLight;
    [SerializeField]
    private Vector3 lightRotationOffset =
        new Vector3(30f, 0f, 0f);

    [Header("Node Focus")]
    [SerializeField] private float focusDistance = 8f;
    [SerializeField] private float focusHeight = 2f;
    [SerializeField] private float focusDuration = 0.6f;

    [SerializeField] private float dragThreshold = 5f;

    public event System.Action<MapNode> OnFocusCanceled;


    public event System.Action<float> OnHorizontalDrag;

    private Vector2 mouseDownPosition;
    private bool mousePressed;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    private bool dragging;

    // 현재 Focus된 노드
    private MapNode focusedNode;

    // Focus 이전 카메라 상태
    private Vector3 previousPosition;
    private Quaternion previousRotation;

    // Focus 애니메이션
    private bool isFocusing;
    private bool returningFromFocus;

    private float focusTimer;

    private Vector3 focusStartPosition;
    private Quaternion focusStartRotation;

    private Vector3 focusTargetPosition;
    private Quaternion focusTargetRotation;


    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "MapOrbitCamera: Target이 지정되지 않았습니다."
            );

            enabled = false;
            return;
        }

        Vector3 offset =
            transform.position - target.position;

        distance = offset.magnitude;

        if (distance < 0.01f)
        {
            distance = 40f;

            offset =
                new Vector3(
                    0f,
                    20f,
                    -distance
                );

            transform.position =
                target.position + offset;
        }

        Vector3 direction =
            offset.normalized;

        targetPitch =
            Mathf.Asin(
                Mathf.Clamp(
                    direction.y,
                    -1f,
                    1f
                )
            ) * Mathf.Rad2Deg;

        targetYaw =
            Mathf.Atan2(
                direction.x,
                direction.z
            ) * Mathf.Rad2Deg;

        currentYaw = targetYaw;
        currentPitch = targetPitch;

        ApplyCamera(
            currentYaw,
            currentPitch
        );
    }


    private void Update()
    {
        if (target == null)
            return;

        // ESC는 Focus 상태에서도 항상 처리
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (focusedNode != null)
            {
                CancelFocus();
            }
        }

        // Focus 이동 중
        if (isFocusing)
        {
            UpdateFocus();
            return;
        }

        // Focus가 완료된 상태
        // 카메라를 완전히 정지시킨다.
        if (focusedNode != null)
        {
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        HandleInput(mouse);

        // 부드러운 회전
        currentYaw =
            Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref yawVelocity,
                smoothTime
            );

        currentPitch =
            Mathf.SmoothDamp(
                currentPitch,
                targetPitch,
                ref pitchVelocity,
                smoothTime
            );

        ApplyCamera(
            currentYaw,
            currentPitch
        );
    }


    private void HandleInput(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            mousePressed = true;
            dragging = false;

            mouseDownPosition =
                mouse.position.ReadValue();
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            mousePressed = false;
            dragging = false;
        }

        if (!mousePressed)
            return;

        Vector2 currentMousePosition =
            mouse.position.ReadValue();

        Vector2 movement =
            currentMousePosition - mouseDownPosition;

        // 일정 거리 이상 움직였을 때만 드래그 시작
        if (!dragging)
        {
            if (movement.sqrMagnitude <
                dragThreshold * dragThreshold)
            {
                return;
            }

            dragging = true;
        }

        Vector2 delta =
            mouse.delta.ReadValue();

        if (delta.sqrMagnitude < 0.0001f)
            return;

        float horizontal = delta.x;
        float vertical = delta.y;

        if (invertX)
            horizontal = -horizontal;

        if (invertY)
            vertical = -vertical;

        OnHorizontalDrag?.Invoke(horizontal);

        targetYaw +=
            horizontal * sensitivity;

        targetPitch -=
            vertical * sensitivity;

        targetPitch =
            Mathf.Clamp(
                targetPitch,
                minPitch,
                maxPitch
            );
    }


    private void ApplyCamera(
        float yaw,
        float pitch)
    {
        float yawRad =
            yaw * Mathf.Deg2Rad;

        float pitchRad =
            pitch * Mathf.Deg2Rad;

        float cosPitch =
            Mathf.Cos(pitchRad);

        Vector3 offset =
            new Vector3(
                Mathf.Sin(yawRad) * cosPitch,
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * cosPitch
            );

        offset *= distance;

        transform.position =
            target.position + offset;

        transform.LookAt(
            target.position,
            Vector3.up
        );

        UpdateLinkedLight();
    }


    private void UpdateLinkedLight()
    {
        if (linkedLight == null)
            return;

        linkedLight.rotation =
            transform.rotation *
            Quaternion.Euler(
                lightRotationOffset
            );
    }


    // =========================================================
    // Node Focus
    // =========================================================

    public void FocusNode(MapNode node)
    {
        if (node == null)
            return;

        // 같은 노드를 다시 클릭하면 취소
        if (focusedNode == node)
        {
            CancelFocus();
            return;
        }

        // 이미 다른 노드가 Focus되어 있다면 무시
        if (focusedNode != null)
            return;

        // 현재 카메라 상태 저장
        previousPosition =
            transform.position;

        previousRotation =
            transform.rotation;

        focusedNode = node;

        focusStartPosition =
            transform.position;

        focusStartRotation =
            transform.rotation;

        Vector3 direction =
            transform.position -
            node.transform.position;

        direction.Normalize();

        focusTargetPosition =
            node.transform.position +
            direction * focusDistance +
            Vector3.up * focusHeight;

        Vector3 lookDirection =
            node.transform.position -
            focusTargetPosition;

        focusTargetRotation =
            Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );

        focusTimer = 0f;

        isFocusing = true;
        returningFromFocus = false;

        dragging = false;
        mousePressed = false;
    }


    private void UpdateFocus()
    {
        focusTimer +=
            Time.deltaTime / focusDuration;

        float t =
            Mathf.Clamp01(focusTimer);

        // SmoothStep
        t =
            t * t * (3f - 2f * t);

        transform.position =
            Vector3.Lerp(
                focusStartPosition,
                focusTargetPosition,
                t
            );

        transform.rotation =
            Quaternion.Slerp(
                focusStartRotation,
                focusTargetRotation,
                t
            );

        UpdateLinkedLight();

        if (t >= 1f)
        {
            isFocusing = false;

            if (returningFromFocus)
            {
                returningFromFocus = false;
                focusedNode = null;
            }
        }
    }


        public void CancelFocus()
    {
        if (focusedNode == null)
            return;

        MapNode canceledNode =
            focusedNode;

        focusStartPosition =
            transform.position;

        focusStartRotation =
            transform.rotation;

        focusTargetPosition =
            previousPosition;

        focusTargetRotation =
            previousRotation;

        focusTimer = 0f;

        isFocusing = true;
        returningFromFocus = true;

        OnFocusCanceled?.Invoke(
            canceledNode
        );
    }


    public bool IsFocused
    {
        get
        {
            return focusedNode != null;
        }
    }

    public bool IsReturningFromFocus
    {
        get
        {
            return returningFromFocus;
        }
    }


    public MapNode FocusedNode
    {
        get
        {
            return focusedNode;
        }
    }
}