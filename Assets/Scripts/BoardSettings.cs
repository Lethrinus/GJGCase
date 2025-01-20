// Global container for board settings chosen by the player.
public static class BoardSettings
{
    // Default values
    public static int Rows = 5;
    public static int Columns = 8;

    // Optional : Define the limits 
    public const int MinRows = 2;
    public const int MaxRows = 10;

    public const int MinColumns = 2;
    public const int MaxColumns = 10;
}