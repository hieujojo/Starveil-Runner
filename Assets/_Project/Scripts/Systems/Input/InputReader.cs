using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidRunner.Core;

namespace VoidRunner.Systems.Input
{
    /// <summary>
    /// Wrapper Input System — đọc action di chuyển (composite 2DVector: A/D + mũi tên) và phát event chuyển lane.
    /// Không phụ thuộc asset inputactions — dùng composite trực tiếp để chạy ngay.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        private InputAction _moveAction;

        public event Action LaneLeft;
        public event Action LaneRight;

        private void Awake()
        {
            // Composite 2DVector → A/D/mũi tên cho giá trị x = -1/+1 (button đơn lẻ chỉ cho +1)
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
        }

        private void OnEnable()
        {
            _moveAction.performed += HandleMove;
            _moveAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.performed -= HandleMove;
            _moveAction.Disable();
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            Vector2 value = context.ReadValue<Vector2>();
            if (value.x < -0.1f) LaneLeft?.Invoke();
            else if (value.x > 0.1f) LaneRight?.Invoke();
        }
    }
}
