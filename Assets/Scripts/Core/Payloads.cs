using System;

namespace Core
{
    public class Payloads
    {
        [Serializable]
        public class JoinRequestPayload
        {
            public string playerName;
        }

        [Serializable]
        public class JoinResponsePayload
        {
            public bool success;
            public string playerId;
            public string playerName;
            public string reason;
        }

        [Serializable]
        public class PlayerJoinedPayload
        {
            public string playerId;
            public string playerName;
        }

        [Serializable]
        public class PromptPayload
        {
            public string promptText;
            public float timeLimit;
        }
        
        [Serializable]
        public class VotingOption
        {
            public string optionId;
            public string optionText;
            public bool isPlayerAnswer;
        }
        
        [Serializable]
        public class VoteResult
        {
            public string optionId;
            public string optionText;
            public int voteCount;
            public string[] voterIds;
            public string authorId;
        }
        
        [Serializable]
        public class StateChangedPayload
        {
            public string newState;
            public float timeLimit;
        }
    }
}
