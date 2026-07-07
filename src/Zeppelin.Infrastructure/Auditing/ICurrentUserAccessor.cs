namespace Zeppelin.Infrastructure.Auditing;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
