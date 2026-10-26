using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public interface IExternalParticipantService : ISpreadsheetImporter
{
    Task<IReadOnlyList<ExternalParticipant>> GetAllAsync(CancellationToken ct = default);
    Task<ExternalParticipant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExternalParticipant> CreateAsync(ExternalParticipant participant, CancellationToken ct = default);
    Task<ExternalParticipant?> UpdateAsync(Guid id, ExternalParticipant input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
