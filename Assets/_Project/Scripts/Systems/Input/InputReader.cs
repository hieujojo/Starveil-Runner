using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidRunner.Core;

namespace VoidRunner.Systems.Input
{
    /// <summary>
    /// Wrapper Input System — đọc hướng di chuyển (composite 2DVector: A/D + mũi tên) và phát event đổi lane.
    ///
    /// Fix 2026-08-11 (user test): trước đây chỉ fire 1 lần khi `performed` → bấm/ĐÈ phím chỉ qua 1 lane,
    /// phải bấm lại mới qua tiếp. Giờ **poll hướng trong Update**: bấm phát qua lane ngay,
    /// **ĐÈ GIỮ → cứ mỗi repeatInterval (0.12s) lại qua lane tiếp** → cảm giác di chuyển mượt, liên tục.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [SerializeField, Tooltip("Khoảng thời gian giữa 2 lần đổi lane khi ĐÈ phím liên tục (giây)")]
        private float repeatInterval = 0.12f;

        private InputAction _moveAction;
        private float _repeatTimer;

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
            _moveAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
        }

        private void Update()
        {
            if (_moveAction == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            Vector2 value = _moveAction.ReadValue<Vector2>();
            bool left = value.x < -0.1f;
            bool right = value.x > 0.1f;

            // Nhả phím → reset timer (lần bấm sau qua lane ngay lập tức)
            if (!left && !right)
            {
                _repeatTimer = 0f;
                return;
            }

            // Bấm lần đầu: qua lane NGAY; ĐÈ GIỮ: lặp mỗi repeatInterval giây → mượt, liên tục
            if (_repeatTimer <= 0f)
            {
                if (left) LaneLeft?.Invoke();
                else LaneRight?.Invoke();
                _repeatTimer = repeatInterval;
            }
            _repeatTimer -= Time.deltaTime;
        }
    }
}
