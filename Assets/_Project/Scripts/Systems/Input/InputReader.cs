using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidRunner.Systems.Input
{
    /// <summary>
    /// Wrapper Input System — đọc hướng di chuyển (composite 2DVector: A/D + mũi tên).
    ///
    /// Cơ chế "ĐÈ GIỮ" (fix 2026-08-11 — user yêu cầu: ko phải bấm, mà đè phím là di chuyển):
    /// KHÔNG phát event rời rạc kiểu bấm — poll trạng thái phím MỖI FRAME qua property MoveInput
    /// (x = -1 trái / +1 phải / 0 không đè). PlayerController dùng nó để TRƯỢT LIÊN TỤC khi đè
    /// (kiểu Subway Surfers — đè lâu = băng qua nhiều lane), nhả phím → tự snap về lane gần nhất.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        private InputAction _moveAction;

        /// <summary>Trạng thái phím hiện tại — đọc mỗi frame (x = -1/0/+1).</summary>
        public Vector2 MoveInput { get; private set; }

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
            _moveAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
        }

        private void Update()
        {
            if (_moveAction == null) return;
            MoveInput = _moveAction.ReadValue<Vector2>();
        }
    }
}
