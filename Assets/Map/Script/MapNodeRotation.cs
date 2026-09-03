using UnityEngine;

public class MapNodeRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 0.5f;

    [SerializeField] private float maxRotationSpeed = 120f;

    [SerializeField] private float acceleration = 300f;

    [Header("Direction")]
    [SerializeField] private bool invertDirection = false;


    private float targetAngularVelocity;
    private float currentAngularVelocity;


    private void Start()
    {
        if (orbitCamera == null)
        {
            Debug.LogError(
                "MapNodeRotation: " +
                "MapOrbitCamera가 지정되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }


        orbitCamera.OnHorizontalDrag +=
            HandleHorizontalDrag;
    }


    private void OnDestroy()
    {
        if (orbitCamera != null)
        {
            orbitCamera.OnHorizontalDrag -=
                HandleHorizontalDrag;
        }
    }


    private void HandleHorizontalDrag(float horizontal)
    {
        if (invertDirection)
        {
            horizontal = -horizontal;
        }


        /*
         * 마우스 이동량 → 목표 회전 속도
         */
        targetAngularVelocity =
            horizontal *
            rotationSensitivity;


        /*
         * 최고 회전 속도 제한
         */
        targetAngularVelocity =
            Mathf.Clamp(
                targetAngularVelocity,
                -maxRotationSpeed,
                maxRotationSpeed
            );
    }


    private void Update()
    {
        /*
         * 목표 속도를 향해 가속/감속
         */
        currentAngularVelocity =
            Mathf.MoveTowards(
                currentAngularVelocity,
                targetAngularVelocity,
                acceleration *
                Time.deltaTime
            );


        /*
         * 마우스를 멈추면 서서히 정지
         */
        targetAngularVelocity =
            Mathf.MoveTowards(
                targetAngularVelocity,
                0f,
                acceleration *
                Time.deltaTime
            );


        /*
         * Y축 회전
         */
        transform.Rotate(
            0f,
            currentAngularVelocity *
            Time.deltaTime,
            0f,
            Space.Self
        );
    }
}