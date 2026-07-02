# PETS-like 2D / 2.5D Unity Prototype

This is a standalone Unity prototype for a PETS-like perspective-switching platformer mechanic.

Open this folder as a Unity project:

```text
D:\work\SwordOfWanYao\Assets\Testfirst\DimensionShiftUnityProject
```

## How To Run

1. Open `DimensionShiftUnityProject` with Unity 2022.3.62f2c1.
2. Open `Assets/DimensionShift/Scenes/DimensionShiftPrototype.unity`.
3. Press Play. The bootstrap object in the scene creates the playable PETS-like test room.
4. Use:
   - `A/D` to move in 2D.
   - `W/A/S/D` to move in 2.5D.
   - `Space` to jump.
   - `E` on a labelled switch tile to switch perspective.
   - `R` to respawn at the last safe position.

The hand-drawn tutorial map is available as:

```text
Assets/DimensionShift/Scenes/DimensionShiftTutorial.unity
```

The editable-map test scene is available as:

```text
Assets/DimensionShift/Scenes/DimensionShiftPainterTest.unity
```

## Current Mechanic Target

- Switch only on tiles labelled `2D` or `2.5D`.
- The switch affects the whole world, not only the player.
- 2D mode is a side-view line-platform view with downward gravity.
- 2D black regions are enterable, invert the player color, and block downward exits.
- 2.5D mode is a lightly perspective angled platform view, not a pure top-down map view.
- 2.5D black regions become holes.
- In 2.5D, holding a direction and pressing `Space` performs a short arc jump.
- A one-cell black hole or empty gap can be jumped across in 2.5D; falling into holes or off platforms respawns the player.
- Bricks and boxes can be painted as overlay props on top of 2D/2.5D terrain.
- Bricks can be stood on; jumping again while standing on one breaks it.
- `HeadBox` is a special box that can be broken from below by a 2D upward head hit.
- Reaching the exit tile ends the prototype.

## Important Scripts

```text
Assets/DimensionShift/Scripts/PetsLike/PetsModeManager.cs
```

Global perspective owner. It only switches when the player is on a valid labelled switch tile.

```text
Assets/DimensionShift/Scripts/PetsLike/PetsLikePlayerController.cs
```

Player movement, jump rules, black-region state, switching input, and respawn logic.

```text
Assets/DimensionShift/Scripts/PetsLike/PetsLevelRuntime.cs
```

Builds grid maps into 2D line-platform objects and angled 2.5D platform/hole objects. The tutorial scene uses the same closed sketch surface for both modes: 2D shows it upright as the drawn side-view map, and 2.5D lays that same surface flat so the player walks on it.

```text
Assets/DimensionShift/Scripts/PetsLike/PetsPrototypeFactory.cs
```

Creates the mechanism test room, materials, player, camera, HUD, and level definition.

## Notes

The prototype now includes a first-pass visual layer: paper-white background, black ink geometry, procedural wobbly line strokes, and separate 2D flat / 2.5D solid player visuals. Physics boxes are hidden while their colliders remain active, so the player reads as moving on ink lines rather than visible debug blocks. In 2.5D, platforms render as outline-only shapes with mild camera perspective and a subtle ground-offset contour cue instead of visible cell grids. Connected black regions/holes draw as one merged outer outline without internal grid lines. Shader polish, bespoke sprites, animation polish, and final art direction are still intentionally left for later.

## Map Painter

Open `Tools > Dimension Shift > PETS Level Painter` to paint a 2D grid map into a `PETS Editable Level` asset. Paint white walkable regions, black regions, `2.5D` / `2D` switch tiles, the exit, spawn, and overlay props such as Brick, Box, and HeadBox. The runtime uses the same painted 2D map to generate the 2D side-view and automatically flatten it into the 2.5D perspective view.
