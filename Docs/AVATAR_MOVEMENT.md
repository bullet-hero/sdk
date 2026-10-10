# AvatarMovement: the mechanism's invariants

`UnityExtensions/Avatars/` holds the whole avatar movement mechanism as one value: `AvatarMovement`
(a `readonly struct`), `TimePoint`, `AvatarStepSpeeds` and `AvatarStepResult`. The numbers it runs on
are `Rules/AvatarRules` constants; this file is the reasoning behind the shape of the code.

## Shape

- **One value, not an object.** A bake's repair pass rewinds by restoring one of these per slot
  boundary wholesale; a mutable controller would have to be unwound field by field and the first
  forgotten field makes the replay disagree with itself. The game reassigns its own copy each frame.
- **One implementation for every consumer.** The game controller, the editor preview, the menu arena and
  a bot's route verifier all run the same `Step`. A second restatement of "is the dash still going"
  is how a verifier starts certifying routes the game will not fly.
- **No Unity runtime.** No `Time`, `Transform`, `Camera`, `Screen`, `UnityEngine.Random`: the clock
  arrives as a `float` (the level clock in the game, the replayed frame's time in a bake) and every
  direction arrives already resolved. `TimePoint` owns no clock for the same reason - reaching for
  one would make game and bake disagree at any playback speed other than 1x.
- **Camera clamping stays with the consumer** (it reads the level's camera rect for the frame).
- **`AvatarStepSpeeds`** keeps the three speeds apart from the per-frame `Scale`, so a caller holds the
  struct for a whole replay and refreshes only `Scale`. The three fields survive being constants
  because tests vary them to pin RELATIONS (damage outranks a dash, a knockback outruns a walk);
  `Default(scale)` is what the game and the verifier call.
- **`AvatarStepResult` carries four things** the animation half needs and must not recompute: heading
  target, squish, and the move/stop edges. It exposes the raw `TargetDirection` and no angle - an angle
  of a zero direction is 0 rad, "facing +X", which swings the avatar right on every arrival. The consumer
  decides what a zero direction means (`AvatarController.ResolveHeading`).

## Dash

- **One shape scaled by `DashFraction`**: travel, i-frames and cooldown are the shipped constants times
  it. It is never below `AvatarRules.MinDashFraction` and never above 1, and a dash never taken leaves it
  at 1, so "no dash yet" needs no case. `ResolveDashFraction` is here (not in the consumer) so the game
  and the verifier answer "how long is this dash" identically: beyond the reach it saturates at 1,
  inside it the dash ENDS on the point, below the floor there is no dash.
- **Windows are read through accessors**, never computed by callers: a collider sized off the bare
  constant while the step ended the dash early makes the avatar untouchable after it visibly stopped.
- **The i-frame window is a parameter** (every other window is read from `AvatarRules`): 0 is a real
  value that switches i-frames off for levels built around solid obstacles, and `DashFraction` scales
  whatever is passed.
- **`ExposedSinceDash` is a counted sample, set by `Observe`.** i-frames and cooldown are the same
  duration, so the only touchable moment is the frame between the i-frames lapsing and the next launch.
  That frame comes from the consumer's ORDER (launch, step, `Observe`, size the collider), not from the
  clock, so it holds at 10 fps and at 300. Every consumer calls `Observe` exactly once per simulated
  frame; forgetting fails CLOSED (one dash, never another). `Observe` asks the same question as the
  collider sizing, at the same instant. It is blind to whether the avatar is collidable at all: a level
  that hides the player must not be able to disarm the dash. With i-frames off every frame is exposure
  and the gate collapses to the plain cooldown.
- **A dash with no direction is not taken**: no cooldown, no window, no press. A direction hashed from
  the seed was a random number on a path the bot corpus replays, and the alternative on offer was a
  twitch (thrown out along the idle angle and hauled back at dash speed).
- **`Step`'s branch order is the meaning of "uncontrolled".** A hit outranks everything, the dash
  included (otherwise a button cancels a knockback). A dash is the walk's direction at the dash's
  speed, so both inputs steer it frame by frame; the input's length is part of the speed, so an EMPTY
  input dashes at zero (a dash launched a frame before the pause menu opened cannot carry the avatar
  across a stopped level).

## Damage

- A hit is an event with a duration; the direction is captured once (re-aimed every frame a shove
  becomes a chase, and its source may be gone a frame later).
- The direction arrives resolved and a ZERO one is legal: the knockback then moves nothing. Drawing a
  random one at that point was a `UnityEngine.Random` call this type may not make and a determinism
  break (randomness is addressed, not drawn).
- `SinceHit` reads as an enormous number before the first hit (`TimePoint.Never` sits far in the
  past), because stage 3 of taking damage is an edge and `InDamage` is false by the time a consumer can
  act on it.

## Step arithmetic

- **Arriving is a snap onto the target**, not a step of the remaining distance: `position += dir * dist`
  with `dir = toTarget / dist` leaves a rounding residue near one float epsilon, the order of the
  "any distance left" test, so a held motionless target flickered moving/stopped every frame (squish,
  eyes and trail with it).
- **The arrival clamp applies during a dash too.** The caller shortens the WINDOW to the distance first,
  so clamp and window agree and the avatar lands as the dash ends. The clamp is the guarantee, not the
  mechanism: if the level's Speed track moves mid-dash the arithmetic stops landing exactly and the clamp
  alone makes overshoot impossible. An early arrival keeps its remaining i-frames while standing still.
- **Both scalings land on `targetSpeed`**, so walk, dash and knockback scale by the same number.
- **The reported speed approaches at the speed the distance needs**, not at full speed with the overshoot
  clamped off afterwards. The position is identical either way; what changes is what the move trail is
  told (a followed route is made of steps of hundredths of a unit, and a trail launched at sprint speed
  sits on top of the avatar instead of behind it). A dash keeps its own reported speed while its travel
  is clamped; the knockback is excluded outright.

## Tests worth knowing (`UnityExtensions/Tests/`)

- `AvatarDashGateTests` sweeps frame rates against the sampled gate. Measured with the old timed
  window (0.25 cooldown against 0.20 i-frames): 3 exposed samples per dash at 60 fps, 1 at 20 fps, and
  at 15 fps 43% of dash cycles had none; a timed window cannot survive a frame longer than itself, a
  counted one cannot fail to. The gate costs two frames per cycle (one to be observed touchable, one
  for the dash), paid in frames rather than seconds, so a slow device waits longer and never gains. The
  expected exposure is exactly one frame per dash.
- `AvatarMovementStateTests` pins the agreement with `GameCycleService.ProcessDamage` (one life, then
  `DamageTimeout`) and `AvatarController.Damage` (shove, no input for `DamageTime`). A replay that
  counted every frame of an overlap reported 25 to 31 hits on a level where the run took 3, and the
  repair pass spent its whole budget on the difference.
- `AvatarPausedClockTests`: `Step` takes the time and the delta separately, so a caller can hand it a
  stopped Time with a live DeltaTime; the knockback (own direction, own window against the stopped Time)
  then never closes. The fix lives in the consumer's `PlayerService.ProcessTime` (a stopped clock reports
  DeltaTime 0) and cannot be tested here; these cases pin the half this type owns.
