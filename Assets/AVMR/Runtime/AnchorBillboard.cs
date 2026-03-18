using UnityEngine;

namespace AVMR.AnatomyViewer
{
    public class AnchorBillboard : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] bool lockYRotationOnly = true;

        void LateUpdate()
        {
            if (cameraTransform == null || !cameraTransform.gameObject.activeInHierarchy)
            {
                var mainCamera = Camera.main;
                if (mainCamera == null)
                    return;

                cameraTransform = mainCamera.transform;
            }

            var direction = cameraTransform.position - transform.position;
            if (lockYRotationOnly)
                direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
