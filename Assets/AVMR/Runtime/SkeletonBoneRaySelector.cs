using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AVMR.AnatomyViewer
{
    public class SkeletonBoneRaySelector : MonoBehaviour
    {
        [Serializable]
        class BoneDefinition
        {
            public string Id;
            public string DisplayName;
            public string Description;
            public string[] ExactNames;
            public string[] PrefixNames;
        }

        class BoneGroup
        {
            public string Id;
            public string Title;
            public string Description;
            public readonly List<Renderer> Renderers = new();
            public readonly List<Transform> Targets = new();
            public readonly List<Collider> Colliders = new();
            public readonly List<XRSimpleInteractable> Interactables = new();
            public readonly List<SkeletonBoneSelectable> Selectables = new();
        }

        [SerializeField] AnatomyInfoPanelController infoPanel;
        [SerializeField] string defaultTitle;
        [SerializeField] string defaultBody;
        [SerializeField] Color hoverColor = new(1f, 0.9f, 0.35f, 1f);
        [SerializeField] Color selectedColor = new(1f, 0.82f, 0.12f, 1f);
        [SerializeField] float boxPadding = 0.62f;
        [SerializeField] float minimumColliderSize = 0.002f;
        [SerializeField] int longAxisSegments = 3;
        [SerializeField] float extractOffset = 0.14f;

        readonly Dictionary<string, BoneGroup> m_Groups = new();
        BoneGroup m_HoveredGroup;
        BoneGroup m_SelectedGroup;
        MaterialPropertyBlock m_HighlightBlock;

        static readonly BoneDefinition[] k_Bones =
        {
            new() { Id = "cranium", DisplayName = "Cranium", Description = "Protects the brain.", ExactNames = new[] { "SK_Cranium" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "mandible", DisplayName = "Mandible", Description = "Lower jaw for chewing.", ExactNames = new[] { "SK_Mandible", "Jaw" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "clavicle", DisplayName = "Clavicle", Description = "Braces the shoulder.", ExactNames = new[] { "SK_Clavicle" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "scapula", DisplayName = "Scapula", Description = "Shoulder blade anchor.", ExactNames = new[] { "SK_Scapula" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "ribcage", DisplayName = "Rib Cage", Description = "Protects heart and lungs.", ExactNames = new[] { "SK_RibCage", "RibCage" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "spine", DisplayName = "Spine", Description = "Supports trunk and spinal cord.", ExactNames = new[] { "SK_Spine", "Spine" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "pelvis", DisplayName = "Pelvis", Description = "Transfers body weight.", ExactNames = new[] { "SK_Pelvis", "Pelvis" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "humerus", DisplayName = "Humerus", Description = "Upper arm bone.", ExactNames = new[] { "SK_Humerus" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "radius", DisplayName = "Radius", Description = "Thumb-side forearm bone.", ExactNames = new[] { "SK_Radius" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "ulna", DisplayName = "Ulna", Description = "Little-finger forearm bone.", ExactNames = new[] { "SK_Ulna" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "carpals", DisplayName = "Carpals", Description = "Wrist bones for mobility.", ExactNames = new[] { "SK_Carpals" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "femur", DisplayName = "Femur", Description = "Primary thigh support bone.", ExactNames = new[] { "SK_Femur" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "patella", DisplayName = "Patella", Description = "Shields the knee joint.", ExactNames = new[] { "SK_Patella" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "tibia", DisplayName = "Tibia", Description = "Main weight-bearing shin bone.", ExactNames = new[] { "SK_Tibia" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "fibula", DisplayName = "Fibula", Description = "Stabilizes the outer leg.", ExactNames = new[] { "SK_Fibula" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "tarsals", DisplayName = "Tarsals", Description = "Ankle bones aid balance.", ExactNames = new[] { "SK_Tarsal" }, PrefixNames = Array.Empty<string>() },
            new() { Id = "metatarsals", DisplayName = "Metatarsals", Description = "Bridge ankle to toes.", ExactNames = Array.Empty<string>(), PrefixNames = new[] { "SK_Metatarsal" } },
            new() { Id = "phalanges", DisplayName = "Phalanges", Description = "Finger and toe bones.", ExactNames = Array.Empty<string>(), PrefixNames = new[] { "SK_Phalange" } },
        };

        public void Configure(AnatomyInfoPanelController panel, string layerTitle, string layerBody)
        {
            infoPanel = panel;
            defaultTitle = layerTitle;
            defaultBody = layerBody;
        }

        void Awake()
        {
            m_HighlightBlock = new MaterialPropertyBlock();
            BuildBoneTargets();
            ShowDefaultInfo();
        }

        void OnDisable()
        {
            m_HoveredGroup = null;
            SetSelectedGroup(null);
        }

        void BuildBoneTargets()
        {
            var assembledRoot = FindAssembledSkeletonRoot();
            if (assembledRoot == null)
                return;

            foreach (var definition in k_Bones)
            {
                var group = new BoneGroup
                {
                    Id = definition.Id,
                    Title = definition.DisplayName,
                    Description = definition.Description,
                };

                foreach (var boneTransform in FindMatchingTransforms(assembledRoot, definition))
                {
                    if (!boneTransform.gameObject.activeInHierarchy)
                        continue;

                    // Prefer a direct renderer; fall back to subtree renderers for container nodes
                    // whose primary child mesh was stripped from the prefab.
                    var directRenderer = boneTransform.GetComponent<Renderer>();
                    if (directRenderer != null)
                    {
                        if (!group.Renderers.Contains(directRenderer))
                            group.Renderers.Add(directRenderer);
                    }
                    else
                    {
                        foreach (var r in boneTransform.GetComponentsInChildren<Renderer>(false))
                            if (!group.Renderers.Contains(r))
                                group.Renderers.Add(r);
                    }

                    if (!group.Targets.Contains(boneTransform))
                        group.Targets.Add(boneTransform);

                    EnsureSelectableTarget(group, boneTransform.gameObject);
                }

                if (group.Targets.Count > 0)
                    m_Groups[group.Id] = group;
            }
        }

        Transform FindAssembledSkeletonRoot()
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "SK_Pelvis")
                    return child;
            }

            return null;
        }

        IEnumerable<Transform> FindMatchingTransforms(Transform root, BoneDefinition definition)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var exactName in definition.ExactNames)
                {
                    if (child.name == exactName)
                    {
                        yield return child;
                        goto nextChild;
                    }
                }

                foreach (var prefix in definition.PrefixNames)
                {
                    if (child.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return child;
                        goto nextChild;
                    }
                }

                nextChild: ;
            }
        }

        void EnsureSelectableTarget(BoneGroup group, GameObject target)
        {
            Bounds localBounds;
            var meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                localBounds = meshFilter.sharedMesh.bounds;
            }
            else
            {
                // Container node: the primary child mesh was removed from the prefab.
                // Derive bounds from whatever child renderers remain.
                var childRenderers = target.GetComponentsInChildren<Renderer>(false);
                if (childRenderers.Length == 0)
                    return;

                var worldBounds = childRenderers[0].bounds;
                for (var i = 1; i < childRenderers.Length; i++)
                    worldBounds.Encapsulate(childRenderers[i].bounds);

                // Convert world-space bounds to target's local space.
                var localCenter = target.transform.InverseTransformPoint(worldBounds.center);
                var scale = target.transform.lossyScale;
                var worldSize = worldBounds.size;
                var localSize = new Vector3(
                    scale.x > 0f ? worldSize.x / scale.x : worldSize.x,
                    scale.y > 0f ? worldSize.y / scale.y : worldSize.y,
                    scale.z > 0f ? worldSize.z / scale.z : worldSize.z);
                localBounds = new Bounds(localCenter, localSize);
            }

            foreach (var existingMeshCollider in target.GetComponents<MeshCollider>())
                Destroy(existingMeshCollider);
            foreach (var existingBoxCollider in target.GetComponents<BoxCollider>())
                Destroy(existingBoxCollider);

            AddCompactColliders(group, target, localBounds, group.Id);

            target.layer = LayerMask.NameToLayer("Default");

            var interactable = target.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
                interactable = target.AddComponent<XRSimpleInteractable>();

            interactable.enabled = true;
            interactable.interactionLayers = InteractionLayerMask.GetMask("Default");
            interactable.selectMode = InteractableSelectMode.Single;
            group.Interactables.Add(interactable);

            var selectable = target.GetComponent<SkeletonBoneSelectable>();
            if (selectable == null)
                selectable = target.AddComponent<SkeletonBoneSelectable>();

            selectable.Configure(this, group.Id);
            group.Selectables.Add(selectable);
        }

        void AddCompactColliders(BoneGroup group, GameObject target, Bounds meshBounds, string groupId)
        {
            var size = meshBounds.size;
            var center = meshBounds.center;

            if (groupId == "spine")
            {
                var compact = Vector3.Scale(size, new Vector3(0.38f, 0.85f, 0.38f));
                AddSegmentedBoxes(group, target, center, EnsureMin(compact), 1, 7);
                return;
            }

            if (groupId == "pelvis")
            {
                // If the mesh is a flat plane (removed child), Z depth is near-zero.
                // Enforce anatomically plausible depth so front-facing rays hit reliably.
                size.z = Mathf.Max(size.z, Mathf.Max(size.x, size.y) * 0.25f);
                var compact = Vector3.Scale(size, new Vector3(0.42f, 0.32f, 0.26f));
                AddBox(group, target, center + new Vector3(-size.x * 0.14f, 0f, 0f), EnsureMin(compact));
                AddBox(group, target, center + new Vector3(size.x * 0.14f, 0f, 0f), EnsureMin(compact));
                AddBox(group, target, center + new Vector3(0f, -size.y * 0.08f, 0f), EnsureMin(Vector3.Scale(size, new Vector3(0.16f, 0.26f, 0.18f))));
                return;
            }

            if (groupId == "ribcage")
            {
                // Same flat-plane safeguard as pelvis.
                size.z = Mathf.Max(size.z, Mathf.Max(size.x, size.y) * 0.25f);
                AddBox(group, target, center + new Vector3(-size.x * 0.18f, 0f, 0f), EnsureMin(Vector3.Scale(size, new Vector3(0.22f, 0.55f, 0.24f))));
                AddBox(group, target, center + new Vector3(size.x * 0.18f, 0f, 0f), EnsureMin(Vector3.Scale(size, new Vector3(0.22f, 0.55f, 0.24f))));
                AddBox(group, target, center + new Vector3(0f, size.y * 0.18f, 0f), EnsureMin(Vector3.Scale(size, new Vector3(0.20f, 0.18f, 0.16f))));
                AddBox(group, target, center + new Vector3(0f, -size.y * 0.16f, 0f), EnsureMin(Vector3.Scale(size, new Vector3(0.18f, 0.16f, 0.14f))));
                return;
            }

            var padded = EnsureMin(size * boxPadding);
            var longestAxis = 0;
            if (padded.y > padded.x && padded.y >= padded.z)
                longestAxis = 1;
            else if (padded.z > padded.x && padded.z > padded.y)
                longestAxis = 2;

            var segmentCount = longAxisSegments;
            if (Mathf.Max(padded.x, padded.y, padded.z) < 0.03f)
                segmentCount = 1;
            else if (Mathf.Max(padded.x, padded.y, padded.z) < 0.07f)
                segmentCount = 2;

            AddSegmentedBoxes(group, target, center, padded, longestAxis, segmentCount);
        }

        void AddSegmentedBoxes(BoneGroup group, GameObject target, Vector3 center, Vector3 size, int axis, int segmentCount)
        {
            if (segmentCount <= 1)
            {
                AddBox(group, target, center, EnsureMin(size));
                return;
            }

            var segmentSize = EnsureMin(size);
            var step = 0f;
            switch (axis)
            {
                case 0:
                    segmentSize.x = Mathf.Max(minimumColliderSize, size.x / segmentCount);
                    step = segmentSize.x;
                    break;
                case 1:
                    segmentSize.y = Mathf.Max(minimumColliderSize, size.y / segmentCount);
                    step = segmentSize.y;
                    break;
                default:
                    segmentSize.z = Mathf.Max(minimumColliderSize, size.z / segmentCount);
                    step = segmentSize.z;
                    break;
            }

            var startOffset = -0.5f * step * (segmentCount - 1);
            for (var i = 0; i < segmentCount; i++)
            {
                var segmentCenter = center;
                var offset = startOffset + i * step;
                switch (axis)
                {
                    case 0:
                        segmentCenter.x += offset;
                        break;
                    case 1:
                        segmentCenter.y += offset;
                        break;
                    default:
                        segmentCenter.z += offset;
                        break;
                }

                AddBox(group, target, segmentCenter, segmentSize);
            }
        }

        Vector3 EnsureMin(Vector3 size)
        {
            size.x = Mathf.Max(minimumColliderSize, size.x);
            size.y = Mathf.Max(minimumColliderSize, size.y);
            size.z = Mathf.Max(minimumColliderSize, size.z);
            return size;
        }

        void AddBox(BoneGroup group, GameObject target, Vector3 center, Vector3 size)
        {
            var collider = target.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.isTrigger = false;
            group.Colliders.Add(collider);
        }

        public void HandleHoverEntered(string groupId)
        {
            if (m_SelectedGroup != null)
                return;

            if (!m_Groups.TryGetValue(groupId, out var group))
                return;

            m_HoveredGroup = group;
            ApplyVisualState();
            if (infoPanel != null)
                infoPanel.UpdatePanel(group.Title, group.Description);
        }

        public void HandleHoverExited(string groupId)
        {
            if (m_SelectedGroup != null)
                return;

            if (!m_Groups.TryGetValue(groupId, out var group) || m_HoveredGroup != group)
                return;

            m_HoveredGroup = null;
            ApplyVisualState();
            ShowDefaultInfo();
        }

        public void HandleSelected(string groupId)
        {
            if (!m_Groups.TryGetValue(groupId, out var group))
                return;

            SetSelectedGroup(group);
            if (infoPanel != null)
                infoPanel.UpdatePanel(group.Title, group.Description);
        }

        void SetSelectedGroup(BoneGroup nextGroup)
        {
            if (m_SelectedGroup == nextGroup)
                return;

            m_SelectedGroup = nextGroup;
            m_HoveredGroup = null;
            ApplyVisualState();
        }

        void ApplyVisualState()
        {
            foreach (var group in m_Groups.Values)
            {
                foreach (var renderer in group.Renderers)
                    if (renderer != null && renderer.enabled)
                        renderer.SetPropertyBlock(null);
            }

            if (m_SelectedGroup != null)
            {
                ApplyGroupColor(m_SelectedGroup, selectedColor);
                return;
            }

            if (m_HoveredGroup != null)
                ApplyGroupColor(m_HoveredGroup, hoverColor);
        }

        void ApplyGroupColor(BoneGroup group, Color color)
        {
            m_HighlightBlock.Clear();
            m_HighlightBlock.SetColor("_BaseColor", color);
            m_HighlightBlock.SetColor("_Color", color);
            foreach (var renderer in group.Renderers)
                if (renderer != null && renderer.enabled)
                    renderer.SetPropertyBlock(m_HighlightBlock);
        }

        void ShowDefaultInfo()
        {
            if (infoPanel != null)
                infoPanel.UpdatePanel(defaultTitle, defaultBody);
        }
    }
}
