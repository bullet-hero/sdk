namespace BH.SDK.Rules
{
    public enum RuleGroup : byte
    {
        /// <summary>Can't summary this issue</summary>
        None = 0,

        /// <summary>Must fix issue. Without fix, you can't play level</summary>
        Error = 1,

        /// <summary>Desirable to fix issue. You still can play level, but better fix it</summary>
        Warning = 2,

        /// <summary>Not necessary to fix issue. You can just ignore it, but listen it is better</summary>
        Advice = 3,
    }
}