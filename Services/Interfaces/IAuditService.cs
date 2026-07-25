using LibraryManagement.Models;

public interface IAuditService
{
    Task SaveAsync(
        string user,
        string action,
        string entity,
        int entityId,
        string description);
}