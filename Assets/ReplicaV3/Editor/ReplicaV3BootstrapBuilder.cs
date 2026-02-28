using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ReplicaV3BootstrapBuilder
{
    private const string RootFolder = "Assets/ReplicaV3";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string EffectPrefabFolder = PrefabFolder + "/Effects";
    private const string SceneFolder = RootFolder + "/Scenes";

    [MenuItem("ReplicaV3/Build/重建 V3 基础资产")]
    public static void BuildAll()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(EffectPrefabFolder);
        EnsureFolder(SceneFolder);

        var shinyPath = BuildShinyTextPrefab();
        var magnetPath = BuildMagnetPrefab();
        var countUpPath = BuildCountUpPrefab();

        var overlayScenePath = BuildShowcaseScene("ReplicaV3_Showcase_Overlay", ReplicaV3CanvasMode.Overlay, shinyPath, magnetPath, countUpPath);
        BuildShowcaseScene("ReplicaV3_Showcase_World", ReplicaV3CanvasMode.WorldSpace, shinyPath, magnetPath, countUpPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(overlayScenePath, OpenSceneMode.Single);
        Debug.Log("[ReplicaV3] 基础资产生成完成：3 个动效预制体 + 2 个 Showcase 场景。");
    }

    private static string BuildShinyTextPrefab()
    {
        var root = new GameObject("ShinyText_V3", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(920f, 220f);

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0.001f);
        rootImage.raycastTarget = true;

        var canvasGroup = root.GetComponent<CanvasGroup>();

        var backgroundImage = ReplicaV3UIFactory.CreateImage(
            "Demo_Background",
            rootRect,
            new Color(0.14f, 0.17f, 0.28f, 0.92f),
            false);
        var backgroundRect = backgroundImage.rectTransform;
        SetRect(backgroundRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 220f));
        var backgroundShadow = ReplicaV3UIFactory.EnsureComponent<Shadow>(backgroundImage.gameObject);
        backgroundShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        backgroundShadow.effectDistance = new Vector2(0f, -8f);

        var contentRect = ReplicaV3UIFactory.CreateRect("Content", rootRect);
        SetRect(contentRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(860f, 132f));

        var text = ReplicaV3UIFactory.CreateText(
            "MaskText",
            contentRect,
            "Shiny Text",
            88,
            TextAnchor.MiddleCenter,
            Color.white,
            FontStyle.Bold);
        var textRect = (RectTransform)text.transform;
        ReplicaV3UIFactory.Stretch(textRect);

        var mask = ReplicaV3UIFactory.EnsureComponent<Mask>(text.gameObject);
        mask.showMaskGraphic = false;

        var shineImage = ReplicaV3UIFactory.CreateImage("ShineBand", text.transform, Color.white, false);
        var shineRect = shineImage.rectTransform;
        ReplicaV3UIFactory.Stretch(shineRect);

        var hintText = ReplicaV3UIFactory.CreateText(
            "Demo_Hint",
            rootRect,
            "Demo 模式：悬停可暂停扫光，纯净模式可一键清理视觉噪音",
            18,
            TextAnchor.LowerCenter,
            new Color(0.73f, 0.84f, 1f, 0.95f),
            FontStyle.Normal,
            false);
        var hintRect = (RectTransform)hintText.transform;
        SetRect(hintRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(860f, 24f));

        var effect = root.AddComponent<ReplicaV3ShinyTextEffect>();
        effect.EffectKey = "shiny-text-v3";
        effect.EffectDisplayName = "ShinyText V3";
        effect.UsageDescription = "用于复刻带扫光的标题文本。核心参数是扫光时长、停顿、宽度倍数；可切换悬停暂停和往返模式。";
        effect.EffectRoot = rootRect;
        effect.EffectCanvasGroup = canvasGroup;
        effect.ContentRect = contentRect;
        effect.DisplayTextLabel = text;
        effect.ShineBandRect = shineRect;
        effect.ShineBandImage = shineImage;
        effect.RaycastSurface = rootImage;
        effect.DisplayText = "Shiny Text";
        effect.DemoOnlyObjects = new List<GameObject> { hintText.gameObject };
        effect.DemoTintGraphics = new List<Graphic> { backgroundImage, hintText };

        var path = $"{EffectPrefabFolder}/ShinyText_V3.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
        return path;
    }

    private static string BuildMagnetPrefab()
    {
        var root = new GameObject("Magnet_V3", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(760f, 420f);

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0.001f);
        rootImage.raycastTarget = true;
        var canvasGroup = root.GetComponent<CanvasGroup>();

        var frameImage = ReplicaV3UIFactory.CreateImage("Demo_Frame", rootRect, new Color(0.07f, 0.10f, 0.17f, 0.95f), false);
        var frameRect = frameImage.rectTransform;
        SetRect(frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
        var frameOutline = ReplicaV3UIFactory.EnsureComponent<Outline>(frameImage.gameObject);
        frameOutline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        frameOutline.effectDistance = new Vector2(1f, -1f);

        var hintText = ReplicaV3UIFactory.CreateText(
            "Hint",
            frameRect,
            "把鼠标移到按钮附近，观察吸附与倾斜",
            20,
            TextAnchor.UpperCenter,
            new Color(0.80f, 0.90f, 1f, 0.96f),
            FontStyle.Bold,
            false);
        var hintRect = (RectTransform)hintText.transform;
        SetRect(hintRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 32f));

        var surfaceImage = ReplicaV3UIFactory.CreateImage("Surface", frameRect, new Color(0.10f, 0.14f, 0.24f, 0.98f), false);
        var surfaceRect = surfaceImage.rectTransform;
        SetRect(surfaceRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(640f, 300f));
        var surfaceOutline = ReplicaV3UIFactory.EnsureComponent<Outline>(surfaceImage.gameObject);
        surfaceOutline.effectColor = new Color(1f, 1f, 1f, 0.13f);
        surfaceOutline.effectDistance = new Vector2(1f, -1f);

        var floatingRoot = ReplicaV3UIFactory.CreateRect("FloatingRoot", surfaceRect);
        ReplicaV3UIFactory.Stretch(floatingRoot);

        var buttonImage = ReplicaV3UIFactory.CreateImage("MagnetCard", floatingRoot, new Color(0.95f, 0.97f, 1f, 0.98f), false);
        var buttonRect = buttonImage.rectTransform;
        SetRect(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(360f, 112f));
        var buttonShadow = ReplicaV3UIFactory.EnsureComponent<Shadow>(buttonImage.gameObject);
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
        buttonShadow.effectDistance = new Vector2(0f, -8f);

        var titleText = ReplicaV3UIFactory.CreateText(
            "Title",
            buttonRect,
            "Hover Me",
            40,
            TextAnchor.MiddleCenter,
            new Color(0.08f, 0.13f, 0.22f, 1f),
            FontStyle.Bold,
            false);
        ReplicaV3UIFactory.Stretch((RectTransform)titleText.transform);

        var effect = root.AddComponent<ReplicaV3MagnetEffect>();
        effect.EffectKey = "magnet-v3";
        effect.EffectDisplayName = "Magnet V3";
        effect.UsageDescription = "用于复刻磁吸按钮/卡片。核心参数是磁吸强度、最大偏移和倾斜角；可快速关停磁吸查看纯视觉布局。";
        effect.EffectRoot = rootRect;
        effect.EffectCanvasGroup = canvasGroup;
        effect.InteractionSurface = surfaceRect;
        effect.FloatingRoot = floatingRoot;
        effect.TiltRoot = floatingRoot;
        effect.TitleText = titleText;
        effect.HintText = hintText;
        effect.Title = "Hover Me";
        effect.Hint = "Move pointer near the card";
        effect.DemoOnlyObjects = new List<GameObject> { hintText.gameObject };
        effect.DemoTintGraphics = new List<Graphic> { frameImage, surfaceImage, hintText };

        var path = $"{EffectPrefabFolder}/Magnet_V3.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
        return path;
    }

    private static string BuildCountUpPrefab()
    {
        var root = new GameObject("CountUp_V3", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(980f, 460f);

        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.06f, 0.08f, 0.13f, 0.96f);
        rootImage.raycastTarget = false;
        var canvasGroup = root.GetComponent<CanvasGroup>();

        var ambientImage = ReplicaV3UIFactory.CreateImage("Demo_Ambient", rootRect, new Color(0.24f, 0.37f, 0.74f, 0.22f), false);
        var ambientRect = ambientImage.rectTransform;
        SetRect(ambientRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, 90f), new Vector2(1100f, 520f));
        ambientRect.localRotation = Quaternion.Euler(0f, 0f, 11f);

        var panelImage = ReplicaV3UIFactory.CreateImage("Panel", rootRect, new Color(0.11f, 0.14f, 0.24f, 0.97f), false);
        var panelRect = panelImage.rectTransform;
        SetRect(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(920f, 400f));
        var panelShadow = ReplicaV3UIFactory.EnsureComponent<Shadow>(panelImage.gameObject);
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        panelShadow.effectDistance = new Vector2(0f, -10f);

        var titleText = ReplicaV3UIFactory.CreateText(
            "Title",
            panelRect,
            "Count Up",
            34,
            TextAnchor.UpperCenter,
            new Color(0.93f, 0.96f, 1f, 1f),
            FontStyle.Bold,
            false);
        var titleRect = (RectTransform)titleText.transform;
        SetRect(titleRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(820f, 36f));

        var valueWrap = ReplicaV3UIFactory.CreateRect("ValueWrap", panelRect);
        SetRect(valueWrap, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(820f, 180f));

        var valueGlow = ReplicaV3UIFactory.CreateImage("ValueGlow", valueWrap, new Color(0.24f, 0.33f, 0.74f, 0.30f), false);
        ReplicaV3UIFactory.Stretch(valueGlow.rectTransform);

        var valueText = ReplicaV3UIFactory.CreateText(
            "Value",
            valueWrap,
            "0",
            128,
            TextAnchor.MiddleCenter,
            Color.white,
            FontStyle.Bold,
            false);
        ReplicaV3UIFactory.Stretch((RectTransform)valueText.transform);

        var progressBg = ReplicaV3UIFactory.CreateImage("ProgressBG", panelRect, new Color(1f, 1f, 1f, 0.14f), false);
        var progressBgRect = progressBg.rectTransform;
        SetRect(progressBgRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(760f, 16f));

        var progressFill = ReplicaV3UIFactory.CreateImage("ProgressFill", progressBgRect, new Color(0.38f, 0.60f, 1f, 0.98f), false);
        var progressFillRect = progressFill.rectTransform;
        ReplicaV3UIFactory.Stretch(progressFillRect);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        var hintText = ReplicaV3UIFactory.CreateText(
            "Hint",
            panelRect,
            "Auto retargeting demo · 可切换倒计数",
            22,
            TextAnchor.MiddleCenter,
            new Color(0.77f, 0.84f, 0.97f, 0.95f),
            FontStyle.Bold,
            false);
        var hintRect = (RectTransform)hintText.transform;
        SetRect(hintRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(760f, 28f));

        var effect = root.AddComponent<ReplicaV3CountUpEffect>();
        effect.EffectKey = "count-up-v3";
        effect.EffectDisplayName = "CountUp V3";
        effect.UsageDescription = "用于复刻数字增长/翻牌类统计动效。核心参数是计数时长、循环间隔、开始延迟，并支持倒计数模式。";
        effect.EffectRoot = rootRect;
        effect.EffectCanvasGroup = canvasGroup;
        effect.TitleText = titleText;
        effect.ValueText = valueText;
        effect.HintText = hintText;
        effect.ValuePulseRoot = valueWrap;
        effect.ProgressFillImage = progressFill;
        effect.Title = "Count Up";
        effect.Hint = "Auto retargeting demo";
        effect.DemoOnlyObjects = new List<GameObject> { ambientImage.gameObject, hintText.gameObject };
        effect.DemoTintGraphics = new List<Graphic> { ambientImage, hintText };

        var path = $"{EffectPrefabFolder}/CountUp_V3.prefab";
        SavePrefab(root, path);
        Object.DestroyImmediate(root);
        return path;
    }

    private static string BuildShowcaseScene(
        string sceneName,
        ReplicaV3CanvasMode mode,
        string shinyPrefabPath,
        string magnetPrefabPath,
        string countUpPrefabPath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = BuildCamera(mode);
        BuildEventSystem();

        var canvas = BuildCanvas(mode, camera);
        var rootRect = BuildRootLayout(canvas.transform, out var leftPanel, out var rightPanel);

        var headerTitle = ReplicaV3UIFactory.CreateText(
            "Title",
            leftPanel,
            "Replica V3 Showcase",
            22,
            TextAnchor.UpperLeft,
            Color.white,
            FontStyle.Bold,
            false);
        var headerRect = (RectTransform)headerTitle.transform;
        SetRect(headerRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -86f), new Vector2(-16f, -16f), true);

        var switchButton = ReplicaV3UIFactory.CreateButton(
            "SwitchSceneButton",
            leftPanel,
            mode == ReplicaV3CanvasMode.Overlay ? "切到 World Space" : "切到 Overlay",
            new Color(0.25f, 0.42f, 0.74f, 0.98f),
            Color.white,
            out var switchButtonText);
        var switchRect = switchButton.GetComponent<RectTransform>();
        SetRect(switchRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -136f), new Vector2(-16f, -96f), true);

        var controlsRow = ReplicaV3UIFactory.CreateRect("ControlButtons", leftPanel);
        SetRect(controlsRow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -244f), new Vector2(-16f, -148f), true);
        var controlsGrid = ReplicaV3UIFactory.EnsureComponent<GridLayoutGroup>(controlsRow.gameObject);
        controlsGrid.cellSize = new Vector2(130f, 42f);
        controlsGrid.spacing = new Vector2(8f, 8f);
        controlsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        controlsGrid.constraintCount = 2;

        var playInButton = ReplicaV3UIFactory.CreateButton("PlayIn", controlsRow, "PlayIn", new Color(0.20f, 0.34f, 0.56f, 1f), Color.white, out _);
        var playOutButton = ReplicaV3UIFactory.CreateButton("PlayOut", controlsRow, "PlayOut", new Color(0.20f, 0.34f, 0.56f, 1f), Color.white, out _);
        var resetButton = ReplicaV3UIFactory.CreateButton("Reset", controlsRow, "Reset", new Color(0.20f, 0.34f, 0.56f, 1f), Color.white, out _);
        var demoToggleButton = ReplicaV3UIFactory.CreateButton("DemoToggle", controlsRow, "切到纯净模式", new Color(0.20f, 0.34f, 0.56f, 1f), Color.white, out var demoToggleText);

        var listLabel = ReplicaV3UIFactory.CreateText(
            "EffectListLabel",
            leftPanel,
            "动效列表",
            22,
            TextAnchor.MiddleLeft,
            new Color(0.80f, 0.86f, 0.99f, 0.98f),
            FontStyle.Bold,
            false);
        var listLabelRect = (RectTransform)listLabel.transform;
        SetRect(listLabelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(16f, -288f), new Vector2(-16f, -258f), true);

        var listPanel = ReplicaV3UIFactory.CreateImage("EffectListPanel", leftPanel, new Color(0.10f, 0.12f, 0.18f, 0.92f), false);
        var listPanelRect = listPanel.rectTransform;
        SetRect(listPanelRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(-16f, -298f), true);

        var listViewport = ReplicaV3UIFactory.CreateImage("Viewport", listPanelRect, new Color(0f, 0f, 0f, 0.01f), false);
        var listViewportRect = listViewport.rectTransform;
        ReplicaV3UIFactory.Stretch(listViewportRect);
        var listMask = ReplicaV3UIFactory.EnsureComponent<Mask>(listViewport.gameObject);
        listMask.showMaskGraphic = false;

        var listContainer = ReplicaV3UIFactory.CreateRect("EffectButtons", listViewportRect);
        listContainer.anchorMin = new Vector2(0f, 1f);
        listContainer.anchorMax = new Vector2(1f, 1f);
        listContainer.pivot = new Vector2(0.5f, 1f);
        listContainer.anchoredPosition = Vector2.zero;
        listContainer.sizeDelta = Vector2.zero;

        var listLayout = ReplicaV3UIFactory.EnsureComponent<VerticalLayoutGroup>(listContainer.gameObject);
        listLayout.padding = new RectOffset(10, 10, 10, 10);
        listLayout.spacing = 8f;
        listLayout.childControlHeight = true;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childForceExpandWidth = true;
        var listFitter = ReplicaV3UIFactory.EnsureComponent<ContentSizeFitter>(listContainer.gameObject);
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var listScroll = ReplicaV3UIFactory.EnsureComponent<ScrollRect>(listPanel.gameObject);
        listScroll.horizontal = false;
        listScroll.vertical = true;
        listScroll.viewport = listViewportRect;
        listScroll.content = listContainer;
        listScroll.movementType = ScrollRect.MovementType.Clamped;
        listScroll.scrollSensitivity = 20f;

        var effectButtonTemplate = ReplicaV3UIFactory.CreateButton(
            "EffectButtonTemplate",
            listContainer,
            "Template",
            new Color(0.20f, 0.24f, 0.33f, 0.95f),
            new Color(0.87f, 0.91f, 0.99f, 0.95f),
            out _);
        var templateElement = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(effectButtonTemplate.gameObject);
        templateElement.preferredHeight = 44f;
        effectButtonTemplate.gameObject.SetActive(false);

        var currentTitle = ReplicaV3UIFactory.CreateText(
            "CurrentEffectTitle",
            rightPanel,
            "当前动效",
            36,
            TextAnchor.UpperLeft,
            Color.white,
            FontStyle.Bold,
            false);
        var currentTitleRect = (RectTransform)currentTitle.transform;
        SetRect(currentTitleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -58f), new Vector2(-396f, -16f), true);

        var currentDesc = ReplicaV3UIFactory.CreateText(
            "CurrentEffectDesc",
            rightPanel,
            "动效说明",
            20,
            TextAnchor.UpperLeft,
            new Color(0.76f, 0.83f, 0.97f, 0.95f),
            FontStyle.Normal,
            false);
        var currentDescRect = (RectTransform)currentDesc.transform;
        SetRect(currentDescRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(18f, -112f), new Vector2(-396f, -60f), true);

        var stagePanel = ReplicaV3UIFactory.CreateImage("StagePanel", rightPanel, new Color(0.09f, 0.11f, 0.16f, 0.96f), false);
        var stageRect = stagePanel.rectTransform;
        SetRect(stageRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(18f, 20f), new Vector2(-388f, -126f), true);
        var stageOutline = ReplicaV3UIFactory.EnsureComponent<Outline>(stagePanel.gameObject);
        stageOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        stageOutline.effectDistance = new Vector2(1f, -1f);

        var effectMount = ReplicaV3UIFactory.CreateRect("EffectMount", stageRect);
        SetRect(effectMount, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(-14f, -14f), true);

        var parameterPanel = ReplicaV3UIFactory.CreateImage("ParameterPanel", rightPanel, new Color(0.12f, 0.15f, 0.22f, 0.96f), false);
        var parameterRect = parameterPanel.rectTransform;
        SetRect(parameterRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-378f, 18f), new Vector2(-18f, -18f), true);

        var foldButton = ReplicaV3UIFactory.CreateButton(
            "FoldButton",
            parameterRect,
            "参数面板 ︽",
            new Color(0.21f, 0.34f, 0.58f, 1f),
            Color.white,
            out var foldLabel);
        var foldRect = foldButton.GetComponent<RectTransform>();
        SetRect(foldRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(10f, -42f), new Vector2(-10f, -10f), true);

        var panelBody = ReplicaV3UIFactory.CreateRect("Body", parameterRect);
        SetRect(panelBody, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(-10f, -50f), true);
        var panelLayout = ReplicaV3UIFactory.EnsureComponent<VerticalLayoutGroup>(panelBody.gameObject);
        panelLayout.spacing = 8f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        var panelEffectTitle = ReplicaV3UIFactory.CreateText(
            "EffectTitle",
            panelBody,
            "未选择动效",
            22,
            TextAnchor.MiddleLeft,
            Color.white,
            FontStyle.Bold,
            false);
        var panelEffectTitleLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(panelEffectTitle.gameObject);
        panelEffectTitleLayout.preferredHeight = 30f;

        var panelGuide = ReplicaV3UIFactory.CreateText(
            "Guide",
            panelBody,
            "参数说明",
            16,
            TextAnchor.UpperLeft,
            new Color(0.74f, 0.82f, 0.98f, 0.95f),
            FontStyle.Normal,
            false);
        var panelGuideLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(panelGuide.gameObject);
        panelGuideLayout.preferredHeight = 72f;

        var rowsViewport = ReplicaV3UIFactory.CreateImage("RowsViewport", panelBody, new Color(0f, 0f, 0f, 0.01f), false);
        var rowsViewportRect = rowsViewport.rectTransform;
        var rowsViewportLayout = ReplicaV3UIFactory.EnsureComponent<LayoutElement>(rowsViewport.gameObject);
        rowsViewportLayout.minHeight = 220f;
        rowsViewportLayout.flexibleHeight = 1f;
        var rowsMask = ReplicaV3UIFactory.EnsureComponent<Mask>(rowsViewport.gameObject);
        rowsMask.showMaskGraphic = false;

        var rowsRoot = ReplicaV3UIFactory.CreateRect("Rows", rowsViewportRect);
        rowsRoot.anchorMin = new Vector2(0f, 1f);
        rowsRoot.anchorMax = new Vector2(1f, 1f);
        rowsRoot.pivot = new Vector2(0.5f, 1f);
        rowsRoot.anchoredPosition = Vector2.zero;
        rowsRoot.sizeDelta = Vector2.zero;

        var rowsLayout = ReplicaV3UIFactory.EnsureComponent<VerticalLayoutGroup>(rowsRoot.gameObject);
        rowsLayout.spacing = 6f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandHeight = false;
        rowsLayout.childForceExpandWidth = true;
        var rowsFitter = ReplicaV3UIFactory.EnsureComponent<ContentSizeFitter>(rowsRoot.gameObject);
        rowsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var rowsScroll = ReplicaV3UIFactory.EnsureComponent<ScrollRect>(rowsViewport.gameObject);
        rowsScroll.horizontal = false;
        rowsScroll.vertical = true;
        rowsScroll.viewport = rowsViewportRect;
        rowsScroll.content = rowsRoot;
        rowsScroll.movementType = ScrollRect.MovementType.Clamped;
        rowsScroll.scrollSensitivity = 20f;

        var panelController = parameterPanel.gameObject.AddComponent<ReplicaV3ParameterPanelController>();
        panelController.PanelBodyRoot = panelBody;
        panelController.FoldButton = foldButton;
        panelController.FoldButtonLabel = foldLabel;
        panelController.EffectTitleLabel = panelEffectTitle;
        panelController.EffectGuideLabel = panelGuide;
        panelController.RowContainer = rowsRoot;

        var showcaseController = rootRect.gameObject.AddComponent<ReplicaV3ShowcaseController>();
        showcaseController.SceneCanvasMode = mode;
        showcaseController.OverlaySceneName = "ReplicaV3_Showcase_Overlay";
        showcaseController.WorldSceneName = "ReplicaV3_Showcase_World";
        showcaseController.OverlayScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_Overlay.unity";
        showcaseController.WorldScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_World.unity";
        showcaseController.HeaderTitleText = headerTitle;
        showcaseController.CurrentEffectTitleText = currentTitle;
        showcaseController.CurrentEffectDescriptionText = currentDesc;
        showcaseController.EffectButtonContainer = listContainer;
        showcaseController.EffectButtonTemplate = effectButtonTemplate;
        showcaseController.EffectMountRoot = effectMount;
        showcaseController.SwitchSceneButton = switchButton;
        showcaseController.SwitchSceneButtonText = switchButtonText;
        showcaseController.PlayInButton = playInButton;
        showcaseController.PlayOutButton = playOutButton;
        showcaseController.ResetButton = resetButton;
        showcaseController.DemoModeToggleButton = demoToggleButton;
        showcaseController.DemoModeToggleButtonText = demoToggleText;
        showcaseController.ParameterPanelController = panelController;
        showcaseController.Effects = LoadEffectEntries(shinyPrefabPath, magnetPrefabPath, countUpPrefabPath);
        showcaseController.DefaultEffectIndex = 0;

        var scenePath = $"{SceneFolder}/{sceneName}.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        return scenePath;
    }

    private static Camera BuildCamera(ReplicaV3CanvasMode mode)
    {
        var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;

        if (mode == ReplicaV3CanvasMode.Overlay)
        {
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
        }
        else
        {
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            camera.transform.position = new Vector3(0f, 0f, -18f);
            camera.transform.rotation = Quaternion.identity;
        }

        return camera;
    }

    private static void BuildEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas BuildCanvas(ReplicaV3CanvasMode mode, Camera camera)
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        var scaler = canvasGo.GetComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);

        if (mode == ReplicaV3CanvasMode.Overlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 18f;
            rect.position = Vector3.zero;
            rect.rotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.01f;
        }

        return canvas;
    }

    private static RectTransform BuildRootLayout(Transform canvasRoot, out RectTransform leftPanel, out RectTransform rightPanel)
    {
        var rootImage = ReplicaV3UIFactory.CreateImage("ReplicaV3Root", canvasRoot, new Color(0.07f, 0.08f, 0.11f, 1f), false);
        var rootRect = rootImage.rectTransform;
        ReplicaV3UIFactory.Stretch(rootRect);

        var leftImage = ReplicaV3UIFactory.CreateImage("LeftPanel", rootRect, new Color(0.11f, 0.13f, 0.19f, 0.97f), false);
        leftPanel = leftImage.rectTransform;
        SetRect(leftPanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), Vector2.zero, new Vector2(360f, 0f));

        var rightImage = ReplicaV3UIFactory.CreateImage("RightPanel", rootRect, new Color(0.08f, 0.10f, 0.15f, 0.98f), false);
        rightPanel = rightImage.rectTransform;
        SetRect(rightPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(360f, 0f), Vector2.zero, true);

        return rootRect;
    }

    private static List<ReplicaV3ShowcaseEffectEntry> LoadEffectEntries(string shinyPath, string magnetPath, string countUpPath)
    {
        var entries = new List<ReplicaV3ShowcaseEffectEntry>();

        var shinyPrefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(shinyPath);
        if (shinyPrefab != null)
        {
            entries.Add(new ReplicaV3ShowcaseEffectEntry
            {
                EffectId = "shiny-text-v3",
                DisplayName = "ShinyText V3",
                Description = "文字扫光效果，适合标题或强调文案。",
                EffectPrefab = shinyPrefab
            });
        }

        var magnetPrefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(magnetPath);
        if (magnetPrefab != null)
        {
            entries.Add(new ReplicaV3ShowcaseEffectEntry
            {
                EffectId = "magnet-v3",
                DisplayName = "Magnet V3",
                Description = "鼠标接近时的磁吸 + 倾斜反馈效果。",
                EffectPrefab = magnetPrefab
            });
        }

        var countUpPrefab = AssetDatabase.LoadAssetAtPath<ReplicaV3EffectBase>(countUpPath);
        if (countUpPrefab != null)
        {
            entries.Add(new ReplicaV3ShowcaseEffectEntry
            {
                EffectId = "count-up-v3",
                DisplayName = "CountUp V3",
                Description = "统计数字滚动效果，支持自动循环与倒计数。",
                EffectPrefab = countUpPrefab
            });
        }

        return entries;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        if (saved == null)
        {
            Debug.LogError($"[ReplicaV3] 预制体保存失败：{path}");
        }
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        bool useOffsets = false)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        if (useOffsets)
        {
            rect.offsetMin = anchoredPosition;
            rect.offsetMax = sizeDelta;
        }
        else
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
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
