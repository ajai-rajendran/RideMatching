namespace RideMatching.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException() { }
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidRideStateException : DomainException
{
    public InvalidRideStateException() { }
    public InvalidRideStateException(string message) : base(message) { }
    public InvalidRideStateException(string message, Exception innerException) : base(message, innerException) { }
}

public class DriverUnavailableException : DomainException
{
    public DriverUnavailableException() { }
    public DriverUnavailableException(string message) : base(message) { }
    public DriverUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException() { }
    public EntityNotFoundException(string message) : base(message) { }
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
    public EntityNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
