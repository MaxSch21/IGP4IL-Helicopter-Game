using System.Collections;
using UnityEngine;

// Legacy state: unused by active gameplay.
public class StartState : State
{
    private Coroutine transitionCoroutine;

    public StartState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        transitionCoroutine = machine.StartCoroutine(TransitionToNoPackage());
    }

    public override void Exit()
    {
        if (transitionCoroutine != null)
        {
            machine.StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private IEnumerator TransitionToNoPackage()
    {
        yield return new WaitForSeconds(0.5f);
        machine.ChangeState(machine.GetNoPackageState());
    }
}
