using UnityEngine;
using UnityEngine.XR.Templates.MR;

namespace AVMR.AnatomyViewer
{
    public class MRFeatureBootstrap : MonoBehaviour
    {
        [SerializeField] bool enablePassthrough = true;
        [SerializeField] bool enablePlaneDetection = true;
        [SerializeField] bool enablePlaneVisuals = true;
        [SerializeField] bool enableBoundingBoxes = true;
        [SerializeField] bool enableBoundingBoxVisuals = true;
        [SerializeField] bool enableBoundingBoxDebug = true;

        void Start()
        {
            var featureController = FindFirstObjectByType<ARFeatureController>();
            if (featureController == null)
                return;

            featureController.TogglePassthrough(enablePassthrough);
            featureController.TogglePlanes(enablePlaneDetection);
            featureController.TogglePlaneVisualization(enablePlaneVisuals);
            featureController.ToggleBoundingBoxes(enableBoundingBoxes);
            featureController.ToggleBoundingBoxVisualization(enableBoundingBoxVisuals);
            featureController.ToggleDebugInfo(enableBoundingBoxDebug);
        }
    }
}
