namespace Zeppelin.Auditing;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
