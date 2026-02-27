# XTweenTimeline Dev Plan v2

## 1. Scope (v1 locked)
- Runtime: `XTweenTimeline`, `XTweenTimelineSequence`, `XTweenTimelinePlayer`, `XTweenTimelineCompat`.
- Actions: `XTweenTimelineCallback`, `XTweenTimelineFrame`, `XTweenTimelineLink`.
- Editor: Timeline inspector (`XTweenTimelineEditor/View/GUI/Controller/PreviewContext`) + action editors.
- Test baseline scene: `Assets/JustForDebug.unity` with `UI/Canvas/XTweenTimeline_RuntimeSmoke`.
- Out of scope: full DOTween edge-case parity, unsupported `XTween_Controller` tween families.

## 2. Architecture decisions
- Sequence orchestration is independent from `XTween_Manager`; child tweens keep native XTween lifecycle.
- Compatibility entry point is centralized in `XTweenTimelineCompat`.
- Editor preview uses `XTweenTimelineEditorPreviewContext` + explicit seek (`SeekTweenInEditor`), not global XTween preview queue.
- Seek default behavior is callback-suppressed; callback execution in editor is opt-in per track (`AllowEditorCallbacks`).
- Controller adaptation is isolated in `XTweenTimelineControllerAdapter`.

## 3. Supported controller types (v1)
- `位置_Position`
- `缩放_Scale`
- `旋转_Rotation`
- `透明度_Alpha`
- `颜色_Color`

Unsupported controller tracks stay visible in timeline but are marked invalid and are skipped at runtime playback.

## 4. Milestones
- M1 Runtime stable: done.
  - Sequence/Timeline/Player/Adapter delivered.
  - Delay semantics guarded through compat APIs.
- M2 Editor timeline stable: done.
  - Inspector timeline with drag/seek/snap/loop/play/stop.
  - Driven properties and preview context integrated.
- M3 Action components stable: done.
  - Callback/Frame/Link runtime + editor integration.
  - Link preview supports nested timeline seek in editor.
- M4 Patch integration ready: done (process documented, compat isolation in place).

## 5. Delivered files (core)
- Runtime folder: `Assets/XtweenTimeline/Runtime`
- Editor folder: `Assets/XtweenTimeline/Editor`
- Tests folder: `Assets/XtweenTimeline/Tests`
- Scene baseline object: `UI/Canvas/XTweenTimeline_RuntimeSmoke`

## 6. Open questions
- Should `XTweenTimelineRuntimeSmoke` stay auto-rebuild (`rebuildOnValidate=true`) in long-term branches, or be converted to a one-shot setup tool only?
- For Link preview callbacks, current behavior uses child `AllowEditorCallbacks`; if strict global suppression is desired, a timeline-level toggle should be added.
- Existing `XTweenDelayPlayOrderReproRunner` in scene is noisy in Play mode; keep for diagnostics or disable by default?
