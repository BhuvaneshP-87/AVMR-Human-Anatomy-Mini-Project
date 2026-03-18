using UnityEngine;

namespace AVMR.AnatomyViewer
{
    public class AnatomyLightingRig : MonoBehaviour
    {
        [SerializeField] Transform followTarget;
        [SerializeField] Light keyLight;
        [SerializeField] Light fillLight;
        [SerializeField] Light rimLight;

        [SerializeField] Vector3 keyOffset = new(0.55f, 1.45f, -0.85f);
        [SerializeField] Vector3 fillOffset = new(-0.8f, 0.95f, 0.7f);
        [SerializeField] Vector3 rimOffset = new(0.1f, 1.15f, 1.1f);

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        void LateUpdate()
        {
            if (followTarget == null)
                return;

            UpdateLight(keyLight, keyOffset, 1.1f, 35f);
            UpdateLight(fillLight, fillOffset, 0.55f, 55f);
            UpdateLight(rimLight, rimOffset, 0.75f, 45f);
        }

        void UpdateLight(Light lightToMove, Vector3 offset, float intensity, float spotAngle)
        {
            if (lightToMove == null)
                return;

            lightToMove.transform.position = followTarget.TransformPoint(offset);
            lightToMove.transform.LookAt(followTarget.position + Vector3.up * 0.8f);
            lightToMove.intensity = intensity;
            lightToMove.spotAngle = spotAngle;
        }
    }
}
