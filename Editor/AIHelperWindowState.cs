using System;

namespace UnityAIHelper.Editor
{

    [Flags]
    public enum AIHelperDirtyFlag
    {
        None = 0,
        SendingMessage = 1 << 0,
        MessageList = 1 << 1,
        StreamingMessage = 1 << 2,
        Chatbot = 1 << 3,
        ChatbotList = 1 << 4,
        Session = 1 << 5,
        SessionList = 1 << 6,
        All = SendingMessage|MessageList |StreamingMessage | Chatbot | ChatbotList | Session | SessionList
    }
}