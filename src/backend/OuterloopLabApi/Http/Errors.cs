namespace OuterloopLabApi.Http;

public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public sealed class UpstreamProviderException : Exception
{
    public UpstreamProviderException(string safeMessage) : base(safeMessage)
    {
        SafeMessage = safeMessage;
    }

    public string SafeMessage { get; }
}

public sealed class JsonMappingException : Exception
{
    public JsonMappingException(string safeMessage) : base(safeMessage)
    {
        SafeMessage = safeMessage;
    }

    public string SafeMessage { get; }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
