# XTweenTimeline Test Checklist（v1）

## 1. 测试目标
- 验证 `Assets/XtweenTimeline` 在 Runtime 与 Editor 下的稳定行为。
- 验证 `Assets/JustForDebug.unity` 可作为持续回归场景。
- 验证“Seek 默认不触发回调”与“兼容层隔离”没有回归。

## 2. 前置条件
- 打开场景：`Assets/JustForDebug.unity`
- 在 Hierarchy 确认对象：`UI/Canvas/XTweenTimeline_RuntimeSmoke`
- 若结构缺失，执行菜单：`XTweenTimeline/Rebuild Smoke In Scene`
- 保留 `XTweenDelayPlayOrderReproRunner` 仅用于 Delay 对照，不作为 Timeline 用例通过条件

## 3. 自动化测试（Unity Test Framework）

### EditMode
- 文件：`Assets/XtweenTimeline/Tests/EditMode/XTweenTimelineEditModeTests.cs`
- 用例：
  - `ControllerAdapter_UnsupportedType_IsReported`
  - `AnimationAdapter_ForSameController_IsComparable`
  - `CompatSeek_SuppressedCallback_DoesNotInvokeComplete`
- 通过标准：
  - 不支持类型被正确标记并带提示文案
  - 同一 Controller 的适配器比较结果稳定
  - Seek 抑制回调时不会触发 complete

### PlayMode
- 文件：
  - `Assets/XtweenTimeline/Tests/PlayMode/XTweenTimelineSequencePlayModeTests.cs`
  - `Assets/XtweenTimeline/Tests/PlayMode/XTweenTimelineLinkPlayModeTests.cs`
- 用例：
  - `Sequence_CallbackLoop_FiresExpectedCount`
  - `Sequence_PauseThenResume_PreservesTime`
  - `Frame_Rewind_RestoresOriginalState`
  - `Link_Duration_AggregatesLinkedChildren`
- 通过标准：
  - 回调循环次数准确
  - Pause/Resume 不导致提前触发
  - Frame 回滚可恢复原状态
  - Link 能聚合子时间轴时长

## 4. 手工回归（JustForDebug）

### 4.1 Runtime 基线
1. 进入 Play Mode。
2. 观察 `XTweenTimeline_RuntimeSmoke` 的 `SmokeTarget`：
   - Position/Scale/Rotation/Alpha/Color 依次生效。
3. 观察 Console：
   - 出现 `[XTweenTimelineRuntimeSmoke] Callback fired.`（播放时允许）。
4. 重复启停 3 次：
   - 无新异常、无 Timeline 残留报错。

### 4.2 Timeline Inspector 预览
1. 退出 Play Mode，选中 `XTweenTimeline_RuntimeSmoke`。
2. 在 Timeline Inspector 中拖动时间头（Seek）来回扫动。
3. 验证：
   - 轨道可预览且不会抖动失步。
   - Callback 轨道不会在拖拽中触发 UnityEvent。
4. 点击播放/停止、切换 Loop/Snap：
   - 时间轴状态与 Scene 显示一致。

### 4.3 Link / Frame 验证
1. 在同对象上确认存在 `XTweenTimelineLink` 与 `XTweenTimelineFrame`。
2. 预览或运行时观察：
   - Link 延迟后触发子 Timeline。
   - Frame 生效后在 Rewind 时可回滚。

## 5. 失败判定（任一命中即失败）
- 拖拽 Seek 触发了 Callback UnityEvent。
- 不支持 Controller 类型被静默执行（没有 warning 且实际播放）。
- Pause 后仍持续推进时间。
- 销毁 Timeline 物体后仍有活动 tween 残留。

## 6. 回归节奏建议
- 每次改动 `XTweenTimelineCompat` 后：全跑 EditMode + PlayMode。
- 每次改动 Editor 预览后：执行 4.2 全步骤。
- 每次改动 Link/Frame/Callback 后：执行 4.1 + 4.3。
