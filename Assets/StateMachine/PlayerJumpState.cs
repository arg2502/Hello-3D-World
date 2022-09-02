using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState, IRootState
{
    IEnumerator IJumpResetCoroutine()
    {
        yield return new WaitForSeconds(.5f);
        _ctx.JumpCount = 0;
    }
    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base (currentContext, playerStateFactory) 
    {
        _isRootState = true;
        InitializeSubState();
    }

    public override void CheckSwitchStates()
    {
        // Debug.LogError($"MY isGrounded: {_ctx.IsGrounded}, Character Controller isGrounded: {_ctx.CharacterController.isGrounded}");
        // Debug.LogError($"AppliedMovementY: {_ctx.AppliedMovementY}");
        // Debug.LogError($"CurrentMovementY: {_ctx.CurrentMovementY}");
        // if (_ctx.IsGrounded)
        if (_ctx.IsGrounded)
        {
            SwitchState(_factory.Grounded());
        }
    }

    public override void EnterState()
    {
        HandleJump();
    }

    public override void ExitState()
    {
        _ctx.Animator.SetBool(_ctx.IsJumpingHash, false);
        if (_ctx.IsJumpPressed)
        {
            _ctx.RequiredNewJumpPress = true;
        }
        _ctx.CurrentJumpResetCoroutine = _ctx.StartCoroutine(IJumpResetCoroutine());
        if (_ctx.JumpCount == 3)
        {
            _ctx.JumpCount = 0;
            _ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
        }
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

    private void HandleJump()
    {
        if (_ctx.JumpCount < 3 && _ctx.CurrentJumpResetCoroutine != null)
        {
            _ctx.StopCoroutine(_ctx.CurrentJumpResetCoroutine);
        }
        _ctx.Animator.SetBool(_ctx.IsJumpingHash, true);
        _ctx.RequiredNewJumpPress = true;
        _ctx.IsJumping = true;
        _ctx.JumpCount += 1;
        _ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
        _ctx.CurrentMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
        _ctx.AppliedMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
        
    }

    public void HandleGravity()
    {
        bool isFalling = _ctx.CurrentMovementY <= 0f || !_ctx.IsJumpPressed;
        float fallMultiplier = 2f;
        if (isFalling)
        {
            float previousYVelocity = _ctx.CurrentMovementY;
            _ctx.CurrentMovementY = _ctx.CurrentMovementY + (_ctx.JumpGravities[_ctx.JumpCount] * fallMultiplier * Time.deltaTime);
            _ctx.AppliedMovementY = Mathf.Max((previousYVelocity + _ctx.CurrentMovementY) * 0.5f, -30f);
        }
        else
        {
            float previousYVelocity = _ctx.CurrentMovementY;
            _ctx.CurrentMovementY = _ctx.CurrentMovementY + (_ctx.JumpGravities[_ctx.JumpCount] * Time.deltaTime);
            _ctx.AppliedMovementY = (previousYVelocity + _ctx.CurrentMovementY) * 0.5f;
        }
    }
}
