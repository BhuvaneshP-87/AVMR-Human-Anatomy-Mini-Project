using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AVMR.AnatomyViewer
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SkeletonBoneSelectable : MonoBehaviour
    {
        SkeletonBoneRaySelector m_Manager;
        string m_GroupId;
        XRSimpleInteractable m_Interactable;

        public void Configure(SkeletonBoneRaySelector manager, string groupId)
        {
            m_Manager = manager;
            m_GroupId = groupId;
        }

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
        }

        void OnEnable()
        {
            if (m_Interactable == null)
                m_Interactable = GetComponent<XRSimpleInteractable>();

            m_Interactable.hoverEntered.AddListener(OnHoverEntered);
            m_Interactable.hoverExited.AddListener(OnHoverExited);
            m_Interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            if (m_Interactable == null)
                return;

            m_Interactable.hoverEntered.RemoveListener(OnHoverEntered);
            m_Interactable.hoverExited.RemoveListener(OnHoverExited);
            m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnHoverEntered(HoverEnterEventArgs _)
        {
            if (m_Manager != null && !string.IsNullOrEmpty(m_GroupId))
                m_Manager.HandleHoverEntered(m_GroupId);
        }

        void OnHoverExited(HoverExitEventArgs _)
        {
            if (m_Manager != null && !string.IsNullOrEmpty(m_GroupId))
                m_Manager.HandleHoverExited(m_GroupId);
        }

        void OnSelectEntered(SelectEnterEventArgs _)
        {
            if (m_Manager != null && !string.IsNullOrEmpty(m_GroupId))
                m_Manager.HandleSelected(m_GroupId);
        }
    }
}
