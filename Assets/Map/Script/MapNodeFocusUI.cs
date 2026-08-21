using System.Collections;
using UnityEngine;

public class MapNodeFocusObjects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapOrbitCamera orbitCamera;

    [Header("Focus Objects")]
    [SerializeField] private Transform[] choiceObjects;

    [Header("Position")]
    [SerializeField] private float spacing = 2f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float startOffset = 10f;

    private MapNode currentNode;

    private Coroutine animationCoroutine;


    private void Start()
    {
        HideObjects();
    }


    private void Update()
    {
        if (orbitCamera == null)
            return;


        MapNode focusedNode =
            orbitCamera.FocusedNode;


        // Focus가 해제됨
        if (!orbitCamera.IsFocused)
        {
            if (currentNode != null)
            {
                currentNode = null;

                HideObjects();
            }

            return;
        }


        // 새로운 Node Focus
        if (focusedNode != currentNode)
        {
            currentNode =
                focusedNode;

            ShowObjects();
        }
    }


    private void ShowObjects()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine =
            StartCoroutine(
                MoveObjectsIn()
            );
    }


    private void HideObjects()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);

            animationCoroutine = null;
        }


        if (choiceObjects == null)
            return;


        foreach (Transform obj in choiceObjects)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
            }
        }
    }


    private IEnumerator MoveObjectsIn()
    {
        if (currentNode == null)
            yield break;


        Camera camera =
            orbitCamera.GetComponent<Camera>();

        if (camera == null)
            yield break;


        Vector3 nodePosition =
            currentNode.transform.position;


        /*
         * 카메라 기준 화면 오른쪽 방향
         */
        Vector3 right =
            camera.transform.right;


        /*
         * 각 Object의 시작 위치와
         * 목표 위치
         */
        Vector3[] startPositions =
            new Vector3[choiceObjects.Length];

        Vector3[] targetPositions =
            new Vector3[choiceObjects.Length];


        for (int i = 0;
             i < choiceObjects.Length;
             i++)
        {
            Transform obj =
                choiceObjects[i];


            if (obj == null)
                continue;


            obj.gameObject.SetActive(true);


            /*
             * Node를 기준으로 왼쪽에 배치
             */
            Vector3 target =
                nodePosition
                - right *
                ((i + 1) * spacing);


            targetPositions[i] =
                target;


            /*
             * 오른쪽에서 시작
             */
            Vector3 start =
                target
                + right * startOffset;


            startPositions[i] =
                start;


            obj.position =
                start;
        }


        float timer = 0f;


        while (timer < duration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / duration
                );


            /*
             * 부드러운 움직임
             */
            t =
                t * t *
                (3f - 2f * t);


            for (int i = 0;
                 i < choiceObjects.Length;
                 i++)
            {
                Transform obj =
                    choiceObjects[i];


                if (obj == null)
                    continue;


                obj.position =
                    Vector3.Lerp(
                        startPositions[i],
                        targetPositions[i],
                        t
                    );
            }


            yield return null;
        }


        /*
         * 마지막 위치 보정
         */
        for (int i = 0;
             i < choiceObjects.Length;
             i++)
        {
            Transform obj =
                choiceObjects[i];


            if (obj == null)
                continue;


            obj.position =
                targetPositions[i];
        }


        animationCoroutine = null;
    }
}