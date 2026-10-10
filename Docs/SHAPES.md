# Shapes: the built-in catalogue and the geometry utilities

Reasoning for `Services/Shapes/` (`ShapeCatalogService`, `ShapeParameters`). The id layout rules and
why `ShapeId` stays a `Guid` are in `IDENTIFIERS.md`; this file holds the form codes and the geometry.

## The id is the parameters

A built-in `ShapeId` is its parameters packed one axis per nibble; the layout is a contract, not a
packing detail. The six rules (`IDENTIFIERS.md`, "Built-in `ShapeId` is safe by construction"):

1. one axis per nibble (two axes sharing one means a new value in one can collide with the other);
2. zero in any field is today's behaviour, so a future axis in the reserved nibbles is zero in every id
   ever written;
3. enumerated axes get a nibble each, booleans share the flags nibble;
4. the form is the SIDE COUNT itself, offset by `ShapeForm.PolygonBase`;
5. reserved nibbles are documented with their intended occupants;
6. the NAME's field order mirrors the id's, so a name decodes and sorts the same way.

The library this replaced numbered shapes 1..78 by array position, so inserting a form renumbered
everything after it and rewrote every level that referenced them. Form code 0 is reserved so none of
those old ids can decode into a valid new shape.

**Form codes are a low band plus a ladder.** Square, Circle, RightTriangle and Triangle are named things
an author reaches for, not "the 4-gon" and "the 32-gon", so they live at 0x01..0x04 with 0x05..0x0F held
for whatever else earns a name; sorting by id then puts them first without a hand-kept ordering list. The
ladder is the side count offset by `PolygonBase`, so an eleven-, thirteen- or twenty-sided shape slots in
later without renumbering or a lookup table. 0x13 and 0x14 are permanently unused (three and four sides
already have names above). `TryDecode` is false for a level-authored shape, a retired 1..78 id and an id
carrying a field this build does not know - which a caller must report, not draw something plausible for.

## Sectors and their forms

- **The cuts are the BOX's own axes, not the polygon's**: half is cut by the vertical through the box
  centre, quarter adds the horizontal, eighth adds the 45 degree diagonal. The sector a slice covers is
  then exactly the sector of the box carrying the same name, which is what lets Invert subtract the shape
  from its own sector instead of from the whole box. This reproduces the shipped shapes rather than
  replacing them (`Pentagon_F4` starts at y = 0.0503 and the pentagon's AABB centre is 0.0502: the
  horizontal cut was made through the AABB centre; once geometry is AABB-centred that line IS the box
  centre).
- **A quarter has two forms and only sometimes needs both**: the lower-right quarter is the upper-right
  one turned by -90 degrees whenever 90 degrees is a whole number of the form's rotational steps
  (`N % 4 == 0`). For every other N they are genuinely different shapes and both exist - nothing relies on
  mirroring through a negative Size, which would also reverse the winding the renderer culls on.
- **Slicing is withheld from three forms** because a slice of them is another shape plus a size: half a
  square is a rectangle and a quarter a smaller square (what Size already does); half an equilateral
  triangle is a 30-60-90 triangle, i.e. `RightTriangle` under a non-uniform Size. Circle is NOT in that
  list: it is the form whose sectors are used most.
- **Omitted inverts are not omissions**; each is another catalogue entry seen differently, and shipping
  both would put two ids on one shape. Square inverted: the square fills its own box, so subtracting it
  leaves nothing, and subtracting a square RING leaves the inner square (Square under a smaller Size).
  RightTriangle inverted and filled: the box minus a filled right triangle is the other right triangle,
  the same shape rotated 180 degrees (its rings invert fine: they leave two pieces, not one).
- **Every form is built with a corner pointing straight up**: `ShapeLoopUtils` measures angles clockwise
  from straight up, so a rim laid out from angle zero has a corner there whatever the side count. Four
  sides is the exception and is turned half a step (a 4-gon with a corner on top is a diamond, and the
  shape wanted is a square).

## Geometry

- **The offset is the FULL shape's**, applied to every sector, ring and inversion of it, so switching an
  object from `Hexagon` to `Hexagon_S4` truncates the shape where it stands instead of moving and
  rescaling it, and a ring stays concentric with its disc. Centring each entry on its own bounds would do
  neither.
