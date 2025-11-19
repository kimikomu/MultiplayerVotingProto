namespace Core
{
    // Message type constants
    public static class MessageTypes
    {
        // Lobby phase
        public const string JOIN_REQUEST = "join_request";
        public const string JOIN_RESPONSE = "join_response";
        public const string PLAYER_JOINED = "player_joined";
        public const string PLAYER_LEFT = "player_left";
        public const string START_GAME = "start_game";
        
        // Game phase
        public const string STATE_CHANGED = "state_changed";
        public const string PROMPT_SENT = "prompt_sent";
        public const string SUBMIT_ANSWER = "submit_answer";
        public const string VOTING_OPTIONS = "voting_options";
        public const string SUBMIT_VOTE = "submit_vote";
        public const string REVEAL_RESULTS = "reveal_results";
        
        // Sync
        public const string SYNC_REQUEST = "sync_request";
        public const string SYNC_RESPONSE = "sync_response";
        public const string HEARTBEAT = "heartbeat";
    }
}
