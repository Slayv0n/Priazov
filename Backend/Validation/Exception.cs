namespace Backend.Validation
{
    public class UnsafeContentException : Exception
    {
        public UnsafeContentException(string message) : base(message) { }
    }
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
