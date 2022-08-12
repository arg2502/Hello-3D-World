using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerBaseState, IRootState
{
    public PlayerFallState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    :base(currentContext, playerStateFactory)
    {
        _isRootState = true;
        InitializeSubState();
    }

    public override void CheckSwitchStates()
    {
        if(_ctx.IsGrounded)
        {
            SwitchState(_factory.Grounded());
        }
    }

    public override void EnterState()
    {
        Debug.LogError("ENTER FALLING");
        _ctx.Animator.SetBool(_ctx.IsFallingHash, true);
    }

    public override void ExitState()
    {
        _ctx.Animator.SetBool(_ctx.IsFallingHash, false);
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
        HandleGravity();
        CheckSwitchStates();
    }

    public void HandleGravity()
    {
        float prevYVel = _ctx.CurrentMovementY;
        _ctx.CurrentMovementY = _ctx.CurrentMovementY + (_ctx.Gravity * Time.deltaTime);
        _ctx.AppliedMovementY = Mathf.Max((prevYVel + _ctx.CurrentMovementY) * .5f, -20f);
    }
}
