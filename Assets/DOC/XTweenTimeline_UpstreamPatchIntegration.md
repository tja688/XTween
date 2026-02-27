# XTweenTimeline Upstream Patch Integration（v1）

## 1. 目标
- 上游 XTween 补丁到来后，以最小改动接入 Timeline。
- 约束：优先只改 `XTweenTimelineCompat` 与相关测试，不重写 Timeline 主体逻辑。

## 2. 当前兼容层边界
- 文件：`Assets/XtweenTimeline/Runtime/XTweenTimelineCompat.cs`
- 对外职责：
  - `PlayTweenWithDelay(...)`
  - `ForceDelayState(...)`
  - `SeekTweenInEditor(...)`
- 设计意图：
  - 吸收 `Play -> SetDelay` 语义差异
  - 吸收编辑器 Seek 与回调抑制差异
  - 隔离反射字段访问，避免扩散到业务层

## 3. 接入流程（固定）
1. 创建补丁接入分支（例如 `xtl/compat-upstream-<date>`）。
2. 先跑基线测试：
   - `XTweenTimeline.Tests.EditMode`
   - `XTweenTimeline.Tests.PlayMode`
3. 仅在 compat 层替换实现：
   - 优先用上游正式 API 替换反射写字段。
   - 保持 `XTweenTimelineCompat` 公共方法签名不变。
4. 运行回归：
   - 自动化测试全通过。
   - `Assets/JustForDebug.unity` 按 `XTweenTimeline_TestChecklist.md` 手工回归。
5. 若行为语义变化不可避免：
   - 先更新 `XTweenTimeline_SequenceBehaviorMatrix.md`
   - 再更新对应测试断言
   - 最后更新本文件“差异记录”

## 4. 明确禁止项
- 不允许直接改 `Assets/Plugins/DOTweenTimeline` 作为修复手段。
- 不允许把 compat 逻辑散落到 `XTweenTimelineSequence` / Editor 控制器中。
- 不允许为单次补丁扩大 v1 支持范围（例如临时放开 Fill/Text 类型）。

## 5. 差异记录模板（每次补丁接入都要填）
- 上游版本/提交：
- 影响点：
  - Delay/StartTime 语义：
  - Editor Seek 语义：
  - Callback 触发边界：
- 代码改动文件：
- 新增/更新测试：
- 与旧版本行为差异（若有）：

## 6. 回滚策略
- 回滚粒度：按“compat 接入提交”整包回滚。
- 触发条件：
  - 自动化测试出现非预期失败且 1 个工作日内无法修复
  - JustForDebug 出现明显可见回归（Seek 抖动、回调误触发、循环中断）
- 回滚后动作：
  - 恢复上一版 compat 实现
  - 标记补丁为 pending，记录阻塞点

## 7. 放行标准
- EditMode + PlayMode 测试全部通过。
- `JustForDebug` 手工回归全部通过。
- 无新增控制台异常。
- 文档（行为矩阵 + 本文件差异记录）已同步更新。
