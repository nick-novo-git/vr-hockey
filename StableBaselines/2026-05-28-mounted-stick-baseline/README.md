# 2026-05-28 Mounted Stick Baseline

This is the current working baseline after the mounted Quest controller orientation fixes.

Preserve this behavior:

- Controller mounted upward/outward on the hockey stick shaft.
- Virtual blade projects from the controller toward the real blade end.
- Blade sits at calibrated ice level after the post-menu rest calibration.
- Blade is a simple rectangular visual/collider.
- Mounted blade is kept flat on the ice while preserving roll-based blade face rotation.
- Current puck handling, menu, camera height, and rink scale are the baseline unless intentionally changed.

Restore files:

- `StickTracker.cs`
- `PrototypeBootstrap.cs`
- `PrototypeMenu.cs`
- `PuckController.cs`
