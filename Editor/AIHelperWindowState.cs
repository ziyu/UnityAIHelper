using System;

namespace UnityAIHelper.Editor
{

    [Flags]
    public enum AIHelperDirtyFlag
    {
        None=0,
        Message=1<<0,
        Chatbot=1<<1,
        ChatbotList=1<<2,
        All = Message | Chatbot | ChatbotList
    }
}