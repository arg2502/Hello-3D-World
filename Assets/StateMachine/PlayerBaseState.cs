public abstract class PlayerBaseState
{
    protected bool _isRootState = false;
    protected PlayerStateMachine _ctx;
    protected PlayerStateFactory _factory;
    protected PlayerBaseState currentSubState;
    protected PlayerBaseState currentSuperState;
    public PlayerBaseState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    {
        _ctx = currentContext;
        _factory = playerStateFactory;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
    public abstract void CheckSwitchStates();
    public abstract void InitializeSubState();

    public void UpdateStates()
    {
        UpdateState();
        if (currentSubState != null)
        {
            currentSubState.UpdateStates();
        }
    }

    protected void SwitchState(PlayerBaseState newState)
    {
        // current state exits state
        ExitState();

        // new state enters state
        newState.EnterState();

        if (_isRootState)
        {
            // switch current state of context
            _ctx.CurrentState = newState;
        }
        else if (currentSuperState != null)
        {
            // set the current super state's sub state to the new state
            currentSuperState.SetSubState(newState);
        }
    }
    protected void SetSuperState(PlayerBaseState newState)
    {
        currentSuperState = newState;
    }

    protected void SetSubState(PlayerBaseState newState)
    {
        currentSubState = newState;
        currentSubState.SetSuperState(this);
    }
}
