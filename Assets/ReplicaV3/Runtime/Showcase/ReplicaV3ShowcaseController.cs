using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ReplicaV3ShowcaseController : MonoBehaviour
{
    [Header("场景模式")]
    [Tooltip("当前场景代表的 Canvas 模式（Overlay / WorldSpace）。")]
    public ReplicaV3CanvasMode SceneCanvasMode = ReplicaV3CanvasMode.Overlay;

    [Tooltip("Overlay 场景名称，用于一键切换。")]
    public string OverlaySceneName = "ReplicaV3_Showcase_Overlay";

    [Tooltip("WorldSpace 场景名称，用于一键切换。")]
    public string WorldSceneName = "ReplicaV3_Showcase_World";

    [Tooltip("Overlay 场景路径。用于未加入 Build Profiles 时的编辑器回退加载。")]
    public string OverlayScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_Overlay.unity";

    [Tooltip("WorldSpace 场景路径。用于未加入 Build Profiles 时的编辑器回退加载。")]
    public string WorldScenePath = "Assets/ReplicaV3/Scenes/ReplicaV3_Showcase_World.unity";

    [Header("UI 引用")]
    [Tooltip("左上角主标题。")]
    public Text HeaderTitleText;

    [Tooltip("当前动效标题。")]
    public Text CurrentEffectTitleText;

    [Tooltip("当前动效说明。")]
    public Text CurrentEffectDescriptionText;

    [Tooltip("动效按钮容器。")]
    public RectTransform EffectButtonContainer;

    [Tooltip("动效按钮模板（运行时会复制该对象）。")]
    public Button EffectButtonTemplate;

    [Tooltip("动效实例挂载点。")]
    public RectTransform EffectMountRoot;

    [Tooltip("切换场景按钮。")]
    public Button SwitchSceneButton;

    [Tooltip("切换场景按钮文本。")]
    public Text SwitchSceneButtonText;

    [Tooltip("播放进入按钮。")]
    public Button PlayInButton;

    [Tooltip("播放退出按钮。")]
    public Button PlayOutButton;

    [Tooltip("重置按钮。")]
    public Button ResetButton;

    [Tooltip("演示/纯净切换按钮。")]
    public Button DemoModeToggleButton;

    [Tooltip("演示/纯净切换按钮文本。")]
    public Text DemoModeToggleButtonText;

    [Tooltip("参数面板控制器。")]
    public ReplicaV3ParameterPanelController ParameterPanelController;

    [Header("动效数据")]
    [Tooltip("动效列表（预制体原子化插拔入口）。")]
    public List<ReplicaV3ShowcaseEffectEntry> Effects = new List<ReplicaV3ShowcaseEffectEntry>();

    [Tooltip("默认选中的动效索引。")]
    public int DefaultEffectIndex = 0;

    private readonly List<Button> mRuntimeButtons = new List<Button>();
    private readonly List<Text> mRuntimeButtonLabels = new List<Text>();
    private ReplicaV3EffectBase mCurrentEffectInstance;
    private int mCurrentIndex = -1;
    private bool mUseDemoMode = true;

    private void Awake()
    {
        BindTopButtons();
        BuildEffectButtons();
        UpdateHeaderText();
        UpdateSceneSwitchText();
        UpdateDemoToggleText();
    }

    private void Start()
    {
        if (Effects.Count <= 0)
        {
            ParameterPanelController?.BindEffect(null);
            return;
        }

        SelectEffect(Mathf.Clamp(DefaultEffectIndex, 0, Effects.Count - 1));
    }

    private void OnDestroy()
    {
        UnbindTopButtons();
        ClearRuntimeButtons();
    }

    private void BindTopButtons()
    {
        if (SwitchSceneButton != null)
        {
            SwitchSceneButton.onClick.AddListener(SwitchScene);
        }

        if (PlayInButton != null)
        {
            PlayInButton.onClick.AddListener(() =>
            {
                if (mCurrentEffectInstance != null)
                {
                    mCurrentEffectInstance.PlayIn();
                }
            });
        }

        if (PlayOutButton != null)
        {
            PlayOutButton.onClick.AddListener(() =>
            {
                if (mCurrentEffectInstance != null)
                {
                    mCurrentEffectInstance.PlayOut();
                }
            });
        }

        if (ResetButton != null)
        {
            ResetButton.onClick.AddListener(() =>
            {
                if (mCurrentEffectInstance != null)
                {
                    mCurrentEffectInstance.ResetEffect();
                    mCurrentEffectInstance.PlayIn();
                }
            });
        }

        if (DemoModeToggleButton != null)
        {
            DemoModeToggleButton.onClick.AddListener(() =>
            {
                mUseDemoMode = !mUseDemoMode;
                if (mCurrentEffectInstance != null)
                {
                    mCurrentEffectInstance.SetDemoMode(mUseDemoMode);
                }

                UpdateDemoToggleText();
            });
        }
    }

    private void UnbindTopButtons()
    {
        if (SwitchSceneButton != null)
        {
            SwitchSceneButton.onClick.RemoveListener(SwitchScene);
        }

        if (PlayInButton != null)
        {
            PlayInButton.onClick.RemoveAllListeners();
        }

        if (PlayOutButton != null)
        {
            PlayOutButton.onClick.RemoveAllListeners();
        }

        if (ResetButton != null)
        {
            ResetButton.onClick.RemoveAllListeners();
        }

        if (DemoModeToggleButton != null)
        {
            DemoModeToggleButton.onClick.RemoveAllListeners();
        }
    }

    private void BuildEffectButtons()
    {
        ClearRuntimeButtons();

        if (EffectButtonTemplate == null || EffectButtonContainer == null)
        {
            return;
        }

        EffectButtonTemplate.gameObject.SetActive(false);

        for (var i = 0; i < Effects.Count; i++)
        {
            var entry = Effects[i];
            if (entry == null || entry.EffectPrefab == null)
            {
                continue;
            }

            var button = Instantiate(EffectButtonTemplate, EffectButtonContainer);
            button.gameObject.SetActive(true);
            button.name = $"EffectButton_{i}_{entry.EffectId}";
            var index = i;
            button.onClick.AddListener(() => SelectEffect(index));

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                var display = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.EffectPrefab.EffectDisplayName : entry.DisplayName;
                label.text = display;
            }

            mRuntimeButtons.Add(button);
            mRuntimeButtonLabels.Add(label);
        }
    }

    private void ClearRuntimeButtons()
    {
        for (var i = 0; i < mRuntimeButtons.Count; i++)
        {
            if (mRuntimeButtons[i] != null)
            {
                mRuntimeButtons[i].onClick.RemoveAllListeners();
                Destroy(mRuntimeButtons[i].gameObject);
            }
        }

        mRuntimeButtons.Clear();
        mRuntimeButtonLabels.Clear();
    }

    private void SelectEffect(int index)
    {
        if (index < 0 || index >= Effects.Count)
        {
            return;
        }

        var entry = Effects[index];
        if (entry == null || entry.EffectPrefab == null)
        {
            return;
        }

        mCurrentIndex = index;
        SpawnEffect(entry);
        UpdateSelectionVisual();
        UpdateCurrentEffectText(entry);
    }

    private void SpawnEffect(ReplicaV3ShowcaseEffectEntry entry)
    {
        if (mCurrentEffectInstance != null)
        {
            Destroy(mCurrentEffectInstance.gameObject);
            mCurrentEffectInstance = null;
        }

        if (EffectMountRoot == null || entry.EffectPrefab == null)
        {
            ParameterPanelController?.BindEffect(null);
            return;
        }

        mCurrentEffectInstance = Instantiate(entry.EffectPrefab, EffectMountRoot);
        var instanceRect = mCurrentEffectInstance.transform as RectTransform;
        if (instanceRect != null)
        {
            if (ShouldStretchToMount(entry.EffectPrefab))
            {
                instanceRect.anchorMin = Vector2.zero;
                instanceRect.anchorMax = Vector2.one;
                instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.anchoredPosition = Vector2.zero;
                instanceRect.sizeDelta = Vector2.zero;
                instanceRect.localScale = Vector3.one;
            }
            else
            {
                instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
                instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
                instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.anchoredPosition = Vector2.zero;
                instanceRect.localScale = Vector3.one;
                FitEffectToMount(instanceRect);
            }
        }

        mCurrentEffectInstance.SetCanvasMode(SceneCanvasMode);
        mCurrentEffectInstance.SetDemoMode(mUseDemoMode);
        mCurrentEffectInstance.ResetEffect();
        mCurrentEffectInstance.PlayIn();

        ParameterPanelController?.BindEffect(mCurrentEffectInstance);
    }

    private static bool ShouldStretchToMount(ReplicaV3EffectBase prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        var root = prefab.EffectRoot != null ? prefab.EffectRoot : prefab.transform as RectTransform;
        if (root == null)
        {
            return false;
        }

        var anchorsStretch = root.anchorMin.sqrMagnitude <= 0.0001f &&
                             (root.anchorMax - Vector2.one).sqrMagnitude <= 0.0001f;
        if (anchorsStretch)
        {
            return true;
        }

        return root.rect.width <= 1f || root.rect.height <= 1f;
    }

    private void FitEffectToMount(RectTransform instanceRect)
    {
        if (instanceRect == null || EffectMountRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        var mountSize = EffectMountRoot.rect.size;
        var effectSize = instanceRect.rect.size;
        if (mountSize.x <= 0.01f || mountSize.y <= 0.01f || effectSize.x <= 0.01f || effectSize.y <= 0.01f)
        {
            return;
        }

        var scaleX = mountSize.x / effectSize.x;
        var scaleY = mountSize.y / effectSize.y;
        var fitScale = Mathf.Min(scaleX, scaleY, 1f);
        if (fitScale < 0.999f)
        {
            instanceRect.localScale = Vector3.one * fitScale;
        }
    }

    private void UpdateSelectionVisual()
    {
        for (var i = 0; i < mRuntimeButtons.Count; i++)
        {
            var button = mRuntimeButtons[i];
            if (button == null)
            {
                continue;
            }

            var selected = i == mCurrentIndex;
            var colorBlock = button.colors;
            colorBlock.normalColor = selected
                ? new Color(0.34f, 0.53f, 0.87f, 1f)
                : new Color(0.20f, 0.24f, 0.33f, 0.95f);
            colorBlock.highlightedColor = selected
                ? new Color(0.42f, 0.62f, 0.95f, 1f)
                : new Color(0.27f, 0.33f, 0.43f, 1f);
            button.colors = colorBlock;

            if (i >= 0 && i < mRuntimeButtonLabels.Count && mRuntimeButtonLabels[i] != null)
            {
                mRuntimeButtonLabels[i].color = selected ? Color.white : new Color(0.87f, 0.91f, 0.99f, 0.95f);
            }
        }
    }

    private void UpdateCurrentEffectText(ReplicaV3ShowcaseEffectEntry entry)
    {
        if (CurrentEffectTitleText != null)
        {
            CurrentEffectTitleText.text = string.IsNullOrWhiteSpace(entry.DisplayName)
                ? (entry.EffectPrefab != null ? entry.EffectPrefab.EffectDisplayName : "未命名动效")
                : entry.DisplayName;
        }

        if (CurrentEffectDescriptionText != null)
        {
            CurrentEffectDescriptionText.text = string.IsNullOrWhiteSpace(entry.Description)
                ? "暂无说明。"
                : entry.Description;
        }
    }

    private void UpdateHeaderText()
    {
        if (HeaderTitleText == null)
        {
            return;
        }

        HeaderTitleText.text = SceneCanvasMode == ReplicaV3CanvasMode.Overlay
            ? "Replica V3 Showcase · Overlay Canvas"
            : "Replica V3 Showcase · World Space Canvas";
    }

    private void UpdateSceneSwitchText()
    {
        if (SwitchSceneButtonText == null)
        {
            return;
        }

        SwitchSceneButtonText.text = SceneCanvasMode == ReplicaV3CanvasMode.Overlay
            ? "切到 World Space"
            : "切到 Overlay";
    }

    private void UpdateDemoToggleText()
    {
        if (DemoModeToggleButtonText == null)
        {
            return;
        }

        DemoModeToggleButtonText.text = mUseDemoMode ? "切到纯净模式" : "恢复演示模式";
    }

    private void SwitchScene()
    {
        var targetSceneName = SceneCanvasMode == ReplicaV3CanvasMode.Overlay
            ? WorldSceneName
            : OverlaySceneName;
        var targetScenePath = SceneCanvasMode == ReplicaV3CanvasMode.Overlay
            ? WorldScenePath
            : OverlayScenePath;

        if (string.IsNullOrWhiteSpace(targetSceneName) && string.IsNullOrWhiteSpace(targetScenePath))
        {
            return;
        }

        if (TryLoadByBuildSettings(targetSceneName, targetScenePath))
        {
            return;
        }

        if (TryLoadByEditorScenePath(targetSceneName, targetScenePath))
        {
            return;
        }

        Debug.LogError($"[ReplicaV3] 场景切换失败。name={targetSceneName}, path={targetScenePath}", this);
    }

    private bool TryLoadByBuildSettings(string sceneName, string scenePath)
    {
        if (!IsSceneInBuildSettings(sceneName, scenePath))
        {
            return false;
        }

        var key = !string.IsNullOrWhiteSpace(sceneName) ? sceneName : scenePath;
        SceneManager.LoadScene(key);
        return true;
    }

    private bool TryLoadByEditorScenePath(string sceneName, string scenePath)
    {
#if UNITY_EDITOR
        var resolvedPath = ResolveScenePath(sceneName, scenePath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return false;
        }

        if (Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                resolvedPath,
                new LoadSceneParameters(LoadSceneMode.Single));
        }
        else
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                resolvedPath,
                UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        return true;
#else
        return false;
#endif
    }

    private static bool IsSceneInBuildSettings(string sceneName, string scenePath)
    {
        var count = SceneManager.sceneCountInBuildSettings;
        for (var i = 0; i < count; i++)
        {
            var buildPath = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrWhiteSpace(scenePath) &&
                string.Equals(buildPath, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                var buildName = Path.GetFileNameWithoutExtension(buildPath);
                if (string.Equals(buildName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string ResolveScenePath(string sceneName, string scenePath)
    {
        if (!string.IsNullOrWhiteSpace(scenePath))
        {
            return scenePath;
        }

#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        var sceneGuids = UnityEditor.AssetDatabase.FindAssets($"{sceneName} t:Scene");
        for (var i = 0; i < sceneGuids.Length; i++)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            if (!string.IsNullOrWhiteSpace(path) &&
                string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }
#endif

        return null;
    }
}
