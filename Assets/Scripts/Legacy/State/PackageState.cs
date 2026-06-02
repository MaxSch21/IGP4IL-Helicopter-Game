using UnityEngine;

// Legacy state: unused by active gameplay.
public class PackageState : State
{
    public PackageState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("Entered Package state.");
    }
}
