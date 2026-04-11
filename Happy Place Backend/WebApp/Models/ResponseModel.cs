namespace HappyWorld.HappyPlace.Web.Models;

public class ResponseModel
{
    // Constructors
    private ResponseModel(bool isSuccessful, IEnumerable<String> errorMessages)
    {
        this.IsSuccessful = isSuccessful;
        this.ErrorMessages = errorMessages;
    }

    // Properties
    public bool IsSuccessful { get; }
    public IEnumerable<String> ErrorMessages { get; }

    // Methods
    internal static ResponseModel AsSuccess()
    {
        return new(true, []);
    }

    internal static ResponseModel WithErrors(IEnumerable<String> validationErrors)
    {
        return new(false, validationErrors);
    }
}
