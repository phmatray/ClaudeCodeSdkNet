namespace ClaudeCodeSdkNet.Exceptions;

public class ClaudeSDKException : Exception
{
    public ClaudeSDKException(string message) : base(message) { }
    public ClaudeSDKException(string message, Exception innerException) : base(message, innerException) { }
}

public class CLIConnectionException : ClaudeSDKException
{
    public CLIConnectionException(string message) : base(message) { }
    public CLIConnectionException(string message, Exception innerException) : base(message, innerException) { }
}

public class CLINotFoundException : ClaudeSDKException
{
    public CLINotFoundException(string message) : base(message) { }
}

public class ProcessException : ClaudeSDKException
{
    public int ExitCode { get; }
    public string? StdError { get; }
    
    public ProcessException(string message, int exitCode, string? stdError = null) : base(message)
    {
        ExitCode = exitCode;
        StdError = stdError;
    }
}

public class CLIJSONDecodeException : ClaudeSDKException
{
    public string RawData { get; }
    
    public CLIJSONDecodeException(string message, string rawData, Exception innerException) 
        : base(message, innerException)
    {
        RawData = rawData;
    }
}