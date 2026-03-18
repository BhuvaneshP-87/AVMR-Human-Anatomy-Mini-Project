using UnityEngine;

namespace AVMR.AnatomyViewer
{
    public class DeskAnchorRig : MonoBehaviour
    {
        [Header("Resolved Scene Anchors")]
        [SerializeField] Transform tableSurfaceAnchor;
        [SerializeField] Transform modelAnchor;
        [SerializeField] Transform panelAnchor;

        [Header("Layout")]
        [SerializeField] Vector3 modelLocalOffset = new(0f, 0.02f, 0f);
        [SerializeField] Vector3 panelLocalOffset = new(1.0f, 0.32f, -0.04f);
        [SerializeField] Vector3 panelEulerOffset = new(0f, -18f, 0f);

        public Transform ModelAnchor => modelAnchor;
        public Transform PanelAnchor => panelAnchor;

        void Reset()
        {
            modelAnchor = transform.Find("ModelAnchor");
            panelAnchor = transform.Find("PanelAnchor");
        }

        void LateUpdate()
        {
            if (tableSurfaceAnchor == null)
                return;

            if (modelAnchor != null)
            {
                modelAnchor.SetPositionAndRotation(
                    tableSurfaceAnchor.TransformPoint(modelLocalOffset),
                    tableSurfaceAnchor.rotation);
            }

            if (panelAnchor != null)
            {
                panelAnchor.SetPositionAndRotation(
                    tableSurfaceAnchor.TransformPoint(panelLocalOffset),
                    tableSurfaceAnchor.rotation * Quaternion.Euler(panelEulerOffset));
            }
        }

        public void BindTableSurface(Transform resolvedTableAnchor)
        {
            tableSurfaceAnchor = resolvedTableAnchor;
        }
    }
}
