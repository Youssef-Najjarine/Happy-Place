using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace;

public class ValidationErrorsException: Exception
{
    // Constructors
    public ValidationErrorsException(IEnumerable<string> validationErrors)
    {
        this.ValidationErrors = validationErrors.ToList();
    }
    public ValidationErrorsException(string message, IEnumerable<String> validationErrors)
        : base(message)
    {
        this.ValidationErrors = validationErrors.ToList();
    }
    public ValidationErrorsException(string message, Exception innerException, IEnumerable<String> validationErrors)
        : base(message, innerException)
    {
        this.ValidationErrors = validationErrors.ToList();
    }
    // Properties
    public IEnumerable<String> ValidationErrors { get; }
}
