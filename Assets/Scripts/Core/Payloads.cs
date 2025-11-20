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
    }
}
