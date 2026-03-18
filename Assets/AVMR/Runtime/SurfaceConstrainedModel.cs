using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AVMR.AnatomyViewer
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class SurfaceConstrainedModel : MonoBehaviour
    {
        [SerializeField] DeskAnchorRig anchorRig;
        [SerializeField] TableSurfaceBinder tableSurfaceBinder;
        [SerializeField] Vector3 anchoredLocalPosition;
        [SerializeField] Vector3 anchoredLocalEulerAngles;
        [SerializeField] float returnDuration = 0.16f;
        [SerializeField] float hoverHeight = 0.02f;

        XRGrabInteractable m_GrabInteractable;
        Rigidbody m_Rigidbody;
        Coroutine m_ReturnRoutine;

        void Awake()
        {
            m_GrabInteractable = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            if (m_GrabInteractable == null)
                m_GrabInteractable = GetComponent<XRGrabInteractable>();

            m_GrabInteractable.selectEntered.AddListener(OnSelected);
            m_GrabInteractable.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            if (m_GrabInteractable == null)
                return;

            m_GrabInteractable.selectEntered.RemoveListener(OnSelected);
            m_GrabInteractable.selectExited.RemoveListener(OnReleased);
        }

        void Update()
        {
            if (m_GrabInteractable == null || !m_GrabInteractable.isSelected || tableSurfaceBinder == null)
                return;

            if (!tableSurfaceBinder.TryGetNearestSurfacePose(transform.position, out var surfacePosition, out _))
                return;

            var clamped = transform.position;
            clamped.y = surfacePosition.y + hoverHeight;
            transform.position = clamped;
        }

        public void Configure(DeskAnchorRig rig, TableSurfaceBinder binder, Vector3 localPosition, Quaternion localRotation)
        {
            anchorRig = rig;
            tableSurfaceBinder = binder;
            anchoredLocalPosition = localPosition;
            anchoredLocalEulerAngles = localRotation.eulerAngles;
            ResetToAnchorImmediate();
        }

        public void SetAnchoredLocalRotation(Quaternion localRotation)
        {
            anchoredLocalEulerAngles = localRotation.eulerAngles;
        }

        public void SetAnchoredPose(Vector3 localPosition, Quaternion localRotation)
        {
            anchoredLocalPosition = localPosition;
            anchoredLocalEulerAngles = localRotation.eulerAngles;
        }

        void OnSelected(SelectEnterEventArgs _)
        {
            if (m_ReturnRoutine != null)
            {
                StopCoroutine(m_ReturnRoutine);
                m_ReturnRoutine = null;
            }
        }

        void OnReleased(SelectExitEventArgs _)
        {
            if (tableSurfaceBinder != null)
                tableSurfaceBinder.TryBindNearestSurface(transform.position);

            if (m_ReturnRoutine != null)
                StopCoroutine(m_ReturnRoutine);

            m_ReturnRoutine = StartCoroutine(ReturnToAnchorRoutine());
        }

        IEnumerator ReturnToAnchorRoutine()
        {
            yield return null;

            if (anchorRig == null || anchorRig.ModelAnchor == null)
                yield break;

            transform.SetParent(anchorRig.ModelAnchor, true);

            var startPosition = transform.localPosition;
            var startRotation = transform.localRotation;
            var targetPosition = anchoredLocalPosition;
            var targetRotation = Quaternion.Euler(anchoredLocalEulerAngles);
            var elapsed = 0f;

            ZeroBodyMotion();

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / returnDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
                transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                yield return null;
            }

            ResetToAnchorImmediate();
            m_ReturnRoutine = null;
        }

        void ResetToAnchorImmediate()
        {
            if (anchorRig == null || anchorRig.ModelAnchor == null)
                return;

            transform.SetParent(anchorRig.ModelAnchor, false);
            transform.localPosition = anchoredLocalPosition;
            transform.localRotation = Quaternion.Euler(anchoredLocalEulerAngles);
            ZeroBodyMotion();
        }

        void ZeroBodyMotion()
        {
            if (m_Rigidbody == null)
                m_Rigidbody = GetComponent<Rigidbody>();

            if (m_Rigidbody == null)
                return;

            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
