using BH.SDK.Models;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;

namespace BH.SDK.Rules
{
    // What a rule sees of its surroundings: the root Level plus the resolved object scope, so a Prefab,
    // LevelMeta or UserSettings validates standalone and a template's frames are measured against its
    // own timeline. Immutable: a nested scope is a new context, so RuleFixer can replay a fix against it.

    /// <summary>
    /// What a rule is allowed to know about its surroundings: the analysis root, the level it
    /// belongs to (when there is one), and the object scope and timeline currently being walked.
    /// </summary>
    public sealed class RuleContext
    {
        /// <summary> Object Analyze was called on. Never null. </summary>
        public object Root { get; }

        /// <summary> Level the walk started from, or null when validating a standalone aggregate.
        /// Only rules that resolve level-wide references need this; frame and id rules must use
        /// the scope fields below, which stay correct inside a prefab template. </summary>
        public Level Level { get; }

        /// <summary> Objects of the scope being walked - a level's own or one prefab template's. </summary>
        public IObjectScope Objects { get; }

        /// <summary> Timeline length of the scope being walked, in frames. </summary>
        public int FrameDuration { get; }

        /// <summary> Whether the current scope is a prefab template rather than the level itself.
        /// PrefabRoot as a parent only means anything here. </summary>
        public bool IsPrefabScope { get; }

        /// <summary> Whether a scope could be resolved at all. False for roots that carry no
        /// timeline (LevelMeta, UserSettings, a bare value model) - scope-dependent rules degrade to
        /// what they can still check on their own rather than failing outright. </summary>
        public bool HasScope { get; }

        private RuleContext(object root, Level level, IObjectScope objects,
            int frameDuration, bool isPrefabScope, bool hasScope)
        {
            Root = root;
            Level = level;
            Objects = objects;
            FrameDuration = frameDuration;
            IsPrefabScope = isPrefabScope;
            HasScope = hasScope;
        }

        /// <summary> Build the starting context for a root, resolving its scope if it has one. </summary>
        public static RuleContext ForRoot(object root)
        {
            switch (root)
            {
                case Level level:
                    return new RuleContext(level, level, level.Game,
                        level.Settings.FrameDuration, false, true);

                // A standalone template validates against its own timeline. Matched on the concrete type:
                // its timeline is its root's span, which no interface can declare.
                case Prefab prefab:
                    return new RuleContext(root, null, prefab, TimelineOf(prefab), true, true);

                default:
                    return new RuleContext(root, null, null, 0, false, false);
            }
        }

        /// <summary> Context for a nested scope reached from this one - keeps the level for
        /// reference lookups, swaps everything scope-local. </summary>
        public RuleContext WithScope(Prefab scope)
        {
            if (scope == null) return this;
            return new RuleContext(Root, Level, scope, TimelineOf(scope), true, true);
        }

        // Zero for a template with no root at all, which is invalid data RuleNotNull reports on its
        // own - every frame rule then degrades to "nothing fits", the same answer HasScope: false
        // produces, rather than throwing inside the walk that was about to report the real problem.
        private static int TimelineOf(Prefab prefab)
            => prefab.Root?.Span.FrameDuration ?? 0;
    }
}
