using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Emitter shape spawning particles anywhere inside an axis-aligned box. The one shape of the
    /// family with no IEffectShapeSpread - a filled area has no rim to walk along.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeRectangle : IEffectShape, IModel<EffectShapeRectangle>
    {
        /// <summary> Full width/height of the spawn box, centered on the effect object. </summary>
        [RuleNotNull, RuleIVector2Min(EffectRules.Shape.BoxSize_Min)]
        [JsonProperty(Names.Size)]
        public IVector2 Size { get; set; }

        public EffectShapeRectangle()
        {
            Size = new Vector2Value(
                EffectRules.Shape.BoxSize_X_Default,
                EffectRules.Shape.BoxSize_Y_Default);
        }
        public EffectShapeRectangle(IVector2 size)
        {
            Size = size;
        }

        public EffectShapeType GetModelType() => EffectShapeType.Rectangle;
    }
}