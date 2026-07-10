using Microsoft.AspNetCore.Identity;

namespace Zeppelin.Entities.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
