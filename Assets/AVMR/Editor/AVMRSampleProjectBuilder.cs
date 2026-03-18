using System.Collections.Generic;
using System.IO;
using AVMR.AnatomyViewer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Templates.MR;

public static class AVMRSampleProjectBuilder
{
    const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    const string TargetScenePath = "Assets/Scenes/AVMRAnatomyViewer.unity";
    const string RuntimeFolder = "Assets/AVMR/Runtime";
    const string PrefabFolder = "Assets/AVMR/Prefabs";
    const string ModelFolder = "Assets/AVMR/Imported";
    const string MaterialFolder = "Assets/AVMR/Materials";
    const string ResourcesFolder = "Assets/AVMR/Resources";
    const string AudioFolder = "Assets/AVMR/Resources/Narration";

    static readonly (string displayName, string relativeFbxPath, float targetHeight, string info)[] k_Layers =
    {
        ("Skin Layer", "Meshy_AI_Skin_Layer_0313093449_texture_fbx/Meshy_AI_Skin_Layer_0313093449_texture.fbx", 1.5f,
            "Protective outer body covering."),
        ("Skeleton Layer", "Assets/AVMR/Prefabs/sm_human_skeleton.fbx", 1.5f,
            "Rigid framework supporting the body."),
        ("Muscle Layer", "Meshy_AI_Muscular_Layer_0313093439_texture_fbx/Meshy_AI_Muscular_Layer_0313093439_texture.fbx", 1.5f,
            "Muscles powering movement and posture."),
        ("Anatomy Layer", "Meshy_AI_Neon_Full_Body_Anatom_0313093357_texture_fbx/Meshy_AI_Neon_Full_Body_Anatom_0313093357_texture.fbx", 1.5f,
            "Integrated view of major internal systems."),
        ("Heart Organ", "Meshy_AI_Anatomical_Heart_0218043313_texture_fbx/Meshy_AI_Anatomical_Heart_0218043313_texture.fbx", 0.8f,
            "Pumps blood through the body.")
    };

    static readonly (string title, string body, string clipName)[] k_Narrations =
    {
        ("Skin Layer", "Protective outer body covering.", "skin_layer"),
        ("Skeleton Layer", "Rigid framework supporting the body.", "skeleton_layer"),
        ("Muscle Layer", "Muscles powering movement and posture.", "muscle_layer"),
        ("Anatomy Layer", "Integrated view of major internal systems.", "anatomy_layer"),
        ("Heart Organ", "Pumps blood through the body.", "heart_organ"),
        ("Cranium", "Protects the brain.", "bone_cranium"),
        ("Mandible", "Lower jaw for chewing.", "bone_mandible"),
        ("Clavicle", "Braces the shoulder.", "bone_clavicle"),
        ("Scapula", "Shoulder blade anchor.", "bone_scapula"),
        ("Rib Cage", "Protects heart and lungs.", "bone_rib_cage"),
        ("Spine", "Supports trunk and spinal cord.", "bone_spine"),
        ("Pelvis", "Transfers body weight.", "bone_pelvis"),
        ("Humerus", "Upper arm bone.", "bone_humerus"),
        ("Radius", "Thumb-side forearm bone.", "bone_radius"),
        ("Ulna", "Little-finger forearm bone.", "bone_ulna"),
        ("Carpals", "Wrist bones for mobility.", "bone_carpals"),
        ("Femur", "Primary thigh support bone.", "bone_femur"),
        ("Patella", "Shields the knee joint.", "bone_patella"),
        ("Tibia", "Main weight-bearing shin bone.", "bone_tibia"),
        ("Fibula", "Stabilizes the outer leg.", "bone_fibula"),
        ("Tarsals", "Ankle bones aid balance.", "bone_tarsals"),
        ("Metatarsals", "Bridge ankle to toes.", "bone_metatarsals"),
        ("Phalanges", "Finger and toe bones.", "bone_phalanges"),
    };

