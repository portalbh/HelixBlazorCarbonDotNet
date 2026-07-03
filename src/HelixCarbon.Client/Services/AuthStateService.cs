using HelixCarbon.Shared.DTOs;

namespace HelixCarbon.Client.Services;

public sealed class AuthStateService
{
    public UserProfileDto? Profile { get; private set; }

    public bool IsAuthenticated => Profile is not null;

    public bool IsLoaded { get; private set; }

    public event Action? Changed;

    public AuthStateService(HelixApiClient api, AuthSessionSignal sessionSignal)
    {
        _api = api;
        sessionSignal.Unauthorized += Clear;
    }

    private readonly HelixApiClient _api;

    public async Task EnsureLoadedAsync()
    {
        if (IsLoaded)
        {
            return;
        }

#if AuthBFF || AuthAdvanced
        Profile = await _api.GetProfileAsync();
#endif
        IsLoaded = true;
        Changed?.Invoke();
    }

    public async Task RefreshAsync()
    {
        IsLoaded = false;
        await EnsureLoadedAsync();
    }

    public void Clear()
    {
        Profile = null;
        IsLoaded = true;
        Changed?.Invoke();
    }
}
