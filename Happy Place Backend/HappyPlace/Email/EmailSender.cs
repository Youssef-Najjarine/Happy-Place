using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace.Email;

public abstract class EmailSender
{
    // Static Fields
    private static Func<EmailSender> _initializer;

    // Methods
    public static void ResetInitializer() => SetInitializer(null);

    public static void SetInitializer(Func<EmailSender> initializer)
    {
        _initializer = initializer;
    }
}
