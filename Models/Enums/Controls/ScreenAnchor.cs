namespace BH.SDK.Models.Enums.Controls
{
    /// <summary>
    /// Where an on-screen control sits: nine corner/edge/centre positions (a thumb control is placed by
    /// reach); Handedness mirrors the layout on top.
    /// </summary>
    public enum ScreenAnchor : byte
    {
        BottomLeft = 0,
        BottomCenter = 1,
        BottomRight = 2,
        CenterLeft = 3,
        Center = 4,
        CenterRight = 5,
        TopLeft = 6,
        TopCenter = 7,
        TopRight = 8,
    }
}
