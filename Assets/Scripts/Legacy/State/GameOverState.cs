using UnityEngine;

// Legacy state: unused by active gameplay.
public class GameOverState : State
{
    public GameOverState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("Game Over");
        machine.character?.SetInputEnabled(false);
    }
}
