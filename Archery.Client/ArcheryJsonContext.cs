using System.Text.Json;
using System.Text.Json.Serialization;

namespace Archery.Client;

/// <summary>
/// Source-generated serialization metadata for every type that crosses the wire.
/// </summary>
/// <remarks>
/// Required by the MAUI Android head: Release builds trim and AOT-compile, and the
/// reflection-based System.Net.Http.Json overloads are marked RequiresUnreferencedCode /
/// RequiresDynamicCode. Without generated metadata the failure is a Release-only, device-only
/// deserialization that silently returns nothing — which is why the client passes a
/// JsonTypeInfo on every call rather than relying on a resolver and a suppression.
///
/// The options below reproduce JsonSerializerDefaults.Web, which is what
/// System.Net.Http.Json applied implicitly before this existed. They must stay in step with
/// it, or the wire format changes under everyone: the contract tests in Archery.Client.Tests
/// assert camelCase names, integer enums and the absence of the computed display properties
/// precisely so that a drift here fails loudly.
///
/// Note there is deliberately no JsonStringEnumConverter. The API registers none either, so
/// enums travel as their ordinal integers; adding one on this side alone would silently break
/// every enum-valued field.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(Member))]
[JsonSerializable(typeof(Member[]))]
[JsonSerializable(typeof(MembershipFee))]
[JsonSerializable(typeof(MembershipFee[]))]
[JsonSerializable(typeof(MemberFeeOverview[]))]
[JsonSerializable(typeof(BulkFeeRequest))]
[JsonSerializable(typeof(Competition))]
[JsonSerializable(typeof(Competition[]))]
[JsonSerializable(typeof(ExternalParticipant))]
[JsonSerializable(typeof(ExternalParticipant[]))]
[JsonSerializable(typeof(CompetitionParticipant))]
[JsonSerializable(typeof(CompetitionParticipant[]))]
[JsonSerializable(typeof(CompetitionResult))]
[JsonSerializable(typeof(CompetitionResult[]))]
[JsonSerializable(typeof(DashboardData))]
[JsonSerializable(typeof(TrainingSessionDetail))]
[JsonSerializable(typeof(SaveTrainingAttendanceRequest))]
[JsonSerializable(typeof(DateOnly[]))]
[JsonSerializable(typeof(ImportResult))]
public sealed partial class ArcheryJsonContext : JsonSerializerContext;

/// <summary>Shared options, so every call site serializes identically.</summary>
public static class ArcheryJsonOptions
{
    public static JsonSerializerOptions Default { get; } = ArcheryJsonContext.Default.Options;
}
