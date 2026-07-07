using Microsoft.AspNetCore.Identity;

namespace Zeppelin.Domain.Entities.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
