using System;

namespace Core
{
    public enum GameState
    {
        Lobby,
        Prompt,
        Submit,
        Vote,
        Reveal,
        GameOver
    }
    
    public class GameStateEventArgs : EventArgs
    {
        public GameState PreviousState { get; set; }
        public GameState NewState { get; set; }
    }
}
