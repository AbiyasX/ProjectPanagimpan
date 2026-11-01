using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public MapNodes[] m_MapNodes;
    public GameObject Parent;

    [Header("Travel")]
    public GameObject TravelObject;
    public float moveDurationPerNode = 0.5f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Vector3 travelOffset = new Vector3(0, 0, -1);

    [ReadOnly] public GameObject travelerInstance;

    public float verticalSpacing = 150f;
    public float horizontalOffset = 250f;

    [Header("Curve")]
    [Range(0f, 1f)] public float curveStrength = 0.5f;
    public int lineSegmentsPerNode = 20;

    [Header("Line")]
    public LineRenderer lineRenderer;
    public float lineWidth = 5f;
    public Color lineColor = Color.white;
    public Material lineMaterial;

    [Header("Camera")]
    public CameraFollow cameraFollow;
    public int currentNodeIndex = 0;

    [Header("Show Map")]
    public GameObject mapView;
    public float delayBeforeTravel = 0.5f;
    public float delayAfterTravel = 1f;

    [HideInInspector] public List<Transform> spawnedNodes = new List<Transform>();

    bool isMoving;
    Coroutine moveRoutine;

    public bool IsMoving => isMoving;
    public int NodeCount => spawnedNodes.Count;

    public void ShowTravel(int nodeIndex, System.Action onFinished)
    {
        if (mapView != null)
            mapView.SetActive(true);

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("MapSystem: this object is turned off, so the map can't animate. Put MapSystem on an object that stays on, or inside the Map View.", this);
            FocusNode(nodeIndex, true);
            onFinished?.Invoke();
            return;
        }

        StartCoroutine(TravelRoutine(nodeIndex, onFinished));
    }

    IEnumerator TravelRoutine(int nodeIndex, System.Action onFinished)
    {
        yield return new WaitForSeconds(delayBeforeTravel);

        FocusNode(nodeIndex, false);
        while (isMoving)
            yield return null;

        yield return new WaitForSeconds(delayAfterTravel);

        if (mapView != null)
            mapView.SetActive(false);

        onFinished?.Invoke();
    }

    [Button]
    public void GenerateAllNodes()
    {
        if (m_MapNodes == null || m_MapNodes.Length == 0) return;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        isMoving = false;

        for (int c = Parent.transform.childCount - 1; c >= 0; c--)
            DestroyImmediate(Parent.transform.GetChild(c).gameObject);

        spawnedNodes.Clear();
        Vector3[] points = new Vector3[m_MapNodes.Length];

        int lastIndex = m_MapNodes.Length - 1;

        for (int i = 0; i < m_MapNodes.Length; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = (i == lastIndex) ? 0f : side * horizontalOffset;
            float y = verticalSpacing * i;

            GameObject node = Instantiate(m_MapNodes[i].Model, Parent.transform);
            node.transform.localRotation = Quaternion.identity;

            if (node.transform is RectTransform rect)
                rect.anchoredPosition = new Vector2(x, y);
            else
                node.transform.localPosition = new Vector3(x, y, 0f);

            spawnedNodes.Add(node.transform);
            points[i] = node.transform.position;
        }

        DrawLine(points);
        SpawnTraveler();

        currentNodeIndex = 0;
        FocusNode(0, true);
    }

    void SpawnTraveler()
    {
        travelerInstance = null;
        if (TravelObject == null) return;

        travelerInstance = Instantiate(TravelObject, Parent.transform);
        travelerInstance.name = "Traveler";
        travelerInstance.transform.localRotation = Quaternion.identity;
        travelerInstance.transform.position = spawnedNodes[0].position + travelOffset;
    }

    [Button]
    public void NextNode() => FocusNode(currentNodeIndex + 1, false);

    [Button]
    public void PreviousNode() => FocusNode(currentNodeIndex - 1, false);

    public void FocusNode(int index, bool snap)
    {
        if (spawnedNodes.Count == 0) return;
        if (isMoving) return;

        int targetIndex = Mathf.Clamp(index, 0, spawnedNodes.Count - 1);

        if (!Application.isPlaying) snap = true;

        if (travelerInstance != null)
        {
            if (snap)
            {
                if (moveRoutine != null) StopCoroutine(moveRoutine);
                isMoving = false;
                travelerInstance.transform.position = spawnedNodes[targetIndex].position + travelOffset;
            }
            else if (targetIndex != currentNodeIndex)
            {
                List<int> path = new List<int>();
                int step = targetIndex > currentNodeIndex ? 1 : -1;
                for (int i = currentNodeIndex; i != targetIndex + step; i += step)
                    path.Add(i);

                moveRoutine = StartCoroutine(MoveAlong(path));
            }
        }

        currentNodeIndex = targetIndex;

        if (cameraFollow != null)
        {
            Transform camTarget = travelerInstance != null ? travelerInstance.transform : spawnedNodes[targetIndex];
            cameraFollow.SetTarget(camTarget, snap);
        }
    }

    IEnumerator MoveAlong(List<int> path)
    {
        isMoving = true;
        Transform t = travelerInstance.transform;

        for (int p = 0; p < path.Count - 1; p++)
        {
            int a = path[p];
            int b = path[p + 1];

            Vector3 from = spawnedNodes[a].position;
            Vector3 to = spawnedNodes[b].position;

            int low = Mathf.Min(a, b);
            int high = Mathf.Max(a, b);
            Vector3 control = GetControlPoint(spawnedNodes[low].position, spawnedNodes[high].position);

            float progress = 0f;

            while (progress < 1f)
            {
                progress += Time.deltaTime / moveDurationPerNode;
                float e = ease.Evaluate(Mathf.Clamp01(progress));
                t.position = QuadraticLerp(from, control, to, e) + travelOffset;
                yield return null;
            }

            t.position = to + travelOffset;
        }

        isMoving = false;
        moveRoutine = null;
    }


    Vector3 QuadraticLerp(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 ab = Vector3.Lerp(a, b, t);
        Vector3 bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }

    Vector3 GetControlPoint(Vector3 from, Vector3 to)
    {
        Vector3 mid = (from + to) * 0.5f;
        Vector3 corner = new Vector3(from.x, to.y, from.z);
        return Vector3.Lerp(mid, corner, curveStrength);
    }


    void DrawLine(Vector3[] points)
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        List<Vector3> curvePoints = new List<Vector3>();
        if (points.Length > 0) curvePoints.Add(points[0]);

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 control = GetControlPoint(points[i], points[i + 1]);

            for (int s = 1; s <= lineSegmentsPerNode; s++)
            {
                float t = s / (float)lineSegmentsPerNode;
                curvePoints.Add(QuadraticLerp(points[i], control, points[i + 1], t));
            }
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        if (lineMaterial != null)
            lineRenderer.sharedMaterial = lineMaterial;
        else if (lineRenderer.sharedMaterial == null)
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }
}