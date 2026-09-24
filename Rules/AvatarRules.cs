namespace BH.SDK.Rules
{
    // THE AVATAR'S BALANCE, AND IT IS FROZEN ON PURPOSE. Every number here used to be a serialized
    // field on AvatarController.Settings with a ScriptableObject overriding it, which meant the game
    // had two answers for each of them and they had drifted: the asset played dashTime 0.15 while the
    // code read 0.2, dashCooldown 0.25 against 0.5, damageTime 0.2 against 0.3, collisionScale 0.4
    // against 0.5 - and knockoutSpeed 50 against a code default of 2, a factor of twenty-five.
    // sizeSpeedInfluence was worse still: the field was added after the asset was last written, so it
    // appeared in no serialized data at all and the shipped value rested on a field initializer.
    //
    // THE VALUES BELOW ARE THE ONES THAT SHIPPED - measured out of the asset, not out of the
    // initializers, because the asset is what players played.
    //
    // WHY THEY MAY NOT MOVE AGAIN. Docs/Bots/README.md promises that a bot has "the same speed, the
    // same dash, the same hitbox, the same damage" a player has, and the PlayMode bot corpus compares
    // runs of real levels against each other across sessions. Both claims are about numbers, and
    // neither survives a number that anyone can nudge in an inspector - a corpus run that moved
    // because a field was dragged is indistinguishable from one that moved because the bot got worse.
    // Levels are authored against these too: a dash covers DashSpeed * DashTime = 7.5 world units, and
    // a level built around crossing a hazard in one dash stops working the moment that product changes.
    //
    // This is a limit table like every other file in this folder, but read the difference: the rest of
    // Rules/ bounds what an author or a player may set, while nothing here is settable at all.

    /// <summary>
    /// The avatar's movement balance: frozen constants, not settings.
    /// </summary>
    public static class AvatarRules
    {
        // ONE AND A HALF DEFAULT CAMERA HEIGHTS PER SECOND (ValueRules.DefaultZoom is 10, and that
        // zoom is the full height rather than the half-extent), so the avatar crosses the screen top to
        // bottom in two thirds of a second. It was exactly one screen a second (10) first, and a cursor
        // chasing at that speed felt sluggish in precise sections. The dash and the knockback are each
        // 3.3 times it (50 against 15), so both still read as bursts rather than as walking.

        /// <summary> Ordinary walking speed, in world units per second. </summary>
        public const float MoveSpeed = 15f;

        // A bigger avatar covers more of the screen per step, so leaving its speed alone makes it feel
        // slower the larger it gets: the dodge it has to make grows while the distance it can travel
        // does not. At 1 speed is exactly proportional to size, at 0 a giant and a dot move alike.

        /// <summary> How much of the player's size carries into its speed, in [0, 1]. </summary>
        public const float SizeSpeedInfluence = 1f;

        // DashSpeed * DashTime = 7.5 world units, and that product is the real number levels are
        // authored against - it is how far one dash reaches: three quarters of the screen's height,
        // in 0.15 s. Changing either factor without the other changes the reach; changing both to
        // keep the product changes how long the avatar is uncontrollable. Neither is a free knob.

        /// <summary> Speed for the length of a dash, in world units per second. </summary>
        public const float DashSpeed = 50f;

        /// <summary> How long a dash lasts, in seconds. </summary>
        public const float DashTime = 0.15f;

        // THE SHORTEST DASH THERE IS, AS A FRACTION OF A FULL ONE - below it there is no dash at all
        // rather than a shorter one. A dash aimed at a point nearer than its reach is the same dash
        // scaled down (AvatarMovement.DashFraction); without a floor that scaling runs to zero, and a
        // cursor resting a hair from the avatar dashes every other frame, each one a trail, a sound
        // and a statistic for a move nobody can see.
        //
        // ONE AVATAR BODY (AvatarScale, 0.5 u) OF REACH, and that is the smallest floor with a
        // meaning: a dash shorter than the body it moves does not carry the avatar off its own
        // footprint. It was one world unit before (MOVEMENT_HISTORY 18-19). Stored as a fraction
        // because the reach it is measured against is scaled by the level's Player Size and Speed
        // tracks, so a fraction means the same thing always.
        //
        // WHAT IT COSTS: the shortest dash runs 0.01 s and its two windows 0.02 s, so the real rate
        // limiter at the bottom is the one observed touchable frame every dash waits for - every third
        // frame at 60 fps.

        /// <summary> The shortest dash, as a fraction of a full one - one avatar body of reach. A
        /// target nearer than this is not dashed to at all. </summary>
        public const float MinDashFraction = AvatarScale / (DashSpeed * DashTime);

        // MEASURED FROM THE LAUNCH, NOT FROM THE LANDING, and it is EXACTLY DashInvulnerabilityTime
        // - which is a deliberate change of kind, not a number that happens to match. The
        // vulnerability window used to be a DURATION (0.30 against 0.20, a tenth of a second no
        // input could close). It is now exactly ONE SAMPLED FRAME, whatever the frame rate, and the
        // gap that carried it is gone: the wanted feel is an unbroken stream of dashes that still
        // lets damage land between them.
        //
        // WHAT MAKES ONE FRAME A GUARANTEE IS THE ORDER, NOT THE CLOCK, and the order runs across
        // three files that must stay in lockstep. BaseAvatarService.DriveAvatar launches the dash
        // BEFORE AvatarController.UpdateAvatar runs, UpdateAvatar calls AvatarMovement.Observe after
        // the step, and GameAvatarService sizes the collider afterwards off the same window. So on
        // the frame the i-frames lapse: the dash is refused (ExposedSinceDash is still false),
        // Observe then sets it because that frame was touchable, the collider is sized REAL for that
        // frame, and the dash is taken on the NEXT one. Damage gets a whole frame to land, at 10 fps
        // and at 300 alike. Move the dash launch after Observe and the window silently becomes zero
        // - the avatar would be observed exposed and then made invulnerable again within one frame.
        //
        // THE SAFETY NET IS NOW THE BALANCE, which is the trade this accepts out loud. The old note
        // here said a cooldown this short rests on AvatarMovement.ExposedSinceDash instead of on the
        // numbers; that is now the design. The cost is that how often a dash comes back depends on
        // the frame rate (one frame, so a slow device waits longer in seconds) - it costs the player
        // rather than paying them, which is the only direction this may fail in.
        //
        // IT STILL MAY NOT BECOME DashTime. The i-frames have to outlast the dash for dashing
        // THROUGH a solid obstacle to work at all (IFrames_OutlastTheDash), and this tracks the
        // i-frames, so cooldown = i-frames > dash holds by construction. Dropping to the dash's own
        // length makes the tail of the dash touchable WHILE the avatar is still travelling at
        // DashSpeed, which is the tunnelling case a point-sampled narrowphase is worst at. The gap
        // belongs after the dash, not inside it. Docs/Issues/MOVEMENT_HISTORY.md 13 is the record.

        /// <summary> How long after a dash STARTS before another may be taken, in seconds. </summary>
        public const float DashCooldown = 0.3f;

        // A dash grants i-frames, and that is a rule of the game rather than a detail: levels are
        // authored around crossing a solid obstacle by dashing through it, which speed alone could
        // only achieve by tunnelling past a thin one between two collision samples.
        //
        // It is its own number rather than DashTime because "how far a dash travels" and "how long you
        // are safe" are two feel decisions. Longer than the dash, as here, is a landing grace; 0 is the
        // global off switch, which a level authored against solid obstacles needs.
        //
        // IT EQUALS DashCooldown, AND DASH SPAM IS STILL NOT IMMUNITY - but nothing in seconds says
        // so any more, so read DashCooldown's note before touching either. The window in which a
        // dashing player can be hit is one sampled frame, produced by the launch/Observe/collider
        // order rather than by the difference between two durations. It MAY NOT EXCEED the cooldown:
        // longer i-frames than cooldown means the next dash is available while the previous one is
        // still protecting, and then no frame is ever observed touchable - that IS literal immunity,
        // and it is the one arrangement AvatarRulesTests forbids outright.

        /// <summary> How long a dash keeps the avatar untouchable, in seconds. 0 means never. </summary>
        public const float DashInvulnerabilityTime = 0.3f;

        // THE DASH'S OWN SPEED, 3.3 TIMES THE WALK, and the shove is short rather than gentle: a knockback has to
        // read as something that happened TO the player, and a slow one reads as the avatar wandering
        // off under its own power. Over DamageTime the travel is 10 world units - a full screen
        // height, which is the point rather than the cost: a hit RELOCATES the player. It was tried at
        // 30 (6 units) to see whether landing nearer to where you were hit read better, and it read as
        // a nudge. The code default of 2 that this line once carried was never what shipped.
        //
        // IT IS NOT THE FEEL OF A HIT ON ITS OWN. DamageTime is what sells the hit - control is gone
        // for that whole window whatever distance the body covers - and DamageTimeout is what keeps
        // the next one off. This number only decides where the avatar lands.

        /// <summary> Speed of the shove a hit gives, in world units per second. </summary>
        public const float KnockoutSpeed = 50f;

        /// <summary> How long that shove lasts, with the avatar answering no input, in seconds. </summary>
        public const float DamageTime = 0.2f;

        // A HIT IS AN EVENT WITH A DURATION, AND THIS IS THE DURATION. One collision lasting a second
        // and a half costs ONE life, because every further collision inside this window is ignored -
        // without it, parking the avatar inside a wall would drain a run in a handful of frames.
        //
        // FIVE TIMES THE KNOCKBACK, and the gap is the point: control comes back long before the
        // player can be hit again, so a shove into a second hazard is survivable. Confusing the two
        // windows is the easy mistake - DamageTime is how long the avatar is NOT STEERING, this is how
        // long it CANNOT BE HIT.
        //
        // It was the last of these numbers to live on a ScriptableObject (GameSettings, now deleted),
        // and it is here for the reason the rest are: the warm bot's route verifier counts a replayed
        // hit with it, so the count only means what the run's means while the two share the number.

        /// <summary> How long after a counted hit every further collision is ignored, in seconds. </summary>
        public const float DamageTimeout = 1f;

        /// <summary> The avatar's own scale, before the level's own Player Size track. </summary>
        public const float AvatarScale = 0.5f;

        // ONE NUMBER FOR BOTH DIRECTIONS, and the asymmetry between an arrival and a death is
        // carried by the EASE rather than by the duration - Core's AvatarPresenceRamp pops in on
        // OutBack and decays out on OutQuad. Two durations would be two knobs for one decision, and
        // the thing a player actually reads is the shape of the curve, not its length.
        //
        // 0.5 IS THE CEILING AND THIS SITS UNDER IT, which is the whole reason the number settled
        // where it did. The animation has to nest inside the checkpoint rewind: a death compresses
        // out during CheckpointRamp's 0.5 s slowdown and the respawn pops back in during its
        // speed-up, so the two clocks never meet.
        //
        // ABOVE 0.5 the departure is still playing when the seek happens -
        // GameAvatarService.ApplyRespawn calls Reset, the arrival replaces the departure mid-way,
        // the avatar never reaches nothing, and therefore is never REMOVED either, since
        // IsDespawned cannot become true. Nothing crashes and no frame is wrong; the death simply
        // reads as a shrink that changed its mind. It was tried at 0.8 to see the curves, and that
        // is what it looked like.
        //
        // It was 0.2 first - a dash's length, the shortest interval this game already asks the eye
        // to resolve. 0.3 is that with enough room to read the ease, and still a comfortable margin
        // under the ceiling.

        /// <summary> How long the avatar takes to grow in, and to compress back to a point, in
        /// seconds. </summary>
        public const float SpawnTime = 0.3f;

        // The hitbox is SMALLER than what is drawn, deliberately and by well over a third: a bullet
        // that visibly clips the avatar's outline and does not kill reads as generous, while the
        // reverse reads as broken. Every genre this game sits in makes the same call, and this is
        // where the genre actually sits - Just Shapes & Beats runs a hitbox 0.4 of its body's
        // half-extent. At AvatarScale 0.5 this lands the radius at 0.15 world units against their
        // 0.11, so the avatar is a touch less forgiving than theirs and much more so than the 0.2 it
        // carried before.

        /// <summary> The collision radius as a fraction of the avatar's drawn scale. </summary>
        public const float CollisionScale = 0.3f;

        // Frame-rate dependent by construction (`lerp(current, target, speed * dt)`), which is why it
        // is 30 rather than a fraction: it is a per-second rate, not a per-frame one. It moves nothing -
        // only the heading the avatar is drawn facing.

        /// <summary> How fast the drawn heading catches up with the direction of travel. </summary>
        public const float RotateLerpSpeed = 30f;

        // WELL UNDER WHAT A PLAYER CAN SEE and well over the noise a resting stick, a moving camera or
        // a pointer between two pixels produces. The avatar is about 0.5 across.

        /// <summary> How close to a target counts as standing on it, in world units. </summary>
        public const float ArrivedDistance = 0.01f;
    }
}