# Unity AI Helper
[中文文档](README_CN.md)

Unity AI Helper is an AI-based Unity editor extension that revolutionizes development workflows through intelligent, dynamic code generation and execution.

## Features

- 🚀 Dynamic Code Compilation: Instantly generate, compile, and execute C# code within the Unity Editor
- 🔌 Unity API Integration: Direct access and manipulation of Unity APIs through dynamic code execution
- 🤖 Intelligent AI Assistant: Advanced chatbot with context-aware Unity development support
- 🛠 Flexible Code Generation: Create complex scripts and components on-the-fly
- 🧰 Extensible Tool System: Adapt and extend Unity workflows with AI-powered code generation
- 💬 Real-time Code Streaming: Generate and execute code snippets in real-time

## Code Execution Capabilities

The core power of Unity AI Helper lies in its advanced dynamic code execution system:

### Instant Code Generation and Execution

- **Dynamic Compilation**: Generate and compile C# code snippets instantly
- **Full Unity API Access**: Directly interact with Unity's entire API ecosystem
- **Context-Aware Generation**: AI understands your project context and generates appropriate code

### Example Workflows

```csharp
// Example 1: Bulk Object Manipulation
"Generate code to scale all objects in the scene by 2x"
// Generates and executes code that:
// - Finds all GameObjects
// - Applies scale transformation
// - Handles Undo/Redo operations

// Example 2: Custom Editor Tools
"Create a tool that randomly generates terrain features"
// Dynamically creates:
// - Custom editor window
// - Procedural generation logic
// - Integration with Unity's terrain system

// Example 3: Runtime Behavior Injection
"Add a component that makes all enemies follow the player"
// Generates a script with:
// - Pathfinding logic
// - Component attachment
// - Performance considerations
```

### Advanced Capabilities

- **Contextual Understanding**: AI analyzes your project structure and generates contextually relevant code
- **Safe Execution**: Integrated with Unity's Undo system for non-destructive modifications
- **Rapid Prototyping**: Transform ideas into functional code within seconds
- **Cross-Component Interaction**: Generate scripts that interact with multiple Unity systems

### How It Works

1. You describe your coding task in natural language
2. AI generates precise, compilable C# code
3. Code is dynamically compiled using Unity's Roslyn compiler
4. Executed directly in the Unity Editor
5. Provides real-time feedback and error handling

## Why Dynamic Code Execution Matters

- 🚀 **Unprecedented Flexibility**: Break free from static workflows
- 💡 **Instant Prototyping**: Turn ideas into working code immediately
- 🔬 **Exploration and Learning**: Experiment with Unity APIs effortlessly
- ⚡ **Productivity Boost**: Reduce repetitive coding tasks

## Installation

### Via Unity Package Manager

1. Open Window > Package Manager
2. Click the "+" button in the top left corner
3. Select "Add package from git URL"
4. Enter: `https://github.com/ziyu/UnityAIHelper.git`

Note: [UnityLLMAPI](https://github.com/ziyu/UnityLLMAPI.git) will be automatically installed as a dependency.

## Usage

1. Open AI Helper Window
   - Select Window > AI Helper in Unity menu

2. Start Dialogue
   - Enter your coding task or development challenge
   - AI generates and executes code in real-time

## System Requirements

- Unity 2021.3 or higher
- [UnityLLMAPI](https://github.com/ziyu/UnityLLMAPI.git) 1.2.0 or higher

## License

MIT License

## Contributing

Issues and Pull Requests are welcome to help improve this project.