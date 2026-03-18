using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class InspectRaySetup
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/AVMRAnatomyViewer.unity");
        foreach (var interactor in Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"RAY {interactor.name} mask={interactor.interactionLayers.value} raycastMask={interactor.raycastMask.value} hitClosest={interactor.hitClosestOnly} enabled={interactor.enabled}");
        }

        foreach (var interactable in Object.FindObjectsByType<XRSimpleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"SIMPLE {interactable.name} mask={interactable.interactionLayers.value} enabled={interactable.enabled}");
        }

        foreach (var interactable in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"GRAB {interactable.name} mask={interactable.interactionLayers.value} enabled={interactable.enabled}");
        }
    }
}
