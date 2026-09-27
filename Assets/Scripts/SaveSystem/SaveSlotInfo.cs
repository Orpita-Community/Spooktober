using System;
using System.Globalization;

public class SaveSlotInfo
{
    public string key;
    public bool exists;
    public bool corrupt;        // The file exists but can't be read
    public bool incompatible;   // Made by a newer version of the game
    public SaveMetadata metadata = new SaveMetadata();
    public string thumbnailPath;

    public bool IsLoadable => exists && !corrupt && !incompatible;

    public DateTime Timestamp
    {
        get
        {
            if (metadata != null && DateTime.TryParse(metadata.timestampUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime time))
                return time;

            return DateTime.MinValue;
        }
    }
}
