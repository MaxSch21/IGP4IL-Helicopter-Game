using UnityEngine;

public class WinState : State
{
    public WinState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("You Win");
    }
}
