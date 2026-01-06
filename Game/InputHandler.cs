using System.Collections.Generic;
using System.Windows.Input;

namespace BIMDefender.Game
{
    /// <summary>
    /// Manages keyboard input state for the game
    /// </summary>
    public class InputHandler
    {
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

        public bool IsLeftPressed => _pressedKeys.Contains(Key.Left) || _pressedKeys.Contains(Key.A);
        public bool IsRightPressed => _pressedKeys.Contains(Key.Right) || _pressedKeys.Contains(Key.D);
        public bool IsFirePressed => _pressedKeys.Contains(Key.Space);
        public bool IsPausePressed => _pressedKeys.Contains(Key.Escape) || _pressedKeys.Contains(Key.P);

        public void KeyDown(Key key)
        {
            _pressedKeys.Add(key);
        }

        public void KeyUp(Key key)
        {
            _pressedKeys.Remove(key);
        }

        public void ClearPauseKey()
        {
            _pressedKeys.Remove(Key.Escape);
            _pressedKeys.Remove(Key.P);
        }

        public void Reset()
        {
            _pressedKeys.Clear();
        }
    }
}
