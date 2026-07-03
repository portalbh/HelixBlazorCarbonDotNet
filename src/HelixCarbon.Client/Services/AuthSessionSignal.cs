namespace HelixCarbon.Client.Services;

public sealed class AuthSessionSignal
{
    public event Action? Unauthorized;

    public void NotifyUnauthorized() => Unauthorized?.Invoke();
}
