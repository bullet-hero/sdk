namespace BH.SDK.Models.Enums
{
    /// <summary> Which kind of object a level object is - the discriminator every object in the format carries. </summary>
    public enum ObjectType : byte
    {
        /// <summary> A rect with no content of its own: a parent, a pivot, a group. </summary>
        RectObject = 0,

        /// <summary> Drawn geometry, and the only thing that can also carry a hitbox. </summary>
        ShapeObject = 1,

        /// <summary> A particle emitter. </summary>
        EffectObject = 2,

        /// <summary> Text, drawn outside the two materials level content goes through. </summary>
        TextObject = 3,

        /// <summary> A placement of a prefab template, whose children are materialized into this scope. </summary>
        PrefabObject = 4,
    }
}