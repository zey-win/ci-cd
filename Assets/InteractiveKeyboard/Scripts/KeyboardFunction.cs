using TMPro;
using UnityEngine;

namespace InteractiveKeyboard.Scripts
{
    public class KeyboardFunction : MonoBehaviour
    {
        public KeyboardFunction sideKeyboard;
        public bool Mayus;

        private TMP_InputField _inputField;

        private void OnDisable()
        {
            _inputField = null;
        }

        public void AlphabetFunction(string alphabet)
        {
            if (_inputField == null)
                return;

            if (Mayus)
            {
                var upper = alphabet.ToUpper();
                _inputField.text += upper;
            }
            else
            {
                var lower = alphabet.ToLower();
                _inputField.text += lower;
            }
        }


        public void BackSpaceFunction()
        {
            string inputText = _inputField.text;
            inputText = inputText[..^1];
            _inputField.text = inputText;
        }


        public void ShiftButtonFunction()
        {
            Mayus = !Mayus;
        }


        public void ConfirmButtonFunction()
        {
            if (!_inputField) return;

            _inputField.onEndEdit?.Invoke(_inputField.text);
            _inputField = null;
        }

        public void Clear()
        {
            _inputField.text = "";
        }

        public void SetInputField(TMP_InputField field)
        {
            _inputField = field;
        }

        public void ChangeKeyboard()
        {
            sideKeyboard.SetInputField(_inputField);
            sideKeyboard.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}