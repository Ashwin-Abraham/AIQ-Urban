# Building constructor accepts existing floor plates

When loading a model from the Rhino doc, we need to create `Building` objects from envelope Breps and pre-existing floor plate curves already baked in the doc. The existing `Building(Brep, double, Use)` constructor recontours the envelope to derive floor plates — discarding any manually drawn or previously exported plate geometry.

We added a new constructor `Building(Brep envelope, IReadOnlyList<FloorPlate> existingPlates)` that preserves the existing floor plate curves as-is. `TypicalFloorHeight` is derived as the mode of non-zero Z-differences between consecutive plates. This avoids losing user edits to floor plate outlines and respects geometry that may have been imported from external sources.

**Considered alternatives:**
- Recontour from envelope (existing constructor): simpler but destructive — existing floor plate curves become orphans.
- Factory method on Building: same effect but doesn't fit the existing construction pattern.

**Consequences:**
- Two constructors for mass-mode buildings — callers must choose intentionally.
- `TypicalFloorHeight` from mode-of-differences is approximate; irregular spacing produces an imprecise value. Accepted trade-off since the value is used for display/metrics, not for floor plate geometry.
