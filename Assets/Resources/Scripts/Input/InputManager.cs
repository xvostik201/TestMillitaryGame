using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public event Action OnEscapePressed;
    public event Action OnLeftClick;
    public event Action OnLeftRelease;
    public event Action<Vector2> OnMouseMove;
    public event Action<float> OnScroll;

    public event Action OnTouchBegin;
    public event Action<Vector2> OnTouchDraw;
    public event Action<Vector2> OnTouchCamera;
    public event Action OnTouchEnd;

    private PlayerInputActions _inputActions;
    public Vector2 LastMousePosition { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (_inputActions != null)
        {
            _inputActions.UI.Escape.performed += HandleEscape;
            _inputActions.UI.LeftClick.started += HandleLeftClick;
            _inputActions.UI.LeftClick.canceled += HandleLeftRelease;
            _inputActions.UI.MouseMove.performed += HandleMouseMove;
            _inputActions.UI.Scroll.performed += HandleScroll;

            _inputActions.Gameplay.TouchPress.started += HandleTouchBegin;
            _inputActions.Gameplay.TouchCamera.performed += HandleTouchCamera;
            _inputActions.Gameplay.TouchDraw.performed += HandleTouchDraw;
            _inputActions.Gameplay.TouchPress.canceled += HandleTouchEnd;

            _inputActions.UI.Enable();
            _inputActions.Gameplay.Enable();
        }
    }

    private void OnDisable()
    {
        if (_inputActions != null)
        {
            _inputActions.UI.Escape.performed -= HandleEscape;
            _inputActions.UI.LeftClick.started -= HandleLeftClick;
            _inputActions.UI.LeftClick.canceled -= HandleLeftRelease;
            _inputActions.UI.MouseMove.performed -= HandleMouseMove;
            _inputActions.UI.Scroll.performed -= HandleScroll;

            _inputActions.Gameplay.TouchPress.started -= HandleTouchBegin;
            _inputActions.Gameplay.TouchCamera.performed += HandleTouchCamera;
            _inputActions.Gameplay.TouchDraw.performed += HandleTouchDraw;
            _inputActions.Gameplay.TouchPress.canceled -= HandleTouchEnd;

            _inputActions.UI.Disable();
            _inputActions.Gameplay.Disable();
        }
    }

    private void HandleEscape(InputAction.CallbackContext context)
    {
        OnEscapePressed?.Invoke();
    }

    private void HandleLeftClick(InputAction.CallbackContext context)
    {
        OnLeftClick?.Invoke();
    }

    private void HandleLeftRelease(InputAction.CallbackContext context)
    {
        OnLeftRelease?.Invoke();
    }

    private void HandleMouseMove(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = context.ReadValue<Vector2>();
        LastMousePosition = mousePosition;
        OnMouseMove?.Invoke(mousePosition);
    }

    private void HandleScroll(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();
        OnScroll?.Invoke(scroll.y);
    }

    private void HandleTouchBegin(InputAction.CallbackContext context)
    {
        OnTouchBegin?.Invoke();
    }

    private void HandleTouchCamera(InputAction.CallbackContext context)
    {
        Vector2 touchPosition = context.ReadValue<Vector2>();
        OnTouchCamera?.Invoke(touchPosition);
    }
    private void HandleTouchDraw(InputAction.CallbackContext context)
    {
        Vector2 touchPosition = context.ReadValue<Vector2>();
        OnTouchDraw?.Invoke(touchPosition);
    }

    private void HandleTouchEnd(InputAction.CallbackContext context)
    {
        OnTouchEnd?.Invoke();
    }
}
