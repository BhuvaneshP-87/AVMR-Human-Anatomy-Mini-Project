using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace AVMR.AnatomyViewer
{
    public class TableOnlySurfaceVisualizer : MonoBehaviour
    {
        ARPlaneManager m_PlaneManager;
        ARBoundingBoxManager m_BoundingBoxManager;

        void LateUpdate()
        {
            m_PlaneManager ??= FindFirstObjectByType<ARPlaneManager>();
            m_BoundingBoxManager ??= FindFirstObjectByType<ARBoundingBoxManager>();

            if (m_PlaneManager != null)
            {
                foreach (var plane in m_PlaneManager.trackables)
                {
                    var isTable = plane.alignment == PlaneAlignment.HorizontalUp &&
                        (plane.classifications & PlaneClassifications.Table) != 0;
                    SetVisualsEnabled(plane.gameObject, isTable);
                }
            }

            if (m_BoundingBoxManager != null)
            {
                foreach (var box in m_BoundingBoxManager.trackables)
                {
                    var isTable = (box.classifications & BoundingBoxClassifications.Table) != 0;
                    SetVisualsEnabled(box.gameObject, isTable);
                }
            }
        }

        static void SetVisualsEnabled(GameObject root, bool enabled)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = enabled;

            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                canvas.enabled = enabled;

            foreach (var line in root.GetComponentsInChildren<LineRenderer>(true))
                line.enabled = enabled;
        }
    }
}
