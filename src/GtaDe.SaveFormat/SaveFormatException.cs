namespace GtaDe.SaveFormat;

/// <summary>Raised when a save file does not match the expected GTA DE layout.</summary>
public class SaveFormatException : Exception
{
    public SaveFormatException(string message) : base(message)
    {
    }

    public SaveFormatException(string message, Exception inner) : base(message, inner)
    {
    }
}
