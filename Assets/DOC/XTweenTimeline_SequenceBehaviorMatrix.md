# XTweenTimeline Sequence Behavior Matrix

## Runtime (`XTweenTimelineSequence`)

| Operation | State before | Expected behavior | Notes |
|---|---|---|---|
| `Play()` | fresh | starts loop 0, registers in runner | child tweens rewound and replayed with computed delay |
| `Play()` | paused | resumes from pause time | keeps elapsed progress |
| `Play()` | completed | restarts from beginning | equivalent to `Restart()` |
| `Pause()` | playing | pauses sequence and children | unregisters from runner |
| `Resume()` | paused | resumes sequence and children | runner registration restored |
| `TogglePause()` | paused/playing | toggles pause state | thin wrapper |
| `Rewind()` | any active | rewinds children, resets callback fired set | invokes `OnRewind` |
| `Restart()` | any active | `Rewind()` then `Play()` | full restart |
| `Kill()` | active | kills children and sequence | invokes `OnKill` |
| Loop end | finite loops | completes on final loop | invokes `OnComplete(duration)` once |
| Loop end | `loops = -1` | infinite restart | never auto-completes |
| Callback trigger | elapsed crosses callback time | callback fires once per loop | deduped by callback id set |

## Seek / Preview (`XTweenTimelineEditorPreviewContext`)

| Operation | Default callback behavior | Track override | Notes |
|---|---|---|---|
| Drag timehead seek | suppressed | `AllowEditorCallbacks=true` can allow | implemented via `XTweenTimelineCompat.SeekTweenInEditor(..., suppressCallbacks)` |
| Preview play tick | suppressed for normal tracks | allowed for frame-like tracks | controlled per animation adapter |
| Preview stop | rewinds + kills preview tweens | n/a | avoids stale preview state |

## Edge cases

| Case | Behavior |
|---|---|
| Sequence with callbacks only | no early completion before pending callbacks fire |
| Empty sequence (no tweens/callbacks) | completes immediately when first update reaches duration (0) |
| Unsupported controller track | shown but invalid, skipped for runtime tween creation |
