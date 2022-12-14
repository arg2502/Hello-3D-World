using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerBaseState, IRootState
{
    public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base(currentContext, playerStateFactory) 
    {
        _isRootState = true;
        InitializeSubState();
    }

    public override void CheckSwitchStates()
    {
        if (_ctx.IsJumpPressed && !_ctx.RequiredNewJumpPress)
        {
            SwitchState(_factory.Jump());
        }
        else if (!_ctx.IsGrounded)
        {
            SwitchState(_factory.Fall());
        }
    }

    public void HandleGravity()
    {
        _ctx.CurrentMovementY = _ctx.Gravity;
        _ctx.AppliedMovementY = _ctx.Gravity;
    }

    public override void EnterState()
    {
        HandleGravity();
    }

    public override void ExitState()
    {
    }

    public override void InitializeSubState()
    {
        if (!_ctx.IsMovementPressed && !_ctx.IsRunPressed)
        {
            SetSubState(_factory.Idle());
        }
        else if (_ctx.IsMovementPressed && !_ctx.IsRunPressed)
        {
            SetSubState(_factory.Walk());
        }
        else
        {
            SetSubState(_factory.Run());
        }
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

}
