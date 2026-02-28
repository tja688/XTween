using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ReplicaV3MigrationUtility
{
    [MenuItem("ReplicaV3/Fix Prefab Bindings")]
    public static void FixBindings()
    {
        FixFadeContent();
        FixDecay();
        FixDock();
        FixGlare();
        FixElastic();
        RegisterInShowcase();
        Debug.Log("V3 Prefab Bindings Fixed!");
    }

    static void RegisterInShowcase()
    {
        var controller = GameObject.FindFirstObjectByType<ReplicaV3ShowcaseController>();
        if (controller == null) return;

        Undo.RecordObject(controller, "Register V3 Effects");

        AddEffect(controller, "decay-v3", "Decay V3", "带有噪声干扰与互动倾斜的卡片入场效果。", "Assets/ReplicaV3/Prefabs/Effects/Decay_V3.prefab");
        AddEffect(controller, "dock-magnify-toolbar-v3", "DockMagnifyToolbar V3", "经典的 Dock 栏缩放工具栏效果。", "Assets/ReplicaV3/Prefabs/Effects/DockMagnifyToolbar_V3.prefab");
        AddEffect(controller, "elastic-overflow-slider-v3", "ElasticOverflowSlider V3", "带弹性反馈与图标挤压效果的进度条。", "Assets/ReplicaV3/Prefabs/Effects/ElasticOverflowSlider_V3.prefab");
        AddEffect(controller, "fade-content-v3", "FadeContent V3", "基础但通用的文本/内容渐入渐出效果。", "Assets/ReplicaV3/Prefabs/Effects/FadeContent_V3.prefab");
        AddEffect(controller, "glare-hover-v3", "GlareHover V3", "卡片扫光与边框高亮 Hover 交互。", "Assets/ReplicaV3/Prefabs/Effects/GlareHover_V3.prefab");

        EditorUtility.SetDirty(controller);
    }

    static void AddEffect(ReplicaV3ShowcaseController controller, string id, string name, string desc, string path)
    {
        if (controller.Effects.Exists(e => e.EffectId == id)) return;

        var prefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(path);
        if (prefab == null) return;

        controller.Effects.Add(new ReplicaV3ShowcaseEffectEntry
        {
            EffectId = id,
            DisplayName = name,
            Description = desc,
            EffectPrefab = prefab
        });
    }

    static void FixElastic()
    {
        string path = "Assets/ReplicaV3/Prefabs/Effects/ElasticOverflowSlider_V3.prefab";
        if (!System.IO.File.Exists(path)) return;
        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);

            var effect = editing.Root.GetComponent<ReplicaV3ElasticOverflowSliderEffect>();
            if (effect == null) effect = editing.Root.AddComponent<ReplicaV3ElasticOverflowSliderEffect>();

            effect.TrackRect = editing.Root.transform.Find("Backdrop/Frame/SliderRow/SliderContainer/TrackRoot/TrackWrapper/Track") as RectTransform;
            effect.TrackWrapper = editing.Root.transform.Find("Backdrop/Frame/SliderRow/SliderContainer/TrackRoot/TrackWrapper") as RectTransform;
            effect.FillRect = editing.Root.transform.Find("Backdrop/Frame/SliderRow/SliderContainer/TrackRoot/TrackWrapper/Track/Fill") as RectTransform;
            effect.KnobRect = editing.Root.transform.Find("Backdrop/Frame/SliderRow/SliderContainer/TrackRoot/Knob") as RectTransform;
            effect.LeftIconRect = editing.Root.transform.Find("Backdrop/Frame/SliderRow/LeftIcon") as RectTransform;
            effect.RightIconRect = editing.Root.transform.Find("Backdrop/Frame/SliderRow/RightIcon") as RectTransform;
            effect.ValueText = editing.Root.transform.Find("Backdrop/Frame/Value")?.GetComponent<Text>();

            effect.EffectDisplayName = "ElasticOverflowSlider V3";
            effect.EffectKey = "elastic-overflow-slider-v3";
        }
    }

    static void FixFadeContent()
    {
        string path = "Assets/ReplicaV3/Prefabs/Effects/FadeContent_V3.prefab";
        if (!System.IO.File.Exists(path)) return;
        using (var editing = new PrefabEditingScope(path))
        {
            var effect = editing.Root.GetComponent<ReplicaV3FadeContentEffect>();
            if (effect == null) effect = editing.Root.AddComponent<ReplicaV3FadeContentEffect>();

            effect.Card = editing.Root.transform.Find("Card") as RectTransform;
            effect.TitleText = editing.Root.transform.Find("Card/Title")?.GetComponent<Text>();
            effect.BodyText = editing.Root.transform.Find("Card/Body")?.GetComponent<Text>();
            effect.EffectDisplayName = "FadeContent V3";
            effect.EffectKey = "fade-content-v3";
        }
    }

    static void FixDecay()
    {
        string path = "Assets/ReplicaV3/Prefabs/Effects/Decay_V3.prefab";
        if (!System.IO.File.Exists(path)) return;
        using (var editing = new PrefabEditingScope(path))
        {
            var effect = editing.Root.GetComponent<ReplicaV3DecayEffect>();
            if (effect == null) effect = editing.Root.AddComponent<ReplicaV3DecayEffect>();

            effect.CardRoot = editing.Root.transform.Find("DecayCard") as RectTransform;
            var photoPath = "DecayCard/CardMask/Photo";
            var photoObj = editing.Root.transform.Find(photoPath);
            effect.CardImage = photoObj?.GetComponent<Image>();

            var noiseRoot = editing.Root.transform.Find(photoPath + "/Noise");
            if (noiseRoot != null)
            {
                effect.NoiseStrips = new List<RectTransform>();
                effect.NoiseImages = new List<Image>();
                for (int i = 0; i < noiseRoot.childCount; i++)
                {
                    var child = noiseRoot.GetChild(i) as RectTransform;
                    effect.NoiseStrips.Add(child);
                    effect.NoiseImages.Add(child.GetComponent<Image>());
                }
            }
            effect.EffectDisplayName = "Decay V3";
            effect.EffectKey = "decay-v3";
        }
    }

    static void FixDock()
    {
        string path = "Assets/ReplicaV3/Prefabs/Effects/DockMagnifyToolbar_V3.prefab";
        if (!System.IO.File.Exists(path)) return;
        using (var editing = new PrefabEditingScope(path))
        {
            var effect = editing.Root.GetComponent<ReplicaV3DockMagnifyToolbarEffect>();
            if (effect == null) effect = editing.Root.AddComponent<ReplicaV3DockMagnifyToolbarEffect>();

            effect.DockPanel = editing.Root.transform.Find("Backdrop/DockPanel") as RectTransform;
            effect.InteractionHitSource = effect.DockPanel;

            effect.ItemRoots = new List<RectTransform>();
            var pA = editing.Root.transform.Find("Backdrop/PlateA");
            var pB = editing.Root.transform.Find("Backdrop/PlateB");
            if (pA != null) effect.ItemRoots.Add(pA as RectTransform);
            if (pB != null) effect.ItemRoots.Add(pB as RectTransform);

            effect.EffectDisplayName = "DockMagnifyToolbar V3";
            effect.EffectKey = "dock-magnify-toolbar-v3";
        }
    }

    static void FixGlare()
    {
        string path = "Assets/ReplicaV3/Prefabs/Effects/GlareHover_V3.prefab";
        if (!System.IO.File.Exists(path)) return;
        using (var editing = new PrefabEditingScope(path))
        {
            var effect = editing.Root.GetComponent<ReplicaV3GlareHoverEffect>();
            if (effect == null) effect = editing.Root.AddComponent<ReplicaV3GlareHoverEffect>();

            effect.Container = editing.Root.transform.Find("Container") as RectTransform;
            effect.Glare = editing.Root.transform.Find("Container/Glare") as RectTransform;
            effect.GlareImage = effect.Glare?.GetComponent<Image>();
            effect.LabelText = editing.Root.transform.Find("Container/Label")?.GetComponent<Text>();

            effect.EffectDisplayName = "GlareHover V3";
            effect.EffectKey = "glare-hover-v3";
        }
    }

    static void RemoveMissingRecursive(GameObject go)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            RemoveMissingRecursive(child.gameObject);
        }
    }

    [MenuItem("ReplicaV3/Migrate/Bind 16-20 Effects")]
    public static void MigrateAndBind16To20()
    {
        EnsurePrefabFromV2("Assets/ReplicaV2/Prefabs/Effects/Noise_Baked.prefab", "Assets/ReplicaV3/Prefabs/Effects/Noise_V3.prefab");
        EnsurePrefabFromV2("Assets/ReplicaV2/Prefabs/Effects/OrbitImages_Baked.prefab", "Assets/ReplicaV3/Prefabs/Effects/OrbitImages_V3.prefab");
        EnsurePrefabFromV2("Assets/ReplicaV2/Prefabs/Effects/PixelTransition_Baked.prefab", "Assets/ReplicaV3/Prefabs/Effects/PixelTransition_V3.prefab");
        EnsurePrefabFromV2("Assets/ReplicaV2/Prefabs/Effects/ReflectiveCard_Baked.prefab", "Assets/ReplicaV3/Prefabs/Effects/ReflectiveCard_V3.prefab");
        EnsurePrefabFromV2("Assets/ReplicaV2/Prefabs/Effects/RotatingText_Baked.prefab", "Assets/ReplicaV3/Prefabs/Effects/RotatingText_V3.prefab");

        BindNoiseV3Prefab();
        BindOrbitImagesV3Prefab();
        BindPixelTransitionV3Prefab();
        BindReflectiveCardV3Prefab();
        BindRotatingTextV3Prefab();

        Register16To20InShowcaseScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ReplicaV3 16~20 已完成 prefab 绑定与 Showcase 注册。");
    }

    static void EnsurePrefabFromV2(string sourcePath, string targetPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"[ReplicaV3] Source prefab not found: {sourcePath}");
            return;
        }

        if (File.Exists(targetPath))
        {
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(sourcePath);
        root.name = Path.GetFileNameWithoutExtension(targetPath);
        RemoveMissingRecursive(root);
        PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void BindNoiseV3Prefab()
    {
        const string path = "Assets/ReplicaV3/Prefabs/Effects/Noise_V3.prefab";
        if (!File.Exists(path)) return;

        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);
            var effect = EnsureComponent<ReplicaV3NoiseEffect>(editing.Root);
            effect.EffectKey = "noise-v3";
            effect.EffectDisplayName = "Noise V3";
            effect.UsageDescription = "16Noise 噪点/视觉噪声控制。";
            effect.EffectRoot = editing.Root.transform as RectTransform;
            effect.EffectCanvasGroup = EnsureComponent<CanvasGroup>(editing.Root);
            effect.NoiseImage = editing.Root.transform.Find("Noise")?.GetComponent<RawImage>();
            EditorUtility.SetDirty(editing.Root);
        }
    }

    static void BindOrbitImagesV3Prefab()
    {
        const string path = "Assets/ReplicaV3/Prefabs/Effects/OrbitImages_V3.prefab";
        if (!File.Exists(path)) return;

        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);
            var effect = EnsureComponent<ReplicaV3OrbitImagesEffect>(editing.Root);
            effect.EffectKey = "orbit-images-v3";
            effect.EffectDisplayName = "OrbitImages V3";
            effect.UsageDescription = "17OrbitImages 图像环绕轨道旋转。";
            effect.EffectRoot = editing.Root.transform as RectTransform;
            effect.EffectCanvasGroup = EnsureComponent<CanvasGroup>(editing.Root);

            effect.Stage = editing.Root.transform.Find("Backdrop/Stage") as RectTransform;
            effect.OrbitRotationRoot = editing.Root.transform.Find("Backdrop/Stage/OrbitRotation") as RectTransform;
            effect.CenterTitleText = editing.Root.transform.Find("Backdrop/Stage/Center/CenterTitle")?.GetComponent<Text>();
            effect.ItemRoots = new List<RectTransform>();
            effect.ItemImages = new List<Image>();

            var itemsRoot = editing.Root.transform.Find("Backdrop/Stage/OrbitRotation/Items");
            if (itemsRoot != null)
            {
                for (int i = 0; i < itemsRoot.childCount; i++)
                {
                    var child = itemsRoot.GetChild(i) as RectTransform;
                    if (child == null) continue;
                    effect.ItemRoots.Add(child);
                    effect.ItemImages.Add(child.GetComponent<Image>());
                }
            }

            EditorUtility.SetDirty(editing.Root);
        }
    }

    static void BindPixelTransitionV3Prefab()
    {
        const string path = "Assets/ReplicaV3/Prefabs/Effects/PixelTransition_V3.prefab";
        if (!File.Exists(path)) return;

        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);
            RemoveInputRelayComponents(editing.Root);

            var effect = EnsureComponent<ReplicaV3PixelTransitionEffect>(editing.Root);
            effect.EffectKey = "pixel-transition-v3";
            effect.EffectDisplayName = "PixelTransition V3";
            effect.UsageDescription = "18PixelTransition 像素化转场效果。";
            effect.EffectRoot = editing.Root.transform as RectTransform;
            effect.EffectCanvasGroup = EnsureComponent<CanvasGroup>(editing.Root);

            effect.Card = editing.Root.transform.Find("Card") as RectTransform;
            effect.DefaultLayer = editing.Root.transform.Find("Card/DefaultLayer") as RectTransform;
            effect.DefaultImage = editing.Root.transform.Find("Card/DefaultLayer/DefaultSprite")?.GetComponent<Image>();
            effect.DefaultLabel = editing.Root.transform.Find("Card/DefaultLayer/DefaultLabel")?.GetComponent<Text>();
            effect.ActiveLayer = editing.Root.transform.Find("Card/ActiveLayer") as RectTransform;
            effect.ActiveImage = editing.Root.transform.Find("Card/ActiveLayer/ActiveSprite")?.GetComponent<Image>();
            effect.ActiveLabel = editing.Root.transform.Find("Card/ActiveLayer/ActiveLabel")?.GetComponent<Text>();
            effect.InteractionHitSource = effect.Card;
            effect.InteractionRangeDependency = effect.Card;

            effect.PixelBlocks = new List<Image>();
            var pixelsRoot = editing.Root.transform.Find("Card/Pixels");
            if (pixelsRoot != null)
            {
                for (int i = 0; i < pixelsRoot.childCount; i++)
                {
                    var image = pixelsRoot.GetChild(i).GetComponent<Image>();
                    if (image != null)
                    {
                        effect.PixelBlocks.Add(image);
                    }
                }
            }

            EditorUtility.SetDirty(editing.Root);
        }
    }

    static void BindReflectiveCardV3Prefab()
    {
        const string path = "Assets/ReplicaV3/Prefabs/Effects/ReflectiveCard_V3.prefab";
        if (!File.Exists(path)) return;

        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);
            var effect = EnsureComponent<ReplicaV3ReflectiveCardEffect>(editing.Root);
            effect.EffectKey = "reflective-card-v3";
            effect.EffectDisplayName = "ReflectiveCard V3";
            effect.UsageDescription = "19ReflectiveCard 卡片反光/镜面感。";
            effect.EffectRoot = editing.Root.transform as RectTransform;
            effect.EffectCanvasGroup = EnsureComponent<CanvasGroup>(editing.Root);

            effect.ContentRoot = editing.Root.transform.Find("Content") as RectTransform;
            effect.Card = editing.Root.transform.Find("Content/Card") as RectTransform;
            effect.CardImage = effect.Card != null ? effect.Card.GetComponent<Image>() : null;

            effect.HintText = editing.Root.transform.Find("Hint")?.GetComponent<Text>();
            effect.UserNameText = editing.Root.transform.Find("Content/Card/CardContent/UserName")?.GetComponent<Text>();
            effect.RoleText = editing.Root.transform.Find("Content/Card/CardContent/Role")?.GetComponent<Text>();
            effect.IdNumberText = editing.Root.transform.Find("Content/Card/CardContent/Footer/IDValue")?.GetComponent<Text>();
            effect.BadgeText = editing.Root.transform.Find("Content/Card/CardContent/Header/SecureBadge/Label")?.GetComponent<Text>();

            effect.Sheen = editing.Root.transform.Find("Content/Card/CardContent/Sheen") as RectTransform;
            effect.SheenImage = effect.Sheen != null ? effect.Sheen.GetComponent<Image>() : null;
            effect.SheenGroup = effect.Sheen != null ? EnsureComponent<CanvasGroup>(effect.Sheen.gameObject) : null;

            effect.Spotlight = editing.Root.transform.Find("Content/Card/CardContent/Spotlight") as RectTransform;
            effect.SpotlightImage = effect.Spotlight != null ? effect.Spotlight.GetComponent<Image>() : null;
            effect.SpotlightGroup = effect.Spotlight != null ? EnsureComponent<CanvasGroup>(effect.Spotlight.gameObject) : null;

            effect.InteractionHitSource = effect.Card;
            effect.InteractionRangeDependency = effect.Card;
            effect.NoiseStrips = new List<RectTransform>();
            effect.NoiseImages = new List<Image>();

            var noiseRoot = editing.Root.transform.Find("Content/Card/CardContent/Noise");
            if (noiseRoot != null)
            {
                for (int i = 0; i < noiseRoot.childCount; i++)
                {
                    var strip = noiseRoot.GetChild(i) as RectTransform;
                    if (strip == null) continue;
                    effect.NoiseStrips.Add(strip);
                    effect.NoiseImages.Add(strip.GetComponent<Image>());
                }
            }

            EditorUtility.SetDirty(editing.Root);
        }
    }

    static void BindRotatingTextV3Prefab()
    {
        const string path = "Assets/ReplicaV3/Prefabs/Effects/RotatingText_V3.prefab";
        if (!File.Exists(path)) return;

        using (var editing = new PrefabEditingScope(path))
        {
            RemoveMissingRecursive(editing.Root);
            var effect = EnsureComponent<ReplicaV3RotatingTextEffect>(editing.Root);
            effect.EffectKey = "rotating-text-v3";
            effect.EffectDisplayName = "RotatingText V3";
            effect.UsageDescription = "20RotatingText 文字旋转/翻转切换。";
            effect.EffectRoot = editing.Root.transform as RectTransform;
            effect.EffectCanvasGroup = EnsureComponent<CanvasGroup>(editing.Root);
            effect.ContentRoot = editing.Root.transform.Find("Content") as RectTransform;
            effect.Viewport = editing.Root.transform.Find("Content/Viewport") as RectTransform;
            effect.TextTemplate = editing.Root.GetComponentInChildren<Text>(true);
            EditorUtility.SetDirty(editing.Root);
        }
    }

    static void Register16To20InShowcaseScenes()
    {
        var scenePaths = new[]
        {
            "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_Overlay.unity",
            "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_World.unity"
        };

        for (int i = 0; i < scenePaths.Length; i++)
        {
            var scenePath = scenePaths[i];
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<ReplicaV3ShowcaseController>();
            if (controller == null)
            {
                Debug.LogWarning($"[ReplicaV3] Showcase controller not found in scene: {scenePath}");
                continue;
            }

            Undo.RecordObject(controller, "Register 16~20 V3 Effects");
            AddOrUpdateEffect(controller, "noise-v3", "Noise V3", "16Noise 噪点/视觉噪声控制。", "Assets/ReplicaV3/Prefabs/Effects/Noise_V3.prefab");
            AddOrUpdateEffect(controller, "orbit-images-v3", "OrbitImages V3", "17OrbitImages 图像环绕轨道旋转。", "Assets/ReplicaV3/Prefabs/Effects/OrbitImages_V3.prefab");
            AddOrUpdateEffect(controller, "pixel-transition-v3", "PixelTransition V3", "18PixelTransition 像素化转场效果。", "Assets/ReplicaV3/Prefabs/Effects/PixelTransition_V3.prefab");
            AddOrUpdateEffect(controller, "reflective-card-v3", "ReflectiveCard V3", "19ReflectiveCard 卡片反光/镜面感。", "Assets/ReplicaV3/Prefabs/Effects/ReflectiveCard_V3.prefab");
            AddOrUpdateEffect(controller, "rotating-text-v3", "RotatingText V3", "20RotatingText 文字旋转/翻转切换。", "Assets/ReplicaV3/Prefabs/Effects/RotatingText_V3.prefab");
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void AddOrUpdateEffect(ReplicaV3ShowcaseController controller, string id, string name, string desc, string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(path);
        if (prefab == null) return;

        var index = controller.Effects.FindIndex(e => e != null && e.EffectId == id);
        if (index >= 0)
        {
            controller.Effects[index].DisplayName = name;
            controller.Effects[index].Description = desc;
            controller.Effects[index].EffectPrefab = prefab;
            return;
        }

        controller.Effects.Add(new ReplicaV3ShowcaseEffectEntry
        {
            EffectId = id,
            DisplayName = name,
            Description = desc,
            EffectPrefab = prefab
        });
    }

    static void RemoveInputRelayComponents(GameObject root)
    {
        var allBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            var behaviour = allBehaviours[i];
            if (behaviour == null) continue;
            var typeName = behaviour.GetType().Name;
            if (typeName.Contains("InputRelay"))
            {
                Object.DestroyImmediate(behaviour, true);
            }
        }
    }

    static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var comp = gameObject.GetComponent<T>();
        if (comp == null)
        {
            comp = gameObject.AddComponent<T>();
        }

        return comp;
    }

    private class PrefabEditingScope : System.IDisposable
    {
        private readonly string _path;
        public readonly GameObject Root;
        public PrefabEditingScope(string path)
        {
            _path = path;
            Root = PrefabUtility.LoadPrefabContents(path);
        }
        public void Dispose()
        {
            PrefabUtility.SaveAsPrefabAsset(Root, _path);
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }
}
