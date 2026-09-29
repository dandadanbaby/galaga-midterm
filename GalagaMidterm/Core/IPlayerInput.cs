namespace GalagaMidterm.Core;

/// <summary>
/// Interface for player input that the backend can read.
/// The front-end InputManager implements this.
/// The backend team can inject IPlayerInput to read player commands.
/// </summary>
public interface IPlayerInput
{
    bool MoveLeft { get; }
    bool MoveRight { get; }
    bool Fire { get; }
    bool Pause { get; }
}
