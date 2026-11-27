using System.IO;
using System.Text.Json;
using GrimmDiff.Data.Abstracts.Models;
using GrimmDiff.Data.Abstracts.Ports;

namespace GrimmDiff.Logic.Tests;

internal sealed class RecordingProcessRunner(CodexProcessResultModel result) : ICodexProcessRunnerPort
{
    public CodexProcessRequestModel? LastRequest { get; private set; }
    public bool SchemaExistedDuringRun { get; private set; }
    public bool SchemaWasValidJsonDuringRun { get; private set; }
    public bool SchemaHadUtf8BomDuringRun { get; private set; }

    public Task<CodexProcessResultModel> RunAsync(CodexProcessRequestModel request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        var schemaIndex = -1;
        for (var index = 0; index < request.Arguments.Count; index++)
            if (request.Arguments[index] == "--output-schema")
            {
                schemaIndex = index;
                break;
            }

        SchemaExistedDuringRun = schemaIndex >= 0 && schemaIndex + 1 < request.Arguments.Count &&
                                 File.Exists(request.Arguments[schemaIndex + 1]);
        if (SchemaExistedDuringRun)
        {
            var bytes = File.ReadAllBytes(request.Arguments[schemaIndex + 1]);
            SchemaHadUtf8BomDuringRun = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF });
            try
            {
                using var document = JsonDocument.Parse(bytes);
                SchemaWasValidJsonDuringRun = document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
            }
        }

        return Task.FromResult(result);
    }
}