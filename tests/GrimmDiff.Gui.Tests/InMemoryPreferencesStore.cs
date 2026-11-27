using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Gui.Tests;

internal sealed class InMemoryPreferencesStore(AppPreferencesModel initial) : IPreferencesStorePort
{
    public AppPreferencesModel Current { get; private set; } = initial;
    public List<AppPreferencesModel> Writes { get; } = [];

    public Task<AppPreferencesModel> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Current);

    public Task SaveAsync(AppPreferencesModel preferences, CancellationToken cancellationToken = default)
    {
        Current = preferences;
        Writes.Add(preferences);
        return Task.CompletedTask;
    }
}