    [MenuItem("Tools/AVMR/Build Anatomy Viewer Scene")]
    public static void Build()
    {
        EnsureFolders();
        EnsureMaterials();
        EnsureNarrationClips();
        EnsurePrefabs();
        BuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("AVMR anatomy viewer scene and prefabs are ready.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/AVMR"))
            AssetDatabase.CreateFolder("Assets", "AVMR");
        if (!AssetDatabase.IsValidFolder(RuntimeFolder))
            AssetDatabase.CreateFolder("Assets/AVMR", "Runtime");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/AVMR", "Prefabs");
        if (!AssetDatabase.IsValidFolder(ModelFolder))
            AssetDatabase.CreateFolder("Assets/AVMR", "Imported");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/AVMR", "Materials");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets/AVMR", "Resources");
        if (!AssetDatabase.IsValidFolder(AudioFolder))
            AssetDatabase.CreateFolder(ResourcesFolder, "Narration");
    }

    static void EnsureMaterials()
    {
        CreateColorMaterial("PanelBackground", new Color(0.09f, 0.12f, 0.16f, 0.92f));
        CreateColorMaterial("PanelAccent", new Color(0.91f, 0.72f, 0.18f, 1f));
        CreateColorMaterial("ButtonActive", new Color(0.95f, 0.71f, 0.16f, 1f));
        CreateColorMaterial("ButtonDisabled", new Color(0.36f, 0.36f, 0.36f, 1f));
    }

    static void CreateColorMaterial(string name, Color color)
    {
        var path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
    }

    static void EnsureNarrationClips()
    {
        foreach (var narration in k_Narrations)
        {
            var assetPath = $"{AudioFolder}/{narration.clipName}.aiff";
            if (File.Exists(assetPath))
                continue;

            Debug.LogWarning($"Missing narration clip: {assetPath}");
        }
    }

    static void EnsurePrefabs()
    {
        foreach (var layer in k_Layers)
        {
            var sourceModelPath = layer.relativeFbxPath.StartsWith("Assets/")
                ? layer.relativeFbxPath
                : $"{ModelFolder}/{layer.relativeFbxPath}";
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelPath);
            if (modelAsset == null)
                throw new FileNotFoundException($"Missing imported model at {sourceModelPath}");

            var root = new GameObject(layer.displayName);
            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                Object.DestroyImmediate(root);
                throw new FileNotFoundException($"Could not instantiate imported model at {sourceModelPath}");
            }

            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation =
                layer.displayName == "Skeleton Layer"
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.Euler(-90f, 180f, 0f);
            modelInstance.transform.localScale = Vector3.one;

            if (layer.displayName == "Skeleton Layer")
                StripLooseSkeletonChildren(modelInstance.transform);

            ApplyImportedMaterialOverrides(modelInstance.transform, Path.GetDirectoryName(sourceModelPath)?.Replace("\\", "/"));

            NormalizeChildTransform(root.transform, modelInstance.transform);

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.angularDamping = 4f;
            rigidbody.linearDamping = 2f;

            AddCompactColliders(root.transform, layer.displayName == "Heart Organ");

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.trackPosition = true;
            grab.trackRotation = true;
            var grabSo = new SerializedObject(grab);
            grabSo.FindProperty("m_PredictedVisualsTransform").objectReferenceValue = modelInstance.transform;
            grabSo.ApplyModifiedPropertiesWithoutUndo();

            var savePath = $"{PrefabFolder}/{layer.displayName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            Object.DestroyImmediate(root);
        }
    }

    static void NormalizeChildTransform(Transform root, Transform child)
    {
        if (TryGetSupportPoint(root, out var supportPoint))
        {
            child.localPosition -= supportPoint;
            return;
        }

        if (!TryGetBounds(root, out var bounds))
            return;

        var bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        child.localPosition -= bottomCenter;
    }

    static bool TryGetSupportPoint(Transform root, out Vector3 supportPoint)
    {
        var points = new List<Vector3>();
        var minY = float.PositiveInfinity;

        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
                continue;

            var localToRoot = root.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            foreach (var vertex in mesh.vertices)
            {
                var point = localToRoot.MultiplyPoint3x4(vertex);
                points.Add(point);
                if (point.y < minY)
                    minY = point.y;
            }
        }

        foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = skinnedRenderer.sharedMesh;
            if (mesh == null)
                continue;

            var localToRoot = root.worldToLocalMatrix * skinnedRenderer.transform.localToWorldMatrix;
            foreach (var vertex in mesh.vertices)
            {
                var point = localToRoot.MultiplyPoint3x4(vertex);
                points.Add(point);
                if (point.y < minY)
                    minY = point.y;
            }
        }

        if (points.Count == 0 || float.IsInfinity(minY))
        {
            supportPoint = default;
            return false;
        }

        var tolerance = 0.02f;
        var accumulated = Vector3.zero;
        var count = 0;
        foreach (var point in points)
        {
            if (point.y - minY > tolerance)
                continue;

            accumulated += point;
            count++;
        }

        if (count == 0)
        {
            supportPoint = default;
            return false;
        }

        supportPoint = accumulated / count;
        supportPoint.y = minY;
        return true;
    }

    static void StripLooseSkeletonChildren(Transform skeletonRoot)
    {
        var toDelete = new List<GameObject>();
        foreach (Transform child in skeletonRoot)
        {
            if (child.name == "SK_Pelvis")
                continue;

            toDelete.Add(child.gameObject);
        }

        foreach (var go in toDelete)
            Object.DestroyImmediate(go);
    }

    static void AddCompactColliders(Transform root, bool isHeart)
    {
        if (!TryGetBounds(root, out var bounds))
            return;

        if (isHeart)
        {
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = root.InverseTransformPoint(bounds.center);
            box.size = new Vector3(bounds.size.x * 0.72f, bounds.size.y * 0.8f, bounds.size.z * 0.72f);
            return;
        }

        var localCenter = root.InverseTransformPoint(bounds.center);
        var size = bounds.size;

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(0f, size.y * 0.43f, 0f),
            new Vector3(size.x * 0.16f, size.y * 0.13f, size.z * 0.16f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(-size.x * 0.26f, size.y * 0.22f, 0f),
            new Vector3(size.x * 0.18f, size.y * 0.14f, size.z * 0.16f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(size.x * 0.26f, size.y * 0.22f, 0f),
            new Vector3(size.x * 0.18f, size.y * 0.14f, size.z * 0.16f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(0f, size.y * 0.18f, 0f),
            new Vector3(size.x * 0.28f, size.y * 0.24f, size.z * 0.20f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(0f, -size.y * 0.02f, 0f),
            new Vector3(size.x * 0.24f, size.y * 0.16f, size.z * 0.18f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(-size.x * 0.10f, -size.y * 0.28f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.34f, size.z * 0.14f));

        AddBoxCollider(
            root.gameObject,
            localCenter + new Vector3(size.x * 0.10f, -size.y * 0.28f, 0f),
            new Vector3(size.x * 0.12f, size.y * 0.34f, size.z * 0.14f));
    }

    static void AddBoxCollider(GameObject target, Vector3 center, Vector3 size)
    {
        var box = target.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    static bool TryGetBounds(Transform root, out Bounds bounds)
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

    static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            throw new FileNotFoundException($"Base scene not found at {SourceScenePath}");

        var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        foreach (var goalManager in Object.FindObjectsByType<GoalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(goalManager.gameObject);

        foreach (var objectSpawner in Object.FindObjectsByType<ObjectSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(objectSpawner.gameObject);

        foreach (var spawnedObjectsManager in Object.FindObjectsByType<SpawnedObjectsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (spawnedObjectsManager != null)
                Object.DestroyImmediate(spawnedObjectsManager);
        }

        var environment = GameObject.Find("Environment");
        if (environment != null)
            Object.DestroyImmediate(environment);

        var oldRoot = GameObject.Find("AVMR Anatomy Viewer");
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        var root = new GameObject("AVMR Anatomy Viewer");
        var bootstrap = root.AddComponent<MRFeatureBootstrap>();
        root.AddComponent<TableOnlySurfaceVisualizer>();
        var anchorRig = CreateDeskAnchorRig(root.transform);
        var panelController = CreatePanel(anchorRig.PanelAnchor);
        var lightingRig = CreateLightingRig(root.transform);
        var experience = root.AddComponent<AnatomyViewerExperience>();
        var binder = root.AddComponent<TableSurfaceBinder>();
        binder.GetType();

        var innerButton = anchorRig.PanelAnchor.Find("InfoPanel/InfoCanvas/Background/ButtonRow/InnerLayerButton").GetComponent<AnatomyLayerButton>();
        var outerButton = anchorRig.PanelAnchor.Find("InfoPanel/InfoCanvas/Background/ButtonRow/OuterLayerButton").GetComponent<AnatomyLayerButton>();
        var rotateLeftButton = anchorRig.PanelAnchor.Find("InfoPanel/InfoCanvas/Background/ButtonRow/RotateLeftButton").GetComponent<AnatomyLayerButton>();
        var rotateRightButton = anchorRig.PanelAnchor.Find("InfoPanel/InfoCanvas/Background/ButtonRow/RotateRightButton").GetComponent<AnatomyLayerButton>();

        SetObjectReference(experience, "anchorRig", anchorRig);
        SetObjectReference(experience, "infoPanel", panelController);
        SetObjectReference(experience, "lightingRig", lightingRig);
        SetObjectReference(experience, "tableSurfaceBinder", binder);
        SetObjectReference(experience, "innerLayerButton", innerButton);
        SetObjectReference(experience, "outerLayerButton", outerButton);
        SetObjectReference(binder, "anchorRig", anchorRig);
        SetObjectReference(rotateLeftButton, "experience", experience);
        SetObjectReference(rotateRightButton, "experience", experience);

        PopulateLayers(experience);

        EditorSceneManager.SaveScene(scene, TargetScenePath);

        var buildSettings = new List<EditorBuildSettingsScene>();
        foreach (var existing in EditorBuildSettings.scenes)
            buildSettings.Add(existing);

        var targetSceneGuid = AssetDatabase.AssetPathToGUID(TargetScenePath);
        if (!buildSettings.Exists(sceneSetting => sceneSetting.guid.ToString() == targetSceneGuid))
            buildSettings.Insert(0, new EditorBuildSettingsScene(TargetScenePath, true));

        EditorBuildSettings.scenes = buildSettings.ToArray();
        EditorSceneManager.SaveScene(scene);
    }

    static DeskAnchorRig CreateDeskAnchorRig(Transform parent)
    {
        var rigRoot = new GameObject("DeskAnchorRig");
        rigRoot.transform.SetParent(parent, false);

        var modelAnchor = new GameObject("ModelAnchor");
        modelAnchor.transform.SetParent(rigRoot.transform, false);

        var panelAnchor = new GameObject("PanelAnchor");
        panelAnchor.transform.SetParent(rigRoot.transform, false);

        var rig = rigRoot.AddComponent<DeskAnchorRig>();
        SetObjectReference(rig, "modelAnchor", modelAnchor.transform);
        SetObjectReference(rig, "panelAnchor", panelAnchor.transform);
        return rig;
    }

    static AnatomyInfoPanelController CreatePanel(Transform parent)
    {
        var panelRoot = new GameObject("InfoPanel");
        panelRoot.transform.SetParent(parent, false);
        panelRoot.transform.localPosition = Vector3.zero;
        panelRoot.transform.localRotation = Quaternion.identity;
        panelRoot.AddComponent<AnchorBillboard>();
        var audioSource = panelRoot.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 0.3f;
        audioSource.maxDistance = 4f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        var canvasRoot = new GameObject("InfoCanvas", typeof(RectTransform));
        canvasRoot.transform.SetParent(panelRoot.transform, false);
        canvasRoot.transform.localPosition = Vector3.zero;
        canvasRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasRoot.transform.localScale = Vector3.one * 0.001f;

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvasRoot.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasRoot.AddComponent<GraphicRaycaster>();
        canvasRoot.AddComponent<TrackedDeviceGraphicRaycaster>();

        var rect = canvasRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(720f, 560f);

        var background = CreateUiObject("Background", canvasRoot.transform);
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.09f, 0.12f, 0.16f, 0.92f);
        Stretch(background.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(700f, 520f), Vector2.zero);

        var accent = CreateUiObject("AccentBar", background.transform);
        var accentImage = accent.AddComponent<Image>();
        accentImage.color = new Color(0.91f, 0.72f, 0.18f, 1f);
        Stretch(accent.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(700f, 18f), new Vector2(0f, -9f));

        var title = CreateText("TitleText", background.transform, new Vector2(640f, 70f), new Vector2(0f, -58f), 40f, FontStyles.Bold);
        var body = CreateText("BodyText", background.transform, new Vector2(640f, 250f), new Vector2(0f, -210f), 24f, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;

        var buttonRow = CreateUiObject("ButtonRow", background.transform);
        Stretch(buttonRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(620f, 188f), new Vector2(0f, 114f));

        CreateLayerButton(buttonRow.transform, "RotateLeftButton", "↺", new Vector2(-114f, -18f), AnatomyLayerButton.ButtonAction.RotateLeft, new Vector2(108f, 52f));
        CreateLayerButton(buttonRow.transform, "RotateRightButton", "↻", new Vector2(114f, -18f), AnatomyLayerButton.ButtonAction.RotateRight, new Vector2(108f, 52f));

        CreateLayerButton(buttonRow.transform, "InnerLayerButton", "GO INSIDE", new Vector2(0f, -90f), AnatomyLayerButton.ButtonAction.InnerLayer);
        CreateLayerButton(buttonRow.transform, "OuterLayerButton", "GO OUTSIDE", new Vector2(0f, -158f), AnatomyLayerButton.ButtonAction.OuterLayer);

        var controller = panelRoot.AddComponent<AnatomyInfoPanelController>();
        SetObjectReference(controller, "titleText", title);
        SetObjectReference(controller, "bodyText", body);
        SetObjectReference(controller, "audioSource", audioSource);
        SetNarrations(controller);
        return controller;
    }

    static void SetNarrations(AnatomyInfoPanelController controller)
    {
        var serializedObject = new SerializedObject(controller);
        var narrations = serializedObject.FindProperty("narrations");
        narrations.arraySize = k_Narrations.Length;

        for (var i = 0; i < k_Narrations.Length; i++)
        {
            var entry = narrations.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("title").stringValue = k_Narrations[i].title;
            entry.FindPropertyRelative("body").stringValue = k_Narrations[i].body;
            entry.FindPropertyRelative("clip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{k_Narrations[i].clipName}.aiff");
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateLayerButton(Transform parent, string name, string label, Vector2 anchoredPosition, AnatomyLayerButton.ButtonAction action, Vector2? sizeOverride = null)
    {
        var buttonRoot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(parent, false);

        var rect = buttonRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = sizeOverride ?? new Vector2(400f, 52f);
        rect.anchoredPosition = anchoredPosition;

        var image = buttonRoot.GetComponent<Image>();
        image.color = new Color(0.95f, 0.71f, 0.16f, 1f);

        var button = buttonRoot.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = new Color(0.95f, 0.71f, 0.16f, 1f);
        colors.highlightedColor = new Color(1f, 0.86f, 0.33f, 1f);
        colors.pressedColor = new Color(0.88f, 0.62f, 0.08f, 1f);
        colors.disabledColor = new Color(0.38f, 0.38f, 0.38f, 1f);
        button.colors = colors;

        var buttonScript = buttonRoot.AddComponent<AnatomyLayerButton>();
        SetEnum(buttonScript, "action", (int)action);
        SetObjectReference(buttonScript, "targetImage", image);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonRoot.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.color = Color.white;
        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return buttonRoot;
    }

    static GameObject CreatePlainButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
    {
        var buttonRoot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(parent, false);

        var rect = buttonRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var image = buttonRoot.GetComponent<Image>();
        image.color = new Color(0.95f, 0.71f, 0.16f, 1f);

        var button = buttonRoot.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1f, 0.86f, 0.33f, 1f);
        colors.pressedColor = new Color(0.88f, 0.62f, 0.08f, 1f);
        colors.disabledColor = new Color(0.38f, 0.38f, 0.38f, 1f);
        button.colors = colors;

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonRoot.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.color = Color.white;
        text.fontSize = 22f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return buttonRoot;
    }

    static AnatomyLightingRig CreateLightingRig(Transform parent)
    {
        var rigRoot = new GameObject("LightingRig");
        rigRoot.transform.SetParent(parent, false);
        var rig = rigRoot.AddComponent<AnatomyLightingRig>();

        var key = CreateSpotlight("KeyLight", rigRoot.transform, new Color(1f, 0.96f, 0.92f));
        var fill = CreateSpotlight("FillLight", rigRoot.transform, new Color(0.82f, 0.9f, 1f));
        var rim = CreateSpotlight("RimLight", rigRoot.transform, new Color(1f, 0.88f, 0.7f));

        SetObjectReference(rig, "keyLight", key);
        SetObjectReference(rig, "fillLight", fill);
        SetObjectReference(rig, "rimLight", rim);
        return rig;
    }

    static Light CreateSpotlight(string name, Transform parent, Color color)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 8f;
        light.color = color;
        return light;
    }

    static void PopulateLayers(AnatomyViewerExperience experience)
    {
        var so = new SerializedObject(experience);
        var layersProperty = so.FindProperty("layers");
        layersProperty.arraySize = k_Layers.Length;

        for (var i = 0; i < k_Layers.Length; i++)
        {
            var element = layersProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("displayName").stringValue = k_Layers[i].displayName;
            element.FindPropertyRelative("infoText").stringValue = k_Layers[i].info;
            var prefabPath = $"{PrefabFolder}/{k_Layers[i].displayName}.prefab";

            element.FindPropertyRelative("modelPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            element.FindPropertyRelative("localPositionOffset").vector3Value = Vector3.zero;
            element.FindPropertyRelative("localRotationOffset").vector3Value = Vector3.zero;
            element.FindPropertyRelative("targetHeightMeters").floatValue = k_Layers[i].targetHeight;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    static void ApplyImportedMaterialOverrides(Transform root, string modelDirectory)
    {
        if (string.IsNullOrEmpty(modelDirectory))
            return;

        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { modelDirectory });
        if (materialGuids.Length == 0)
            return;

        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuids[0]));
        if (material == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var count = renderer.sharedMaterials.Length;
            if (count == 0)
                continue;

            var materials = new Material[count];
            for (var i = 0; i < count; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
        }
    }

    static void Stretch(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    static void SetObjectReference(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetEnum(Object target, string fieldName, int enumValue)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).enumValueIndex = enumValue;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
