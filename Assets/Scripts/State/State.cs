using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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