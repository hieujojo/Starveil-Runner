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

        // ---- Swipe (mobile 2026-08-12 — user: "tối ưu cho mobile, thêm cách vuốt là được") ----
        // Dùng Pointer của Input System (bắt cả TOUCH lẫn kéo CHUỘT desktop → test trên web dễ).
        // Vuốt ngang quá ngưỡng → mô phỏng giữ phím hướng đó ~0.32s → PlayerController xử lý như
        // "vừa bấm" (nhảy 1 lane ngay) + trượt nhẹ — cảm giác đúng kiểu Subway Surfers.
        private const float SwipeThresholdPx = 45f;
        private const float SwipeHoldDuration = 0.32f;
        private Vector2 _pointerStart;
        private bool _pointerDown;
        private float _swipeHold;
        private float _swipeDir;

        /// <summary>Trạng thái phím hiện tại — đọc mỗi frame (x = -1/0/+1). Gộp bàn phím + swipe.</summary>
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
            Vector2 keyboard = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            HandleSwipe();

            float x = 0f;
            if (Mathf.Abs(keyboard.x) > 0.1f)
            {
                x = keyboard.x;
                _swipeHold = 0f; // phím đè — hủy swipe còn dư (tránh cộng hướng)
            }
            else if (_swipeHold > 0f)
            {
                x = _swipeDir;
                _swipeHold -= Time.unscaledDeltaTime; // unscaled — hết hạn kể cả khi pause
            }
            MoveInput = new Vector2(x, 0f);
        }

        /// <summary>
        /// Theo dõi Pointer (touch/chuột): giữ + kéo ngang quá 45px → 1 swipe hướng đó.
        /// Ngưỡng đủ lớn để bấm nút UI (pause, slider...) không gây swipe nhầm.
        /// </summary>
        private void HandleSwipe()
        {
            Pointer ptr = Pointer.current;
            if (ptr == null) return;

            Vector2 pos = ptr.position.ReadValue();
            if (ptr.press.wasPressedThisFrame)
            {
                // FIX 2026-08-12 (góp ý reviewer): bỏ qua swipe khi bấm BẮT ĐẦU trên UI
                // (nút pause II, slider volume...) — kéo slider cũng sinh delta ngang >45px,
                // không bỏ qua sẽ vô tình đổi lane khi resume.
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    _pointerDown = false;
                    return;
                }
                _pointerDown = true;
                _pointerStart = pos;
                return;
            }

            if (_pointerDown && ptr.press.isPressed)
            {
                float dx = pos.x - _pointerStart.x;
                if (Mathf.Abs(dx) >= SwipeThresholdPx)
                {
                    _pointerDown = false;   // 1 cử chỉ = 1 swipe
                    _swipeDir = Mathf.Sign(dx);
                    _swipeHold = SwipeHoldDuration;
                }
                return;
            }

            if (!ptr.press.isPressed) _pointerDown = false;
        }
    }
}
