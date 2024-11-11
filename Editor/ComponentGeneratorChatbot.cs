using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public class ComponentGeneratorChatbot : ChatbotBase
    {
        private const string SYSTEM_PROMPT = @"你是一个Unity组件生成专家，专门负责：
1. 根据用户需求生成高质量的Unity组件脚本
2. 为组件生成合适的类名
3. 确保生成的代码遵循Unity最佳实践和C#编码规范

在生成组件时，你需要：
1. 根据GameObject的名称和用户的功能描述，生成一个合适且独特的类名
2. 生成完整的MonoBehaviour脚本，包含必要的using语句
3. 添加清晰的注释说明每个功能的作用
4. 实现用户描述的所有功能
5. 确保代码的可维护性和可扩展性
6. 使用Unity的最新特性和最佳实践

命名规范：
1. 类名应该是描述性的，表明组件的主要功能
2. 使用PascalCase命名约定
3. 类名应以'Controller'、'Manager'、'Behavior'等后缀结尾，具体取决于组件的功能类型
4. 避免使用过于通用的名称

代码质量要求：
1. 使用清晰的变量命名
2. 添加必要的序列化字段供Unity Inspector调整
3. 实现适当的Unity生命周期方法
4. 添加错误检查和异常处理
5. 优化性能，避免在Update中进行重复计算
6. 使用适当的访问修饰符确保封装性

请直接返回生成的代码，不需要其他解释。如果需要生成脚本名称，则只返回名称字符串。";

        public override string Id => "component_generator";
        public override string Name => "组件生成器";

        public ComponentGeneratorChatbot(Action<ChatMessage,bool> streamingCallback = null) 
            : base(SYSTEM_PROMPT, useStreaming: true, streamingCallback: streamingCallback, useHistoryStorage: false)
        {
        }
    }
}
