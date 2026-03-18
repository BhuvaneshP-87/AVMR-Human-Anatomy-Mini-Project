using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace AVMR.AnatomyViewer
{
    public class AnatomyViewerExperience : MonoBehaviour
    {
        [SerializeField] DeskAnchorRig anchorRig;
        [SerializeField] AnatomyInfoPanelController infoPanel;
        [SerializeField] AnatomyLightingRig lightingRig;
        [SerializeField] TableSurfaceBinder tableSurfaceBinder;
        [SerializeField] AnatomyLayerButton innerLayerButton;
        [SerializeField] AnatomyLayerButton outerLayerButton;
        [SerializeField] List<AnatomyLayerData> layers = new();

        int m_CurrentLayerIndex;
        GameObject m_ActiveInstance;
        bool m_LayerSwitchLocked;

        void Start()
        {
            if (innerLayerButton != null)
                innerLayerButton.SetExperience(this);

            if (outerLayerButton != null)
                outerLayerButton.SetExperience(this);

            ShowLayer(0);
        }

        public void ShowInnerLayer()
        {
            if (m_LayerSwitchLocked)
                return;

            if (m_CurrentLayerIndex < layers.Count - 1)
                ShowLayer(m_CurrentLayerIndex + 1);
        }

        public void ShowOuterLayer()
        {
            if (m_LayerSwitchLocked)
                return;

            if (m_CurrentLayerIndex > 0)
                ShowLayer(m_CurrentLayerIndex - 1);
        }

        public void RotateCurrentModelLeft()
        {
            RotateCurrentModel(-20f);
        }

        public void RotateCurrentModelRight()
        {
            RotateCurrentModel(20f);
        }

        void ShowLayer(int index)
        {
            if (layers.Count == 0 || index < 0 || index >= layers.Count)
                return;

            m_CurrentLayerIndex = index;

            if (m_ActiveInstance != null)
                Destroy(m_ActiveInstance);

            m_LayerSwitchLocked = false;

            var layer = layers[index];
            if (layer.ModelPrefab != null && anchorRig != null && anchorRig.ModelAnchor != null)
            {
                m_ActiveInstance = Instantiate(layer.ModelPrefab, anchorRig.ModelAnchor);
                m_ActiveInstance.name = layer.DisplayName;
                m_ActiveInstance.transform.localPosition = layer.LocalPositionOffset;
                m_ActiveInstance.transform.localRotation = layer.LocalRotationOffset;
                FitToTargetHeight(m_ActiveInstance.transform, m_ActiveInstance.transform, layer.TargetHeightMeters);
                if (layer.DisplayName == "Skeleton Layer")
                {
                    PrepareSkeletonBoneSelection(m_ActiveInstance, layer);
                }
                else
                {
                    EnsureGrabInteractable(m_ActiveInstance);
                    EnsureConstrainedSurfaceModel(m_ActiveInstance, layer);
                }

                if (anchorRig != null && anchorRig.ModelAnchor != null)
                    PlantFeetOnAnchor(m_ActiveInstance.transform, anchorRig.ModelAnchor.position.y);

                if (lightingRig != null)
                    lightingRig.SetFollowTarget(m_ActiveInstance.transform);
            }

            if (infoPanel != null)
                infoPanel.UpdatePanel(layer.DisplayName, layer.InfoText);

            if (innerLayerButton != null)
                innerLayerButton.SetAvailable(index < layers.Count - 1);
            if (outerLayerButton != null)
                outerLayerButton.SetAvailable(index > 0);
        }

        public void SetLayerSwitchLocked(bool locked)
        {
            m_LayerSwitchLocked = locked;

            if (innerLayerButton != null)
                innerLayerButton.SetAvailable(!locked && m_CurrentLayerIndex < layers.Count - 1);
            if (outerLayerButton != null)
                outerLayerButton.SetAvailable(!locked && m_CurrentLayerIndex > 0);
        }

        static void FitToTargetHeight(Transform scaleRoot, Transform boundsRoot, float targetHeightMeters)
        {
            if (scaleRoot == null || boundsRoot == null)
                return;

            if (!TryGetWorldBounds(boundsRoot, out var bounds))
                return;

            var currentHeight = Mathf.Max(0.001f, bounds.size.y);
            var scaleMultiplier = targetHeightMeters / currentHeight;
            scaleRoot.localScale *= scaleMultiplier;

            if (TryGetWorldBounds(boundsRoot, out bounds))
            {
                var raiseAmount = -bounds.min.y;
                scaleRoot.position += Vector3.up * raiseAmount;
            }
        }

        static void PlantFeetOnAnchor(Transform root, float anchorY)
        {
            if (root == null)
                return;

            if (!TryGetWorldBounds(root, out var bounds))
                return;

            var correctionY = anchorY - bounds.min.y;
            root.position += Vector3.up * correctionY;
        }

        static void EnsureGrabInteractable(GameObject root)
        {
            var collider = root.GetComponent<Collider>();
            if (collider == null && TryGetWorldBounds(root.transform, out var bounds))
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = root.transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size;
                collider = box;
            }

            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = root.AddComponent<Rigidbody>();

            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.constraints = RigidbodyConstraints.None;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.linearDamping = 8f;
            rigidbody.angularDamping = 12f;

            var grabInteractable = root.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                grabInteractable = root.AddComponent<XRGrabInteractable>();

            var transformer = root.GetComponent<XRGeneralGrabTransformer>();
            if (transformer == null)
                transformer = root.AddComponent<XRGeneralGrabTransformer>();

            transformer.permittedDisplacementAxes = XRGeneralGrabTransformer.ManipulationAxes.All;
            transformer.constrainedAxisDisplacementMode = XRGeneralGrabTransformer.ConstrainedAxisDisplacementMode.WorldAxisRelative;
            transformer.allowOneHandedScaling = false;
            transformer.allowTwoHandedScaling = true;
            transformer.minimumScaleRatio = 0.75f;
            transformer.maximumScaleRatio = 1.5f;
            transformer.thresholdMoveRatioForScale = 0.02f;

            grabInteractable.selectMode = InteractableSelectMode.Multiple;
            grabInteractable.useDynamicAttach = true;
            grabInteractable.matchAttachPosition = true;
            grabInteractable.matchAttachRotation = true;
            grabInteractable.snapToColliderVolume = true;
            grabInteractable.reinitializeDynamicAttachEverySingleGrab = true;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.throwOnDetach = false;
            grabInteractable.trackPosition = true;
            grabInteractable.trackRotation = true;
            grabInteractable.smoothPosition = true;
            grabInteractable.smoothPositionAmount = 0.08f;
            grabInteractable.tightenPosition = 0.85f;
            grabInteractable.smoothRotation = true;
            grabInteractable.smoothRotationAmount = 0.1f;
            grabInteractable.tightenRotation = 0.85f;
            grabInteractable.attachEaseInTime = 0.05f;
        }

        void EnsureConstrainedSurfaceModel(GameObject root, AnatomyLayerData layer)
        {
            var constrainedModel = root.GetComponent<SurfaceConstrainedModel>();
            if (constrainedModel == null)
                constrainedModel = root.AddComponent<SurfaceConstrainedModel>();

            constrainedModel.Configure(anchorRig, tableSurfaceBinder, layer.LocalPositionOffset, layer.LocalRotationOffset);
        }

        void PrepareSkeletonBoneSelection(GameObject root, AnatomyLayerData layer)
        {
            foreach (var collider in root.GetComponents<Collider>())
            {
                collider.enabled = false;
                Destroy(collider);
            }

            var grabInteractable = root.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
                Destroy(grabInteractable);
            }

            var transformer = root.GetComponent<XRGeneralGrabTransformer>();
            if (transformer != null)
            {
                transformer.enabled = false;
                Destroy(transformer);
            }

            var constrainedModel = root.GetComponent<SurfaceConstrainedModel>();
            if (constrainedModel != null)
            {
                constrainedModel.enabled = false;
                Destroy(constrainedModel);
            }

            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody != null)
                Destroy(rigidbody);

            var selector = root.GetComponent<SkeletonBoneRaySelector>();
            if (selector == null)
                selector = root.AddComponent<SkeletonBoneRaySelector>();

            selector.Configure(infoPanel, layer.DisplayName, layer.InfoText);
        }

        void RotateCurrentModel(float yawDegrees)
        {
            if (m_ActiveInstance == null)
                return;

            if (!TryGetWorldBounds(m_ActiveInstance.transform, out var beforeBounds))
                return;

            var layerName = m_CurrentLayerIndex >= 0 && m_CurrentLayerIndex < layers.Count
                ? layers[m_CurrentLayerIndex].DisplayName
                : string.Empty;
            var preserveFullBasePoint =
                layerName == "Skin Layer" ||
                layerName == "Muscle Layer" ||
                layerName == "Anatomy Layer";

            var plantedHeight = beforeBounds.min.y;
            var plantedPoint = new Vector3(beforeBounds.center.x, beforeBounds.min.y, beforeBounds.center.z);
            m_ActiveInstance.transform.localRotation *= Quaternion.Euler(0f, yawDegrees, 0f);

            if (TryGetWorldBounds(m_ActiveInstance.transform, out var afterBounds))
            {
                if (preserveFullBasePoint)
                {
                    var rotatedPoint = new Vector3(afterBounds.center.x, afterBounds.min.y, afterBounds.center.z);
                    m_ActiveInstance.transform.position += plantedPoint - rotatedPoint;
                }
                else
                {
                    var correctionY = plantedHeight - afterBounds.min.y;
                    m_ActiveInstance.transform.position += Vector3.up * correctionY;
                }
            }

            var constrainedModel = m_ActiveInstance.GetComponent<SurfaceConstrainedModel>();
            if (constrainedModel != null)
                constrainedModel.SetAnchoredPose(m_ActiveInstance.transform.localPosition, m_ActiveInstance.transform.localRotation);
        }

        static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }
    }
}
