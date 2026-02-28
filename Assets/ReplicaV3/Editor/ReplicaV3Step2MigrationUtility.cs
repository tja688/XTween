using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ReplicaV3Step2MigrationUtility
{
    private const string PrefabFolder = "Assets/ReplicaV3/Prefabs/Effects";
    private const string OverlayScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_Overlay.unity";
    private const string WorldScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_World.unity";

    [MenuItem("ReplicaV3/Build Step2 Prefabs And Register Showcase")]
    public static void BuildStep2PrefabsAndRegisterShowcase()
    {
        EnsureFolder(PrefabFolder);

        var built = new List<ReplicaV3EffectBase>
        {
            BuildGlassIconsTiltPrefab(),
            BuildGlitchTextPrefab(),
            BuildGradientTextPrefab(),
            BuildGridMotionPrefab(),
            BuildMenuRevealStaggerLayeredPrefab()
        };

        RegisterEntriesInScene(OverlayScenePath, built);
        RegisterEntriesInScene(WorldScenePath, built);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ReplicaV3] Step2 prefab build + showcase registration complete.");
    }

    private static ReplicaV3EffectBase BuildGlassIconsTiltPrefab()
    {
        var root = CreateRoot("GlassIconsTilt_V3", new Vector2(980f, 620f), out var rootImage, out var group);
        rootImage.color = new Color(0.05f, 0.07f, 0.13f, 0.90f);

        var hint = CreateText("Hint", root.transform as RectTransform, "GlassIcons  |  Hover each icon tile", 30, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.93f, 0.96f, 1f, 0.95f));
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.sizeDelta = new Vector2(900f, 58f);
        hintRect.anchoredPosition = new Vector2(0f, -30f);

        var grid = CreateRect("Grid", root.transform as RectTransform, Vector2.zero, new Vector2(760f, 430f));
        grid.anchorMin = new Vector2(0.5f, 0.5f);
        grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.pivot = new Vector2(0.5f, 0.5f);
        grid.anchoredPosition = new Vector2(0f, -20f);

        var effect = root.AddComponent<ReplicaV3GlassIconsTiltEffect>();
        effect.EffectKey = "glass-icons-tilt-v3";
        effect.EffectDisplayName = "GlassIconsTilt V3";
        effect.UsageDescription = "玻璃感图标卡片悬停倾斜与标签揭示。";
        effect.EffectRoot = root.transform as RectTransform;
        effect.EffectCanvasGroup = group;
        effect.StageRoot = root.transform as RectTransform;
        effect.GridRoot = grid;
        effect.HintText = hint;
        effect.InteractionHitSource = root.transform as RectTransform;
        effect.ShowInteractionRange = true;

        effect.Icons = new List<ReplicaV3GlassIconsTiltEffect.IconBinding>();
        var defaults = new[]
        {
            ("Home", "H", new Color(0.22f, 0.46f, 0.90f, 1f)),
            ("Email", "@", new Color(0.64f, 0.34f, 0.92f, 1f)),
            ("Play", ">", new Color(0.88f, 0.36f, 0.30f, 1f)),
            ("Photo", "P", new Color(0.34f, 0.44f, 0.88f, 1f)),
            ("Chart", "#", new Color(0.84f, 0.58f, 0.24f, 1f)),
            ("Cloud", "C", new Color(0.28f, 0.66f, 0.42f, 1f))
        };

        var columns = 3;
        var spacing = new Vector2(250f, 210f);
        for (var i = 0; i < defaults.Length; i++)
        {
            var row = i / columns;
            var col = i % columns;
            var startX = -spacing.x;
            var startY = 110f;

            var iconRoot = CreateRect($"Icon_{i}", grid, new Vector2(startX + (col * spacing.x), startY - (row * spacing.y)), new Vector2(180f, 220f));
            var iconHit = iconRoot.gameObject.AddComponent<Image>();
            iconHit.color = new Color(1f, 1f, 1f, 0.001f);
            iconHit.raycastTarget = true;

            var iconBase = CreateRect("IconBase", iconRoot, new Vector2(0f, -20f), new Vector2(128f, 128f));
            iconBase.anchorMin = new Vector2(0.5f, 1f);
            iconBase.anchorMax = new Vector2(0.5f, 1f);
            iconBase.pivot = new Vector2(0.5f, 1f);

            var back = CreatePanel("Back", iconBase, defaults[i].Item3);
            Stretch(back);
            back.localRotation = Quaternion.Euler(0f, 0f, effect.BackIdleRotation);

            var front = CreatePanel("Front", iconBase, new Color(1f, 1f, 1f, 0.14f));
            Stretch(front);

            var glyph = CreateText("Glyph", front, defaults[i].Item2, 56, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.97f, 1f, 0.96f));
            Stretch(glyph.rectTransform);

            var labelRoot = CreateRect("LabelRoot", iconRoot, new Vector2(0f, effect.LabelIdleY), new Vector2(170f, 32f));
            labelRoot.anchorMin = new Vector2(0.5f, 1f);
            labelRoot.anchorMax = new Vector2(0.5f, 1f);
            labelRoot.pivot = new Vector2(0.5f, 1f);
            var label = CreateText("Label", labelRoot, defaults[i].Item1, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 1f, 0.98f));
            Stretch(label.rectTransform);
            var labelGroup = labelRoot.gameObject.AddComponent<CanvasGroup>();
            labelGroup.alpha = 0f;

            effect.Icons.Add(new ReplicaV3GlassIconsTiltEffect.IconBinding
            {
                Root = iconRoot,
                Back = back,
                Front = front,
                LabelRoot = labelRoot,
                LabelGroup = labelGroup,
                GlyphText = glyph,
                LabelText = label,
                BackImage = back.GetComponent<Image>()
            });
        }

        return SavePrefabAndGetComponent<ReplicaV3GlassIconsTiltEffect>(root, Path.Combine(PrefabFolder, "GlassIconsTilt_V3.prefab"));
    }

    private static ReplicaV3EffectBase BuildGlitchTextPrefab()
    {
        var root = CreateRoot("GlitchText_V3", new Vector2(1180f, 460f), out var rootImage, out var group);
        rootImage.color = new Color(0.02f, 0.02f, 0.07f, 0.98f);

        var backdrop = CreatePanel("Backdrop", root.transform as RectTransform, new Color(0.04f, 0.03f, 0.10f, 0.90f));
        backdrop.sizeDelta = new Vector2(1180f, 460f);

        var hint = CreateText("Hint", root.transform as RectTransform, "GlitchText  |  hover to trigger RGB slices", 30, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.95f, 0.97f, 1f, 1f));
        hint.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        hint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        hint.rectTransform.pivot = new Vector2(0.5f, 1f);
        hint.rectTransform.sizeDelta = new Vector2(900f, 52f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -40f);

        var container = CreateRect("GlitchContainer", backdrop, Vector2.zero, new Vector2(900f, 220f));
        var containerHit = container.gameObject.AddComponent<Image>();
        containerHit.color = new Color(1f, 1f, 1f, 0.001f);
        containerHit.raycastTarget = true;

        var main = CreateText("MainText", container, "GLITCH", 132, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(main.rectTransform);

        var afterMask = CreateRect("AfterMask", container, Vector2.zero, new Vector2(0f, 68f));
        Stretch(afterMask);
        afterMask.gameObject.AddComponent<RectMask2D>();
        var afterText = CreateText("AfterText", afterMask, "GLITCH", 132, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(afterText.rectTransform);

        var beforeMask = CreateRect("BeforeMask", container, Vector2.zero, new Vector2(0f, 58f));
        Stretch(beforeMask);
        beforeMask.gameObject.AddComponent<RectMask2D>();
        var beforeText = CreateText("BeforeText", beforeMask, "GLITCH", 132, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(beforeText.rectTransform);

        var effect = root.AddComponent<ReplicaV3GlitchTextEffect>();
        effect.EffectKey = "glitch-text-v3";
        effect.EffectDisplayName = "GlitchText V3";
        effect.UsageDescription = "故障风/乱码文字效果，支持悬停触发。";
        effect.EffectRoot = root.transform as RectTransform;
        effect.EffectCanvasGroup = group;
        effect.ContentRoot = backdrop;
        effect.GlitchContainer = container;
        effect.MainRect = main.rectTransform;
        effect.AfterMask = afterMask;
        effect.BeforeMask = beforeMask;
        effect.AfterTextRect = afterText.rectTransform;
        effect.BeforeTextRect = beforeText.rectTransform;
        effect.MainText = main;
        effect.AfterText = afterText;
        effect.BeforeText = beforeText;
        effect.HintText = hint;
        effect.InteractionHitSource = container;
        effect.ShowInteractionRange = true;

        return SavePrefabAndGetComponent<ReplicaV3GlitchTextEffect>(root, Path.Combine(PrefabFolder, "GlitchText_V3.prefab"));
    }

    private static ReplicaV3EffectBase BuildGradientTextPrefab()
    {
        var root = CreateRoot("GradientText_V3", new Vector2(720f, 140f), out _, out var group);

        var content = CreateRect("Content", root.transform as RectTransform, Vector2.zero, new Vector2(720f, 140f));
        var frame = CreateRect("Frame", content, Vector2.zero, content.sizeDelta);
        Stretch(frame);
        frame.gameObject.AddComponent<RectMask2D>();

        var borderClip = CreateRect("BorderGradientClip", frame, Vector2.zero, frame.sizeDelta);
        Stretch(borderClip);
        borderClip.gameObject.AddComponent<RectMask2D>();

        var borderGradient = CreateRect("BorderGradient", borderClip, Vector2.zero, frame.sizeDelta);
        Stretch(borderGradient);
        var borderImage = borderGradient.gameObject.AddComponent<Image>();
        borderImage.color = Color.white;
        borderImage.raycastTarget = false;

        var inner = CreatePanel("InnerBackground", frame, new Color(0.024f, 0.00f, 0.063f, 0.90f));
        Stretch(inner);
        var textMask = CreateRect("TextMask", inner, Vector2.zero, inner.sizeDelta);
        Stretch(textMask);

        var maskText = CreateText("MaskText", textMask, "GradientText", 56, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(maskText.rectTransform);
        var mask = maskText.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var textGradient = CreateRect("TextGradient", maskText.rectTransform, Vector2.zero, maskText.rectTransform.sizeDelta);
        Stretch(textGradient);
        var textImage = textGradient.gameObject.AddComponent<Image>();
        textImage.color = Color.white;
        textImage.raycastTarget = false;

        var hit = root.GetComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0.001f);
        hit.raycastTarget = true;

        var effect = root.AddComponent<ReplicaV3GradientTextEffect>();
        effect.EffectKey = "gradient-text-v3";
        effect.EffectDisplayName = "GradientText V3";
        effect.UsageDescription = "动态渐变文字。";
        effect.EffectRoot = root.transform as RectTransform;
        effect.EffectCanvasGroup = group;
        effect.ContentRoot = content;
        effect.Frame = frame;
        effect.BorderGradientClip = borderClip;
        effect.BorderGradientRect = borderGradient;
        effect.BorderGradientImage = borderImage;
        effect.InnerBackground = inner;
        effect.TextGradientRect = textGradient;
        effect.TextGradientImage = textImage;
        effect.MaskText = maskText;
        effect.InteractionHitSource = frame;
        effect.ShowInteractionRange = true;

        return SavePrefabAndGetComponent<ReplicaV3GradientTextEffect>(root, Path.Combine(PrefabFolder, "GradientText_V3.prefab"));
    }

    private static ReplicaV3EffectBase BuildGridMotionPrefab()
    {
        var root = CreateRoot("GridMotion_V3", new Vector2(1100f, 700f), out var rootImage, out var group);
        rootImage.color = Color.black;

        var container = CreateRect("Container", root.transform as RectTransform, Vector2.zero, new Vector2(1100f, 700f));
        container.localRotation = Quaternion.Euler(0f, 0f, -15f);
        container.localScale = new Vector3(1.5f, 1.5f, 1f);
        Stretch(container);

        var rowsRoot = CreateRect("RowsRoot", container, Vector2.zero, container.sizeDelta);
        Stretch(rowsRoot);
        var rowsLayout = rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 16f;
        rowsLayout.childControlHeight = true;
        rowsLayout.childControlWidth = true;
        rowsLayout.childForceExpandHeight = true;
        rowsLayout.childForceExpandWidth = true;

        var rowContents = new List<RectTransform>();
        var grids = new List<GridLayoutGroup>();

        for (var row = 0; row < 4; row++)
        {
            var slot = CreateRect($"RowSlot_{row}", rowsRoot, Vector2.zero, Vector2.zero);
            var slotLayout = slot.gameObject.AddComponent<LayoutElement>();
            slotLayout.flexibleWidth = 1f;
            slotLayout.flexibleHeight = 1f;

            var rowRect = CreateRect($"Row_{row}", slot, Vector2.zero, Vector2.zero);
            Stretch(rowRect);
            var grid = rowRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.spacing = new Vector2(16f, 16f);
            grid.cellSize = new Vector2(100f, 100f);
            grids.Add(grid);
            rowContents.Add(rowRect);

            for (var col = 0; col < 7; col++)
            {
                var tile = CreatePanel($"Tile_{row}_{col}", rowRect, new Color(0.067f, 0.067f, 0.067f, 1f));
                var text = CreateText("Text", tile, $"Item {row * 7 + col + 1}", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                Stretch(text.rectTransform);
            }
        }

        var effect = root.AddComponent<ReplicaV3GridMotionEffect>();
        effect.EffectKey = "grid-motion-v3";
        effect.EffectDisplayName = "GridMotion V3";
        effect.UsageDescription = "网格布局动态位移。";
        effect.EffectRoot = root.transform as RectTransform;
        effect.EffectCanvasGroup = group;
        effect.RowsRoot = rowsRoot;
        effect.RowContents = rowContents;
        effect.RowGridLayouts = grids;
        effect.InteractionHitSource = root.transform as RectTransform;
        effect.ShowInteractionRange = true;

        return SavePrefabAndGetComponent<ReplicaV3GridMotionEffect>(root, Path.Combine(PrefabFolder, "GridMotion_V3.prefab"));
    }

    private static ReplicaV3EffectBase BuildMenuRevealStaggerLayeredPrefab()
    {
        var root = CreateRoot("MenuRevealStaggerLayered_V3", new Vector2(1280f, 720f), out var rootImage, out var group);
        rootImage.color = new Color(0.05f, 0.07f, 0.14f, 0.92f);

        var topBar = CreateRect("TopBar", root.transform as RectTransform, new Vector2(0f, 0f), new Vector2(0f, 108f));
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);

        var toggle = CreatePanel("Toggle", topBar, new Color(1f, 1f, 1f, 0f));
        toggle.anchorMin = new Vector2(1f, 0.5f);
        toggle.anchorMax = new Vector2(1f, 0.5f);
        toggle.pivot = new Vector2(1f, 0.5f);
        toggle.sizeDelta = new Vector2(182f, 52f);
        toggle.anchoredPosition = new Vector2(-30f, 0f);

        var toggleMask = CreateRect("ToggleLabelMask", toggle, Vector2.zero, new Vector2(116f, 30f));
        toggleMask.anchorMin = new Vector2(0f, 0.5f);
        toggleMask.anchorMax = new Vector2(0f, 0.5f);
        toggleMask.pivot = new Vector2(0f, 0.5f);
        toggleMask.gameObject.AddComponent<RectMask2D>();

        var toggleStack = CreateRect("ToggleTextStack", toggleMask, Vector2.zero, new Vector2(0f, 60f));
        toggleStack.anchorMin = new Vector2(0f, 1f);
        toggleStack.anchorMax = new Vector2(1f, 1f);
        toggleStack.pivot = new Vector2(0.5f, 1f);

        var toggleTop = CreateText("MenuLine", toggleStack, "Menu", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.93f, 0.95f, 1f, 1f));
        toggleTop.rectTransform.anchorMin = new Vector2(0f, 1f);
        toggleTop.rectTransform.anchorMax = new Vector2(1f, 1f);
        toggleTop.rectTransform.pivot = new Vector2(0.5f, 1f);
        toggleTop.rectTransform.sizeDelta = new Vector2(0f, 30f);

        var toggleBottom = CreateText("CloseLine", toggleStack, "Close", 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.12f, 0.14f, 0.22f, 1f));
        toggleBottom.rectTransform.anchorMin = new Vector2(0f, 1f);
        toggleBottom.rectTransform.anchorMax = new Vector2(1f, 1f);
        toggleBottom.rectTransform.pivot = new Vector2(0.5f, 1f);
        toggleBottom.rectTransform.sizeDelta = new Vector2(0f, 30f);
        toggleBottom.rectTransform.anchoredPosition = new Vector2(0f, -30f);

        var icon = CreateRect("Icon", toggle, new Vector2(-8f, 0f), new Vector2(24f, 24f));
        icon.anchorMin = new Vector2(1f, 0.5f);
        icon.anchorMax = new Vector2(1f, 0.5f);
        icon.pivot = new Vector2(1f, 0.5f);

        var preLayersRoot = CreateRect("PreLayers", root.transform as RectTransform, Vector2.zero, new Vector2(470f, 0f));
        preLayersRoot.anchorMin = new Vector2(1f, 0f);
        preLayersRoot.anchorMax = new Vector2(1f, 1f);
        preLayersRoot.pivot = new Vector2(1f, 0.5f);

        var layer0 = CreatePanel("Layer_0", preLayersRoot, new Color(0.63f, 0.53f, 0.95f, 0.86f));
        var layer1 = CreatePanel("Layer_1", preLayersRoot, new Color(0.32f, 0.15f, 1.00f, 0.72f));
        var layer2 = CreatePanel("Layer_2", preLayersRoot, new Color(0.16f, 0.09f, 0.55f, 0.58f));
        Stretch(layer0);
        Stretch(layer1);
        Stretch(layer2);

        var panel = CreatePanel("Panel", root.transform as RectTransform, new Color(0.96f, 0.97f, 1f, 0.98f));
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.sizeDelta = new Vector2(470f, 0f);

        var panelInner = CreateRect("PanelInner", panel, Vector2.zero, panel.sizeDelta);
        panelInner.anchorMin = Vector2.zero;
        panelInner.anchorMax = Vector2.one;
        panelInner.offsetMin = new Vector2(38f, 36f);
        panelInner.offsetMax = new Vector2(-36f, -132f);

        var menuList = CreateRect("MenuList", panelInner, Vector2.zero, new Vector2(0f, 420f));
        menuList.anchorMin = new Vector2(0f, 1f);
        menuList.anchorMax = new Vector2(1f, 1f);
        menuList.pivot = new Vector2(0.5f, 1f);

        var itemNames = new[] { "HOME", "WORKS", "SERVICES", "JOURNAL", "CONTACT" };
        var itemLabels = new List<RectTransform>();
        var itemNumbers = new List<Text>();

        for (var i = 0; i < itemNames.Length; i++)
        {
            var item = CreateRect($"Item_{i}", menuList, new Vector2(0f, -i * 74f), new Vector2(0f, 72f));
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0f, 1f);

            var label = CreateText("Label", item, itemNames[i], 52, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.09f, 0.10f, 0.14f, 1f));
            label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(0f, 70f);

            var number = CreateText("Number", item, (i + 1).ToString("00"), 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.32f, 0.15f, 1f, 1f));
            number.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            number.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            number.rectTransform.pivot = new Vector2(1f, 0.5f);
            number.rectTransform.sizeDelta = new Vector2(74f, 32f);
            number.rectTransform.anchoredPosition = new Vector2(0f, 8f);

            itemLabels.Add(label.rectTransform);
            itemNumbers.Add(number);
        }

        var socials = CreateRect("Socials", panelInner, Vector2.zero, new Vector2(0f, 160f));
        socials.anchorMin = new Vector2(0f, 0f);
        socials.anchorMax = new Vector2(1f, 0f);
        socials.pivot = new Vector2(0.5f, 0f);

        var socialTitle = CreateText("SocialTitle", socials, "Socials", 24, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.32f, 0.15f, 1f, 1f));
        socialTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        socialTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        socialTitle.rectTransform.pivot = new Vector2(0f, 1f);
        socialTitle.rectTransform.sizeDelta = new Vector2(0f, 38f);

        var socialNames = new[] { "GitHub", "Dribbble", "Behance" };
        var socialLinks = new List<RectTransform>();
        for (var i = 0; i < socialNames.Length; i++)
        {
            var link = CreateText($"Social_{i}", socials, socialNames[i], 26, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.12f, 0.13f, 0.20f, 1f));
            link.rectTransform.anchorMin = new Vector2(0f, 1f);
            link.rectTransform.anchorMax = new Vector2(1f, 1f);
            link.rectTransform.pivot = new Vector2(0f, 1f);
            link.rectTransform.sizeDelta = new Vector2(0f, 34f);
            link.rectTransform.anchoredPosition = new Vector2(0f, -52f - (i * 34f));
            socialLinks.Add(link.rectTransform);
        }

        var dismiss = CreatePanel("Dismiss", root.transform as RectTransform, new Color(0f, 0f, 0f, 0f));
        Stretch(dismiss);
        var dismissGroup = dismiss.gameObject.AddComponent<CanvasGroup>();
        dismissGroup.blocksRaycasts = false;
        dismissGroup.interactable = false;

        var effect = root.AddComponent<ReplicaV3MenuRevealStaggerLayeredEffect>();
        effect.EffectKey = "menu-reveal-stagger-layered-v3";
        effect.EffectDisplayName = "MenuRevealStaggerLayered V3";
        effect.UsageDescription = "菜单分层交错揭示。";
        effect.EffectRoot = root.transform as RectTransform;
        effect.EffectCanvasGroup = group;
        effect.Panel = panel;
        effect.PreLayers = new List<RectTransform> { layer0, layer1, layer2 };
        effect.ItemLabels = itemLabels;
        effect.ItemNumbers = itemNumbers;
        effect.SocialLinks = socialLinks;
        effect.SocialTitle = socialTitle;
        effect.ToggleIcon = icon;
        effect.ToggleTextStack = toggleStack;
        effect.ToggleTextTop = toggleTop;
        effect.DismissGroup = dismissGroup;

        return SavePrefabAndGetComponent<ReplicaV3MenuRevealStaggerLayeredEffect>(root, Path.Combine(PrefabFolder, "MenuRevealStaggerLayered_V3.prefab"));
    }

    private static void RegisterEntriesInScene(string scenePath, List<ReplicaV3EffectBase> built)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning($"[ReplicaV3] Scene not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var controller = GameObject.FindFirstObjectByType<ReplicaV3ShowcaseController>();
        if (controller == null)
        {
            Debug.LogWarning($"[ReplicaV3] Showcase controller not found in: {scenePath}");
            return;
        }

        if (controller.Effects == null)
        {
            controller.Effects = new List<ReplicaV3ShowcaseEffectEntry>();
        }

        AddOrUpdateEntry(controller, "glass-icons-tilt-v3", "GlassIconsTilt V3", "玻璃感图标倾斜跟随。", built, "GlassIconsTilt_V3");
        AddOrUpdateEntry(controller, "glitch-text-v3", "GlitchText V3", "故障风/乱码文字效果。", built, "GlitchText_V3");
        AddOrUpdateEntry(controller, "gradient-text-v3", "GradientText V3", "动态渐变文字。", built, "GradientText_V3");
        AddOrUpdateEntry(controller, "grid-motion-v3", "GridMotion V3", "网格布局动态位移。", built, "GridMotion_V3");
        AddOrUpdateEntry(controller, "menu-reveal-stagger-layered-v3", "MenuRevealStaggerLayered V3", "菜单分层交错揭示。", built, "MenuRevealStaggerLayered_V3");

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddOrUpdateEntry(
        ReplicaV3ShowcaseController controller,
        string id,
        string displayName,
        string description,
        List<ReplicaV3EffectBase> built,
        string prefabPrefix)
    {
        ReplicaV3EffectBase prefab = null;
        for (var i = 0; i < built.Count; i++)
        {
            var item = built[i];
            if (item != null && item.name.StartsWith(prefabPrefix, StringComparison.Ordinal))
            {
                prefab = item;
                break;
            }
        }

        if (prefab == null)
        {
            var path = Path.Combine(PrefabFolder, prefabPrefix + ".prefab").Replace("\\", "/");
            prefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(path);
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[ReplicaV3] Missing prefab for entry: {id}");
            return;
        }

        for (var i = 0; i < controller.Effects.Count; i++)
        {
            var entry = controller.Effects[i];
            if (entry != null && string.Equals(entry.EffectId, id, StringComparison.Ordinal))
            {
                entry.DisplayName = displayName;
                entry.Description = description;
                entry.EffectPrefab = prefab;
                return;
            }
        }

        controller.Effects.Add(new ReplicaV3ShowcaseEffectEntry
        {
            EffectId = id,
            DisplayName = displayName,
            Description = description,
            EffectPrefab = prefab
        });
    }

    private static GameObject CreateRoot(string name, Vector2 size, out Image image, out CanvasGroup group)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        var rect = go.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        image = go.GetComponent<Image>();
        image.raycastTarget = true;

        group = go.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
        return go;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform CreatePanel(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Text CreateText(string name, RectTransform parent, string text, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var rect = go.transform as RectTransform;
        rect.SetParent(parent, false);

        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = ResolveBuiltinFont();
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static Font ResolveBuiltinFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (ArgumentException)
        {
        }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (ArgumentException)
            {
            }
        }

        return font;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static T SavePrefabAndGetComponent<T>(GameObject root, string prefabPath) where T : Component
    {
        var normalized = prefabPath.Replace("\\", "/");
        EnsureFolder(Path.GetDirectoryName(normalized)?.Replace("\\", "/"));

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, normalized);
        UnityEngine.Object.DestroyImmediate(root);

        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        folder = folder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        var parts = folder.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
