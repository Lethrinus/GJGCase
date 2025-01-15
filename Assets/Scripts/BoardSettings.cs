
/// <summary>
/// Global container for board settings chosen by the player.
/// </summary>
public static class BoardSettings
{
    // Default values (you can change them as you wish).
    public static int Rows = 10;
    public static int Columns = 12;

    // Optionally, define limits here if you want to clamp values in your UI logic:
    public const int MinRows = 5;
    public const int MaxRows = 20;

    public const int MinColumns = 5;
    public const int MaxColumns = 20;
}