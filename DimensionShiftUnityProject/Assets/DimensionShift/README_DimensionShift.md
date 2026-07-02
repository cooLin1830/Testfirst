# PETS-like 2D / 2.5D Implementation Notes

This folder contains the complete prototype implementation.

The prototype is code-generated so you can study the mechanics without manually setting up a scene. Press Play in an empty scene, or use:

```text
Tools > Dimension Shift > Create Prototype Scene
Tools > Dimension Shift > Create Tutorial Scene
Tools > Dimension Shift > Create Painter Test Scene
Tools > Dimension Shift > Create Map For Current Scene
Tools > Dimension Shift > Bind Selected Map To Current Scene
Tools > Dimension Shift > Ensure Scene Map Preview
Tools > Dimension Shift > PETS Level Painter
```

The prototype uses generated grid definitions with optional mode-aware cell maps:

```text
2D mode: side-view line-platform movement.
2.5D mode: lightly perspective angled platform movement.
```

The tutorial scene treats the hand-drawn sketch as one shared closed surface. In 2D it appears upright as the original side-view drawing; in 2.5D the same surface is laid flat into the angled view, so switching perspective reads as converting the existing map instead of loading a different route.

Editable maps are stored as `PetsEditableLevelAsset` assets. The painter window edits the 2D map data directly, and the playable test scene builds a `PetsLevelDefinition` from that asset so the same drawing can be tested in 2D and 2.5D.

The most important technical choice is still this:

```text
Do not switch between Unity 2D physics and Unity 3D physics at runtime.
```

Instead, use 3D physics all the time and change constraints:

- 2D mode: lock Z, side-view camera, X/Y movement, black regions are enterable.
- 2.5D mode: unlock Z, lightly perspective angled camera, X/Z movement, black regions become holes.

This gives stable collision callbacks and makes dimension-aware props much easier to reason about.

## Current Visual Pass

- Paper-white platform faces.
- Black ink line platforms and region outlines.
- Procedural wobbly LineRenderer strokes for hand-drawn edges.
- Outline-only 2.5D platforms generated from the merged outer contour of the same surface; mild camera perspective and a subtle ground-offset contour cue make the original drawing read as laid flat, while internal cell fill, side-tone, shadow blocks, and per-cell edge strokes are hidden.
- Separate player visuals for flat 2D and solid 2.5D forms.
- Hidden physics bodies: colliders remain active, but their generated cube renderers are disabled.
- Connected black regions and 2.5D holes use merged outer ink outlines, so adjacent black cells do not show internal grid seams.
- 2.5D jump arcs can cross a one-cell black hole or one-cell empty gap when the landing cell is valid.
- Bricks and boxes are generated one cell at a time as overlay props on top of the painted terrain. Adjacent brick/box cells remain separate props and are not merged into a larger shape.
- Bricks are standable solid props. The first landing arms them; the next landing on their top breaks them. They can also be broken from below by a 2D upward head hit.
- `HeadBox` paints a special solid box that breaks only from a 2D upward head hit.
- Boxes are static in 2D. In 2.5D they become tall pushable blocks that the player can shove one grid cell at a time.

## Editable Map Tool

- `Tools > Dimension Shift > PETS Level Painter` opens a grid-painting editor window.
- `Assets/DimensionShift/EditableLevels/PainterTestLevel.asset` is a starter editable map.
- `Assets/DimensionShift/Scenes/DimensionShiftPainterTest.unity` is a separate test scene that loads the starter map asset.
- For another scene, open that scene first, then click `New For Scene` in the painter window or use `Tools > Dimension Shift > Create Map For Current Scene`.
- To reuse an existing map in a scene, select the `PetsEditableLevelAsset` in the Project window and run `Tools > Dimension Shift > Bind Selected Map To Current Scene`, or assign it in the painter window and click `Bind To Scene`.
- Scene binding is stored on a `DimensionPrototypeBootstrap` component in that scene. Its `levelKind` is set to `EditableAsset`, and its `editableLevel` reference points to the map asset for that scene.
- Binding or creating a scene map also creates a `PETS Scene Map Preview` object. Select it to view the editable grid in the Scene view and paint directly there.
- If an older scene has a bound map but no preview object, use `Tools > Dimension Shift > Ensure Scene Map Preview` or click `Show In Scene` in the painter window.
- White cells become closed 2D walkable regions and flattened 2.5D platforms.
- Black cells are enterable/inverting regions in 2D and holes in 2.5D.
- `Brick`, `Box`, and `HeadBox` paint one-cell overlay props on top of the current terrain. Painting a prop onto an empty cell creates a white terrain cell underneath it.
- `Brick` can be stood on, breaks on the second landing from above, and can be broken from below by a 2D head hit.
- `Box` is pushable in 2.5D.
- `HeadBox` is a special 2D head-break box.
- Switch, exit, and spawn cells are painted from the same editor window.
