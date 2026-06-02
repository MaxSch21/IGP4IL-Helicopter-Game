using System;

// Legacy event bus: retained only for reference/compatibility with recovery assets.
public static class MenuEvents
{
    public static MenuState CurrentState { get; private set; } = MenuState.Main;

    public static event Action<MenuState> OnMenuStateRequested;

    public static void RequestMenuState(MenuState state)
    {
        CurrentState = state;
        OnMenuStateRequested?.Invoke(state);
    }

    public static void SyncMenuState(MenuState state)
    {
        CurrentState = state;
    }
}
