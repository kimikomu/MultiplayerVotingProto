using System;

namespace Core
{
    public class GameStateEventArgs : EventArgs
    {
        public GameState PreviousState { get; set; }
        public GameState NewState { get; set; }
    }
}
