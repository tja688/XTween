# XTweenTimeline Controller Mapping（v1）

## 1. 映射范围与原则
- 本文针对 `Assets/XtweenTimeline` 当前实现，不讨论“计划中但未实现”的能力。
- Timeline 适配入口为 `XTweenTimelineControllerAdapter`，仅允许 5 类 `XTween_Controller.TweenTypes` 进入时间轴。
- 不支持项策略固定为：可见但不可激活（`IsValid = false`），Inspector 给 Warning，运行时跳过，不做隐式近似。

## 2. 轨道类型映射（DOTweenTimeline -> XTweenTimeline）

| DOTweenTimeline 常见轨道 | XTween_Controller 映射 | v1 状态 | 说明 |
|---|---|---|---|
| Position | `XTweenTypes.位置_Position` | Supported | 可用；由 `XTweenTypes_Positions` 决定 AnchoredPosition/3D 等细分。 |
| Scale | `XTweenTypes.缩放_Scale` | Supported | 可用。 |
| Rotation | `XTweenTypes.旋转_Rotation` | Supported | 可用；由 `XTweenTypes_Rotations` 决定欧拉角或其他细分。 |
| Fade/Alpha | `XTweenTypes.透明度_Alpha` | Supported | 可用；由 `XTweenTypes_Alphas` 决定 Image/CanvasGroup。 |
| Color | `XTweenTypes.颜色_Color` | Supported | 可用。 |
| Fill | `XTweenTypes.填充_Fill` | Unsupported | v1 不接入时间轴播放。 |
| Size / Shake / Path / Text / TMPText / To 等 | 对应各 `XTweenTypes.*` | Unsupported | v1 不接入时间轴播放。 |

## 3. 参数映射（DOTweenAnimation 常用项 -> XTween_Controller）

| DOTween 语义 | XTween 字段 | v1 状态 | 说明 |
|---|---|---|---|
| `delay` | `Delay` | Exact | Timeline 拖拽修改的是适配器 `Delay`。 |
| `duration` | `Duration` | Exact | 时间轴长度按 `Duration * Loops` 计算。 |
| `loops` | `LoopCount` | Near-Exact | `-1` 视为无限；其余值按 `Mathf.Max(1, loops)` 用于可视化时长。 |
| `loopType` | `LoopType` | Exact（底层） | Timeline 本身不改写该值，沿用 Controller 配置。 |
| `loopDelay` | `LoopDelay` | Exact（底层） | 参与 tween 自身总时长。 |
| `ease` | `EaseMode` | Exact | 由 Controller `Tween_Create()` 生效。 |
| `easeCurve` | `UseCurve + Curve` | Exact | 同上。 |
| `from` | `IsFromMode` | Exact | 编辑器预览可识别 `IsFrom`。 |
| `relative` | `IsRelative` | Exact | 由 Controller 执行。 |
| `autoKill` | `IsAutoKill` | Exact（底层） | Timeline 预览会额外 `SetAutoKill(false)` 保持可 seek。 |
| `target` | `Target_RectTransform/Image/CanvasGroup/...` | Exact | Adapter `Targets` 会聚合目标用于高亮。 |
| `id` | 无独立字段（显示名用 `name`） | Unsupported | v1 不提供 DOTween 风格 `id` 字段映射。 |
| `SetLink(gameObject)` | `XTweenTimelineLink` 组件 | Replaced | 通过 Link 组件处理跨 Timeline/生命周期聚合。 |
| `InsertCallback` | `XTweenTimelineCallback` 组件 | Replaced | 回调用独立组件，不依赖 Controller。 |

## 4. 编辑器 Seek/回调约束
- Controller 轨道 `AllowEditorCallbacks = false`，拖拽时间头默认不触发回调。
- `Frame` 轨道显式允许编辑器回调（用于状态快照/回滚语义）。
- Seek 的核心入口在 `XTweenTimelineCompat.SeekTweenInEditor(...)`，由兼容层统一处理回调抑制。

## 5. 代码锚点
- `Assets/XtweenTimeline/Runtime/XTweenTimelineControllerAdapter.cs`
- `Assets/XtweenTimeline/Runtime/XTweenTimelineAnimationAdapter.cs`
- `Assets/XtweenTimeline/Runtime/XTweenTimelineCompat.cs`
- `Assets/XtweenTimeline/Editor/XTweenTimelineEditor.cs`
