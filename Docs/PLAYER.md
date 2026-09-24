# The player

What the avatar does, and every number it does it with.

> **THIS FILE IS THE SOURCE OF TRUTH.** Every avatar parameter and every rule below is decided
> *here*, and the rest of the project is built to match: `Rules/AvatarRules.cs` holds the same values
> as `const`, `Avatars/AvatarMovement.cs` implements the logic, `AvatarRulesTests` pins the numbers a
> second time so one cannot drift, and both bots are tuned against them. A number changes in THIS
> FILE first; code that disagrees with it is a bug in the code. Nothing here is a description of what
> the code happens to do.

Distances are in **world units**. One unit is meaningful because the default camera is exactly
**10 units tall** (`ValueRules.DefaultZoom`), so a unit is a tenth of the screen's height and the
avatar's own body is half a unit across. A level can scale the **player** through its own tracks, and
everything below scales with that — see [Scaling](#scaling). A level can also zoom its **camera**,
and that changes none of these numbers: it changes how much of the level you can see, not how fast
you move or how far you dash.

---

## Every value

| What | Value | In plain terms |
|---|---|---|
| Walking speed | **15** u/s | Crosses the screen top to bottom in two thirds of a second |
| Dash speed | **50** u/s | 3.3 times the walk |
| Dash duration | **0.15** s | |
| **Dash reach** | **7.5** u | Speed × duration — three quarters of the screen's height |
| Dash cooldown | **0.3** s | Measured from the moment the dash **starts** |
| Dash invulnerability | **0.3** s | Also from the start, so it outlasts the travel by 0.15 s |
| Shortest dash | **0.5 u** of reach — one avatar body | Below that the dash is refused — see [Aiming a dash](#aiming-a-dash) |
| Knockback speed | **50** u/s | 3.3 times the walk |
| Knockback duration | **0.2** s | No steering during it; the avatar travels 10 u |
| Damage timeout | **1.0** s | Every further hit inside it is ignored |
| Body size | **0.5** | The drawn square |
| Hitbox | **0.3** of the body | Radius 0.15 u — smaller than what you see, deliberately |
| Spawn / despawn | **0.3** s | Growing in and compressing to a point |
| Player size affects speed | **fully** (1.0) | A bigger **avatar** moves proportionally faster. This is the level's Player Size track — **the camera's zoom has nothing to do with it** |
| Heading turn rate | **30** /s | Cosmetic: which way the body faces |
| Arrival distance | **0.01** u | Nearer than this to your cursor counts as standing on it |

Derived, because these are the numbers that actually decide a dodge:

| | |
|---|---|
| Shortest dash: distance | **0.5** u |
| Shortest dash: duration | **0.01** s |
| Shortest dash: cooldown and i-frames | **0.02** s each |
| Fastest possible dashing | **every third frame** at 60 fps — 20 per second |
| Knockback distance | **10** u |

---

## Moving

You steer in one of two ways, and the game never mixes them:

- **A direction** — WASD, a gamepad stick, the gyro. The avatar moves the way you are pushing, at
  walking speed. A stick pushed halfway moves at half speed; that is the only "walk slowly" there is.
- **A cursor** — mouse, or a finger. The avatar **chases** the point, it does not teleport to it. It
  moves at its own walking speed and lags behind a fast mouse.

Movement is instant in both: there is no acceleration, no slide, no momentum. You stop the frame you
stop asking to move.

---

## Dashing

A dash is a short burst at 3.3 times walking speed. While it runs you are **invulnerable**, and the
invulnerability lasts longer than the movement does — 0.3 s against 0.15 s. That extra time is the
landing grace: you are still untouchable for a moment after the dash puts you down, so dashing
*through* something solid works.

**You steer during a dash.** Whichever way you are asking to go — the direction you are holding, or
the point you are aiming at — is where the dash goes, and you may change it mid-dash. A dash with
nothing held moves nothing.

### Aiming a dash

With a cursor, the dash goes to **where you are pointing** and stops there. How far you are pointing
decides how long the dash is:

| Where your cursor is | What happens |
|---|---|
| Further than 7.5 u | An ordinary full dash towards it — you stop short of the cursor |
| Between 0.5 and 7.5 u | A **shorter dash that ends exactly on the cursor** |
| Nearer than 0.5 u — one avatar body | **No dash at all** — the press does nothing |

A shorter dash is the same dash scaled down: the travel, the invulnerability and the cooldown all
shrink by the same amount. A half-length dash is invulnerable for half as long and comes back in half
the time. **It is never a cheaper dash** — two half dashes cost exactly the time one full dash costs
and cover exactly the same ground.

A refused dash costs nothing: no cooldown starts, no window opens, nothing is spent. And because the
dash needs somewhere to go, a direction player holding nothing gets the same answer — no dash.

### Dashing over and over

The cooldown and the invulnerability are the **same length**, so you can dash again the moment the
last one's protection ends. Holding the dash button gives you an unbroken stream of dashes.

**It is not invulnerability.** Between one dash's protection ending and the next dash starting there
is always **exactly one frame** where you can be hit, and that frame exists whatever your frame rate
is — on a slow phone as much as on a fast PC. A player who never stops dashing is still a player who
can die; they just have to be unlucky in a much narrower window.

### Why both control styles are equal

Neither input is the meta. They buy different things with the same budget:

- A **direction** player always dashes the full 7.5 u. Reliable reach, no aiming, but no way to stop
  partway.
- A **cursor** player chooses the length. They can stop exactly on a point — which is the whole
  reason for aiming — but a short dash means shorter protection and an extra vulnerable frame per
  dash. Pointing far away gives them exactly the direction player's dash back.

Down a straight line both cover the same ground in the same time. What differs is whether you are
buying precision or simplicity — a preference, not an advantage.

---

## Getting hit

A hit happens in three stages, and they are three different lengths:

1. **The shove** — 0.2 s. You are pushed away from what hit you at 50 u/s — about 10 u, a full screen
   height — and answer no input at all. A hit **relocates** you; you cannot dash out of it, because a
   knockback outranks everything.
2. **Control returns** — after those 0.2 s you steer again.
3. **You still cannot be hit** — for a full second from the moment of the hit. That is why sitting
   inside a hazard costs one life and not ten, and why being shoved into a second hazard is
   survivable.

If you are standing exactly on top of what hit you, there is no direction to be pushed in, and you
are not pushed at all.

---

## The hitbox

**The hitbox is smaller than the avatar you see** — a circle of radius 0.15 u inside a body half a
unit across. A bullet that visibly clips your outline and does not kill you is the game working as
intended; the reverse would read as broken. The ring drawn around the avatar is the hitbox's real
size, and you can turn its visibility up or down in the settings.

While you are invulnerable — dashing, or just hit — the hitbox is **not there at all** rather than
being ignored, which is the same thing from where you are standing.

---

## Arriving and leaving

The avatar takes 0.3 s to grow into a level and 0.3 s to compress back to a point when it dies. It
answers no input during either, and it cannot be hit while arriving.

---

## Scaling

A level can animate the player's **size** and **speed** through its own tracks, and everything above
scales with them together:

- All three speeds — walk, dash and knockback — are multiplied by the same number, so a level that
  halves your speed also halves how far a dash reaches (3.75 u instead of 7.5).
- Size feeds into speed fully: a double-size avatar moves twice as fast, because it covers twice as
  much of the screen per step and the dodges it has to make grew with it. **Size here means the
  avatar's own size, set by the level's Player Size track.** The camera's zoom is a separate thing
  entirely and changes no speed, no distance and no window — zooming out shows more of the level, it
  does not make you slower. The avatar always covers 15 units a second whatever the camera does.
- The dash aiming rules use the **current** reach, not the printed 7.5 — on a level that halved your
  speed, a cursor 3.75 u away already asks for a full-length dash, and the 0.5 u floor is a quarter
  of a unit there, because the dash it is a floor under is half as long too.
- The hitbox is a fraction of the drawn body, so it shrinks and grows with the avatar too.

Windows measured in **seconds** — the cooldown, the invulnerability, the damage timeout, the spawn —
are not scaled by any of this. They are scaled only by the dash's own length, and only for the dash's
own two.

---

## What a bot may do

A bot plays through the controls you play through. It produces the same one input per frame a
keyboard produces and nothing else — same speed, same dash, same hitbox, same damage, the same
refusals. There is no path from a bot to your position, your health or the level's state. See
`Docs/Bots/README.md` in the game's own repository.
