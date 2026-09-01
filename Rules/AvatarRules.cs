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
        // Roughly two thirds of the default camera height per second, which is what makes a screen
        // crossable in about the time a bar of music lasts.

        /// <summary> Ordinary walking speed, in world units per second. </summary>
        public const float MoveSpeed = 10f;

        // A bigger avatar covers more of the screen per step, so leaving its speed alone makes it feel
        // slower the larger it gets: the dodge it has to make grows while the distance it can travel
        // does not. At 1 speed is exactly proportional to size, at 0 a giant and a dot move alike.

        /// <summary> How much of the player's size carries into its speed, in [0, 1]. </summary>
        public const float SizeSpeedInfluence = 1f;

        // DashSpeed * DashTime = 7.5 world units, and that product is the real number levels are
        // authored against - it is how far one dash reaches. Changing either factor without the other
        // changes the reach; changing both to keep the product changes how long the avatar is
        // uncontrollable. Neither is a free knob.

        /// <summary> Speed for the length of a dash, in world units per second. </summary>
        public const float DashSpeed = 50f;

        /// <summary> How long a dash lasts, in seconds. </summary>
        public const float DashTime = 0.15f;

        /// <summary> How long after a dash before another may be taken, in seconds. </summary>
        public const float DashCooldown = 0.25f;

        // A dash grants i-frames, and that is a rule of the game rather than a detail: levels are
        // authored around crossing a solid obstacle by dashing through it, which speed alone could
        // only achieve by tunnelling past a thin one between two collision samples.
        //
        // It is its own number rather than DashTime because "how far a dash travels" and "how long you
        // are safe" are two feel decisions. Longer than the dash, as here, is a landing grace; 0 is the
        // global off switch, which a level authored against solid obstacles needs.

        /// <summary> How long a dash keeps the avatar untouchable, in seconds. 0 means never. </summary>
        public const float DashInvulnerabilityTime = 0.2f;

        // FIVE TIMES THE WALKING SPEED, and the shove is short rather than gentle: a knockback has to
        // read as something that happened TO the player, and a slow one reads as the avatar wandering.
        // The code default of 2 that this replaces was never what shipped.

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

        // The hitbox is SMALLER than what is drawn, deliberately and by a fifth: a bullet that visibly
        // clips the avatar's outline and does not kill reads as generous, while the reverse reads as
        // broken. Every genre this game sits in makes the same call.

        /// <summary> The collision radius as a fraction of the avatar's drawn scale. </summary>
        public const float CollisionScale = 0.4f;

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
