using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Legacy system: retained only for reference while active gameplay uses GameManager.GameState.
public abstract class State
{
    protected StateMachine machine;
    
    public State(StateMachine machine)
    {
        this.machine = machine;
    }
    
    public virtual void Enter()
    {
        
    }

    public virtual void Exit()
    {
        
    }

    public virtual void Update()
    {
        MakeTransition();
    }

    public virtual void MakeTransition()
    {
        
    }
}
