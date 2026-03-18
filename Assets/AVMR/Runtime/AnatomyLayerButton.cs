using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AVMR.AnatomyViewer
{
    public class AnatomyLayerButton : MonoBehaviour
    {
        public enum ButtonAction
        {
            InnerLayer,
            OuterLayer,
            RotateLeft,
            RotateRight
        }

        [SerializeField] AnatomyViewerExperience experience;
        [SerializeField] ButtonAction action;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Image targetImage;
        [SerializeField] Color availableColor = new(0.95f, 0.71f, 0.16f);
        [SerializeField] Color disabledColor = new(0.38f, 0.38f, 0.38f);
        [SerializeField] Color hoverColor = new(1f, 0.86f, 0.33f);

        XRSimpleInteractable m_Interactable;
        Button m_Button;
        bool m_IsAvailable = true;
        MaterialPropertyBlock m_Block;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_Button = GetComponent<Button>();
            m_Block = new MaterialPropertyBlock();

            if (m_Interactable != null)
            {
                m_Interactable.selectEntered.AddListener(OnSelected);
                m_Interactable.hoverEntered.AddListener(_ => ApplyColor(m_IsAvailable ? hoverColor : disabledColor));
                m_Interactable.hoverExited.AddListener(_ => ApplyColor(m_IsAvailable ? availableColor : disabledColor));
            }

            if (m_Button != null)
                m_Button.onClick.AddListener(OnUiClicked);

            ApplyColor(availableColor);
        }

        void OnDestroy()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.RemoveListener(OnSelected);

            if (m_Button != null)
                m_Button.onClick.RemoveListener(OnUiClicked);
        }

        public void SetExperience(AnatomyViewerExperience targetExperience)
        {
            experience = targetExperience;
        }

        public void SetAvailable(bool available)
        {
            m_IsAvailable = available;
            if (m_Interactable != null)
                m_Interactable.enabled = available;
            if (m_Button != null)
                m_Button.interactable = available;

            ApplyColor(available ? availableColor : disabledColor);
        }

        void OnUiClicked()
        {
            Press();
        }

        void OnSelected(SelectEnterEventArgs _)
        {
            Press();
        }

        void Press()
        {
            if (!m_IsAvailable || experience == null)
                return;

            switch (action)
            {
                case ButtonAction.InnerLayer:
                    experience.ShowInnerLayer();
                    break;
                case ButtonAction.OuterLayer:
                    experience.ShowOuterLayer();
                    break;
                case ButtonAction.RotateLeft:
                    experience.RotateCurrentModelLeft();
                    break;
                case ButtonAction.RotateRight:
                    experience.RotateCurrentModelRight();
                    break;
            }
        }

        void ApplyColor(Color color)
        {
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(m_Block);
                m_Block.SetColor("_BaseColor", color);
                m_Block.SetColor("_Color", color);
                targetRenderer.SetPropertyBlock(m_Block);
            }

            if (targetImage != null)
                targetImage.color = color;
        }
    }
}
