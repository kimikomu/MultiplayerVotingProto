using System;

namespace Core
{
    public class Payloads
    {
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
    }
}
