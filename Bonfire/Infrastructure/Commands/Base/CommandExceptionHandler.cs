using System;

namespace Bonfire.Infrastructure.Commands.Base;

/// <summary>
/// Точка перехвата исключений, возникших при выполнении команд.
/// Назначается один раз при старте приложения (см. App.OnStartup),
/// чтобы ошибки команд логировались и показывались пользователю,
/// а не роняли приложение через необработанный async void.
/// </summary>
internal static class CommandExceptionHandler
{
    public static Action<Exception>? OnException { get; set; }

    public static void Report(Exception exception) => OnException?.Invoke(exception);
}
