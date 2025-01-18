namespace GptLib.Exceptions;

public class SafetyException(string message) : GptException(message)
{
}