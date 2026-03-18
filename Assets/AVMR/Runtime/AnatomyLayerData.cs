using System;
using UnityEngine;

namespace AVMR.AnatomyViewer
{
    [Serializable]
    public class AnatomyLayerData
    {
        [SerializeField] string displayName;
        [SerializeField] [TextArea(3, 5)] string infoText;
        [SerializeField] GameObject modelPrefab;
        [SerializeField] Vector3 localPositionOffset;
        [SerializeField] Vector3 localRotationOffset;
        [SerializeField] float targetHeightMeters = 1.7f;

        public string DisplayName => displayName;
        public string InfoText => infoText;
        public GameObject ModelPrefab => modelPrefab;
        public Vector3 LocalPositionOffset => localPositionOffset;
        public Quaternion LocalRotationOffset => Quaternion.Euler(localRotationOffset);
        public float TargetHeightMeters => Mathf.Max(0.05f, targetHeightMeters);
    }
}
