using System.Collections;
using UnityEngine;

// Legacy state: unused by active gameplay.
public class DeliveredState : State
{
    private Coroutine transitionCoroutine;

    public DeliveredState(StateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("Package delivered");
        transitionCoroutine = machine.StartCoroutine(TransitionAfterDelay());
    }

    public override void Exit()
    {
        if (transitionCoroutine != null)
        {
            machine.StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        machine.ChangeState(machine.GetNoPackageState());
    }
}
