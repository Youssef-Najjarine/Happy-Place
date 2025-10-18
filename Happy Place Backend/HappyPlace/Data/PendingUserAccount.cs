using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace.Data;
[Table(nameof(PendingUserAccount))]
public class PendingUserAccount
{
    public Guid Id { get; set; }
    public String EmailAddress { get; set; }
    public String PhoneNumber { get; set; }
    public String DisplayName { get; set; }
    public String Username { get; set; }
    public String HashedPassword { get; set; }
    public String VerificationCode { get; set; }
}
