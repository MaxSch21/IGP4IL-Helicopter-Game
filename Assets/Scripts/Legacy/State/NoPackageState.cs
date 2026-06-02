using UnityEngine;

// Legacy state: unused by active gameplay.
public class NoPackageState : State
{
    public NoPackageState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("Entered NoPackage state.");
    }
}