- **A whole ring is measured from the SHAPE's centroid, not the box's**: they differ for an odd-sided form
  and only the first is inside both rims (the right triangle's hypotenuse runs through the box centre). A
  SLICED ring has no such choice: its cut rays leave the box centre, so its angles are measured from there.
- **The shape comes out of ITS OWN SECTOR of the box**, not out of the whole box: the same cuts that cut
  the shape cut the box, so an inverted quarter is a quarter of a wall with a quarter of a hole in it, and
  the two cuts line up instead of leaving a sliver of wall.
- `GetCentroidPivot` returns the pivot (in the `RectObject.Pivots` 0..1 space) that puts an object's
  transform on the form's centre of mass; (0.5, 0.5), the default, is already the AABB centre, so any form
  whose two centres coincide answers exactly that.

## One definition of a valid shape (`ShapeGeometryUtils`)

The single implementation of "what a valid shape is" and "how to make an invalid one valid". Two callers
share it and must not drift: `RuleShapeGeometryAttribute.Fix` (validation) and the in-game shape editor's
Save (authoring). A second copy would let a shape pass the editor and fail validation, or the reverse.
It is Unity-independent: the game's `ShapeMeshUtils` owns the float2/Mesh side, this owns the model side.
The one duplicated line is `SignedDoubleArea` (sharing it would drag `Unity.Mathematics` in here).

- **`Sanitize` order is the order in which fixing one problem cannot reintroduce another**, not the order
  they are listed in: clamping first (it moves points and can collapse a triangle), welding before the
  degenerate check for the same reason, winding LAST once the triangle set cannot change. It repairs in
  place, never throws, never leaves data half-repaired; an empty result means nothing survived and the
  caller refuses to store it.
- **Duplicates are reported, not just repaired.** Sanitize welds them either way, but a rule calling a
  weldable shape clean would make Fix change data validation swore was fine. Two corners at one place look
  like one until the author drags it and only one moves - the defect indexed storage exists to prevent.
- **The centroid is weighted by ABSOLUTE area**, which makes it the general form of the game's
  `ShapeCatalogService.GetCentroidPivot` (that one averages a form's rim corners, a centre of mass only
  for triangles and regular polygons; a triangle soup has no rim). They agree on every built-in form -
  `ShapeGeometryUtilsTests` pins it. A back-facing triangle is a defect Sanitize repairs, not a hole to
  subtract, and a signed weight would cancel it and answer a point outside the shape. The result is a
  PIVOT in [0, 1] against the rect while the geometry lives in [-0.5, 0.5], hence the 0.5 offset.

## Radial loops (`ShapeLoopUtils`)

Every built-in shape is two closed loops and a sector: a filled shape is one loop, a ring has a second
inside it, an inverted shape is the BOX's loop with the shape's loop inside it, and slicing is the same
operation on whichever loops exist. The catalogue therefore needs two emitters (a fan and an annulus), not
one per combination; a new axis means producing different loops, not a fourth builder.

- **Angles run clockwise from straight up**: [0, pi] is the right half, [0, pi/2] upper-right, [pi/2, pi]
  lower-right, [0, pi/4] the 45-degree octant. Those are the box's own axes and diagonal through its
  centre, the rule the shipped library follows (Pentagon_F4 is cut at y = 0.0503, the pentagon's AABB
  centre is 0.0502), and why an inverted slice can subtract the shape from the matching sector of the box.
- **Angles are always relative.** A rim corner meant at angle zero is routinely built at x = -1e-17,
  whose absolute angle is a hair under a full turn; it sorts to the far end of its loop and silently
  reverses a sector. Measuring against a reference and snapping to zero (`AngleEpsilon`) prevents it.
- **The polygon radius fits the BOUNDING BOX to the unit box**, not every corner within half a unit. For an
  odd side count they differ: a circumradius-inscribed equilateral triangle reaches 0.5 up and 0.433
  sideways, while the shipped Triangle is a full unit wide. Fitting the bounds keeps the shipped size and,
  once recentred on its bounds, puts every point inside the box.
- **A ring shrinks towards the CORNER CENTROID**, not the origin: the right triangle's hypotenuse runs
  through the origin, so an origin-scaled copy keeps the hypotenuse and the ring has no width along it.
  For regular polygons the two centres coincide.
- **Both rims of an inverted shape are resampled onto the UNION of their angles**, not walked in
  lockstep. A regular polygon fitted to the box touches it at its widest corners, so the region between
  them is pinched to zero there and is not one ribbon. A merge advancing whichever rim comes next
  straddles the pinch and emits a triangle across the shape (invisible in signed area, obvious on screen
  as a flipped overlapping triangle). Sampling both rims at every turn makes each cell a trapezoid, and
  the pinch only makes one degenerate, which Sanitize drops.

## Synthesis (`ShapeSynthUtils`) and pivots (`alignment`)

- The built-in library is exactly the ids `ShapeId` names. Anything outside it (a half ring, an outline
  slice) is level-authored data instead of a bigger library: a level carrying its geometry works
  everywhere, while an extra preset would have to exist in every build that ever opens the file. Every
  synthesized shape ends with `Sanitize`, the same call `RuleShapeGeometry.Fix` makes, so a generator is
  plain trigonometry and what it returns is by definition accepted by the format.
- **Geometry is centred on its bounding box**, so the default pivot (0.5, 0.5) already puts the transform on
  the box centre (what an editor drags, sizes and aligns against; every gizmo and the picker assume it).
  The centroid constants in `alignment` are what an author picks to ROTATE about the centre of mass: for an
  odd-sided polygon the two centres are up to 0.145 apart, and rotating about the box centre makes a
  pentagon wobble instead of spin. They are exact (`ShapeCatalogService.GetCentroidPivot`). Only forms
  whose two centres differ appear: even-sided polygons and the circle balance on the box centre, so their
  answer is `center_middle`.
