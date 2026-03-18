using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace AVMR.AnatomyViewer
{
    public class TableSurfaceBinder : MonoBehaviour
    {
        [SerializeField] DeskAnchorRig anchorRig;
        [SerializeField] float rescanIntervalSeconds = 1f;
        [SerializeField] float minPlaneHeight = 0.55f;
        [SerializeField] float maxPlaneHeight = 1.2f;
        [SerializeField] float minPlaneArea = 0.15f;
        [SerializeField] float minBoundingBoxTopArea = 0.18f;
        [SerializeField] float maxSurfaceSnapDistance = 0.6f;
        [SerializeField] bool logSelection;

        ARBoundingBoxManager m_BoundingBoxManager;
        ARPlaneManager m_PlaneManager;
        Transform m_ResolvedAnchor;
        ARBoundingBox m_BoundBoundingBox;
        ARPlane m_BoundPlane;

        void Reset()
        {
            anchorRig = FindFirstObjectByType<DeskAnchorRig>();
        }

        IEnumerator Start()
        {
            yield return null;
            EnsureResolvedAnchor();
            BindBestSurface();

            while (true)
            {
                if (m_BoundBoundingBox == null && m_BoundPlane == null)
                    BindBestSurface();

                UpdateResolvedAnchor();
                yield return new WaitForSeconds(rescanIntervalSeconds);
            }
        }

        void EnsureManagers()
        {
            m_BoundingBoxManager ??= FindFirstObjectByType<ARBoundingBoxManager>();
            m_PlaneManager ??= FindFirstObjectByType<ARPlaneManager>();
        }

        void EnsureResolvedAnchor()
        {
            if (m_ResolvedAnchor != null)
                return;

            var resolved = new GameObject("ResolvedTableSurface");
            resolved.transform.SetParent(transform, false);
            m_ResolvedAnchor = resolved.transform;

            if (anchorRig != null)
                anchorRig.BindTableSurface(m_ResolvedAnchor);
        }

        public void BindBestSurface()
        {
            EnsureManagers();
            EnsureResolvedAnchor();

            if (anchorRig == null)
                return;

            if (TryGetBestBoundingBox(out var boundingBox))
            {
                m_BoundBoundingBox = boundingBox;
                m_BoundPlane = null;
                UpdateResolvedAnchor();
                if (logSelection)
                    Debug.Log($"Bound anatomy viewer to bounding box table '{boundingBox.name}'.", this);
                return;
            }

            if (TryGetBestPlane(out var plane))
            {
                m_BoundPlane = plane;
                m_BoundBoundingBox = null;
                UpdateResolvedAnchor();
                if (logSelection)
                    Debug.Log($"Bound anatomy viewer to plane table '{plane.name}'.", this);
            }
        }

        public bool TryBindNearestSurface(Vector3 worldPosition)
        {
            if (!TryGetNearestSurfacePose(worldPosition, out _, out _, out var nearestBox, out var nearestPlane))
                return false;

            m_BoundBoundingBox = nearestBox;
            m_BoundPlane = nearestPlane;
            UpdateResolvedAnchor();
            return true;
        }

        public bool TryGetNearestSurfacePose(Vector3 worldPosition, out Vector3 surfacePosition, out Quaternion surfaceRotation)
        {
            return TryGetNearestSurfacePose(worldPosition, out surfacePosition, out surfaceRotation, out _, out _);
        }

        public bool TryGetWallPose(int seed, out Vector3 surfacePosition, out Quaternion surfaceRotation)
        {
            EnsureManagers();
            EnsureResolvedAnchor();

            var wallPlanes = new System.Collections.Generic.List<ARPlane>();
            if (m_PlaneManager != null)
            {
                foreach (var trackedPlane in m_PlaneManager.trackables)
                {
                    if (!IsWallPlane(trackedPlane))
                        continue;

                    wallPlanes.Add(trackedPlane);
                }
            }

            if (wallPlanes.Count == 0)
            {
                surfacePosition = default;
                surfaceRotation = Quaternion.identity;
                return false;
            }

            var selectedPlane = wallPlanes[Mathf.Abs(seed) % wallPlanes.Count];
            var halfWidth = Mathf.Max(0.08f, selectedPlane.size.x * 0.35f);
            var halfHeight = Mathf.Max(0.12f, selectedPlane.size.y * 0.30f);

            var hashA = Mathf.Abs(seed * 0.6180339f);
            var hashB = Mathf.Abs(seed * 1.3247179f);
            var xOffset = Mathf.Lerp(-halfWidth, halfWidth, hashA - Mathf.Floor(hashA));
            var yOffset = Mathf.Lerp(-halfHeight, halfHeight, hashB - Mathf.Floor(hashB));

            surfacePosition =
                selectedPlane.transform.position +
                selectedPlane.transform.right * xOffset +
                Vector3.up * yOffset +
                selectedPlane.transform.forward * 0.02f;
            surfaceRotation = GetWallRotation(selectedPlane.transform);
            return true;
        }

        bool TryGetNearestSurfacePose(Vector3 worldPosition, out Vector3 surfacePosition, out Quaternion surfaceRotation, out ARBoundingBox nearestBox, out ARPlane nearestPlane)
        {
            EnsureManagers();
            EnsureResolvedAnchor();

            var bestDistance = maxSurfaceSnapDistance;
            nearestBox = null;
            nearestPlane = null;
            surfacePosition = default;
            surfaceRotation = Quaternion.identity;

            if (m_BoundingBoxManager != null)
            {
                foreach (var box in m_BoundingBoxManager.trackables)
                {
                    if ((box.classifications & BoundingBoxClassifications.Table) == 0)
                        continue;

                    var surfacePoint = box.transform.position + box.transform.up * (box.size.y * 0.5f);
                    var distance = Vector3.Distance(worldPosition, surfacePoint);
                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    nearestBox = box;
                    nearestPlane = null;
                    surfacePosition = surfacePoint;
                    surfaceRotation = GetSurfaceRotation(box.transform);
                }
            }

            if (m_PlaneManager != null)
            {
                foreach (var plane in m_PlaneManager.trackables)
                {
                    var classifications = plane.classifications;
                    var isHorizontal = plane.alignment == PlaneAlignment.HorizontalUp;
                    var isTable = (classifications & PlaneClassifications.Table) != 0;
                    if (!isHorizontal || !isTable)
                        continue;

                    var distance = Vector3.Distance(worldPosition, plane.transform.position);
                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    nearestPlane = plane;
                    nearestBox = null;
                    surfacePosition = plane.transform.position;
                    surfaceRotation = GetSurfaceRotation(plane.transform);
                }
            }

            return nearestBox != null || nearestPlane != null;
        }

        void UpdateResolvedAnchor()
        {
            if (m_ResolvedAnchor == null)
                return;

            if (m_BoundBoundingBox != null)
            {
                var size = m_BoundBoundingBox.size;
                var topCenter = m_BoundBoundingBox.transform.position + m_BoundBoundingBox.transform.up * (size.y * 0.5f);
                m_ResolvedAnchor.SetPositionAndRotation(topCenter, GetSurfaceRotation(m_BoundBoundingBox.transform));
                return;
            }

            if (m_BoundPlane != null)
            {
                m_ResolvedAnchor.SetPositionAndRotation(m_BoundPlane.transform.position, GetSurfaceRotation(m_BoundPlane.transform));
            }
        }

        bool TryGetBestBoundingBox(out ARBoundingBox best)
        {
            best = null;
            if (m_BoundingBoxManager == null)
                return false;

            var bestArea = 0f;
            foreach (var box in m_BoundingBoxManager.trackables)
            {
                if ((box.classifications & BoundingBoxClassifications.Table) == 0)
                    continue;

                var area = box.size.x * box.size.z;
                if (area < minBoundingBoxTopArea || area <= bestArea)
                    continue;

                best = box;
                bestArea = area;
            }

            return best != null;
        }

        bool TryGetBestPlane(out ARPlane best)
        {
            best = null;
            if (m_PlaneManager == null)
                return false;

            var bestArea = 0f;
            foreach (var plane in m_PlaneManager.trackables)
            {
                var classifications = plane.classifications;
                var isTable = (classifications & PlaneClassifications.Table) != 0;
                var isHorizontal = plane.alignment == PlaneAlignment.HorizontalUp;
                if (!isHorizontal || !isTable)
                    continue;

                var planeArea = plane.size.x * plane.size.y;
                var height = plane.transform.position.y;
                if (planeArea < minPlaneArea || height < minPlaneHeight || height > maxPlaneHeight || planeArea <= bestArea)
                    continue;

                best = plane;
                bestArea = planeArea;
            }

            return best != null;
        }

        static Quaternion GetSurfaceRotation(Transform source)
        {
            var forward = Vector3.ProjectOnPlane(source.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        static Quaternion GetWallRotation(Transform source)
        {
            var outward = source.forward;
            if (Vector3.Dot(outward, Vector3.forward) < 0f)
                outward = -outward;

            return Quaternion.LookRotation(outward.normalized, Vector3.up);
        }

        static bool IsWallPlane(ARPlane plane)
        {
            if (plane == null)
                return false;

            var isVertical = plane.alignment == PlaneAlignment.Vertical;
            if (!isVertical)
                return false;

            return true;
        }
    }
}
