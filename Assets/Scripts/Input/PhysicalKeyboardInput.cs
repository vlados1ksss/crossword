using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace CrosswordGame
{
    /// <summary>
    /// Дополнительный ввод с физической клавиатуры (ПК). Работает через события Input System, без Update.
    /// Буквы принимаются как в русской раскладке, так и в английской (по положению клавиш ЙЦУКЕН).
    /// </summary>
    public class PhysicalKeyboardInput : MonoBehaviour
    {
        public event Action<char> LetterTyped;
        public event Action BackspacePressed;
        public event Action SubmitPressed;
        public event Action CancelPressed;

        private Keyboard _keyboard;
        private IDisposable _buttonSubscription;

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            Attach(Keyboard.current);
            _buttonSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            Attach(null);
            _buttonSubscription?.Dispose();
            _buttonSubscription = null;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard && (change == InputDeviceChange.Added || change == InputDeviceChange.Removed))
                Attach(Keyboard.current);
        }

        private void Attach(Keyboard keyboard)
        {
            if (_keyboard != null) _keyboard.onTextInput -= OnTextInput;
            _keyboard = keyboard;
            if (_keyboard != null) _keyboard.onTextInput += OnTextInput;
        }

        private void OnTextInput(char character)
        {
            if (char.IsControl(character)) return;
            char letter = LocalizationManager.MapPhysicalChar(character);
            if (letter != '\0') LetterTyped?.Invoke(letter);
        }

        private void OnAnyButtonPress(InputControl control)
        {
            if (!(control is KeyControl key) || !(control.device is Keyboard)) return;
            switch (key.keyCode)
            {
                case Key.Backspace:
                case Key.Delete:
                    BackspacePressed?.Invoke();
                    break;
                case Key.Enter:
                case Key.NumpadEnter:
                    SubmitPressed?.Invoke();
                    break;
                case Key.Escape:
                    CancelPressed?.Invoke();
                    break;
            }
        }
    }
}
