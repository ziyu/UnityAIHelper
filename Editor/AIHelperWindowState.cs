using System;

namespace UnityAIHelper.Editor
{

    [Flags]
    public enum AIHelperDirtyFlag
    {
        None = 0,
        MessageList = 1 << 0,
        StreamingMessage = 1 << 1,
        Chatbot = 1 << 2,
        ChatbotList = 1 << 3,
        Session = 1 << 4,
        SessionList = 1 << 5,
        All = MessageList |StreamingMessage | Chatbot | ChatbotList | Session | SessionList
    }
}