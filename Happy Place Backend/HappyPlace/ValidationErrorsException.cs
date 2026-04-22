namespace HappyWorld.HappyPlace;

public class ValidationErrorsException : Exception {
    // Constructors
    public ValidationErrorsException(IEnumerable<string> validationErrors) {
        this.ValidationErrors = [.. validationErrors];
    }
    public ValidationErrorsException(string message, IEnumerable<String> validationErrors)
        : base(message) {
        this.ValidationErrors = [.. validationErrors];
    }
    public ValidationErrorsException(string message, Exception innerException, IEnumerable<String> validationErrors)
        : base(message, innerException) {
        this.ValidationErrors = [.. validationErrors];
    }
    // Properties
    public IEnumerable<String> ValidationErrors { get; }
}
