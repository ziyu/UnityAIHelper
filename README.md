# Unity AI Helper
[中文文档](README_CN.md)

Unity AI Helper is an AI-based Unity editor extension that assists Unity development work through natural language interaction. It can understand developers' questions and instructions, provide answers, and execute related Unity operations.

## Features

- 🤖 Intelligent Dialogue: Interact with AI assistant through natural language
- 🛠 Unity Command Execution: Support creating and modifying game objects through dialogue
- 📝 Development Suggestions: Provide Unity-related development advice and best practices
- 🔧 Editor Integration: Perfectly integrated into Unity editor

## Installation

### Via Unity Package Manager

1. Open Window > Package Manager
2. Click the "+" button in the top left corner
3. Select "Add package from git URL"
4. Enter: `https://github.com/ziyu/UnityAIHelper.git`

Note: [UnityLLMAPI](https://github.com/ziyu/UnityLLMAPI.git) will be automatically installed as a dependency when Unity Package Manager resolves the package dependencies.

### Manual Installation

1. Download the latest version of Unity AI Helper
2. Extract to your Unity project's Assets directory
3. Install [UnityLLMAPI](https://github.com/ziyu/UnityLLMAPI.git) manually if not automatically installed

## Usage

1. Configure API Settings
   - Open Window > AI Helper in Unity menu
   - Configure your API settings in the window's settings panel


2. Open AI Helper Window
   - Select Window > AI Helper in Unity menu

3. Start Dialogue
   - Enter your question or instruction in the input box
   - Click send or press Enter key

4. Example Commands
   ```
   - Create a new game object: create cube
   - Select object: select Main Camera
   - Add component: addcomponent Cube Rigidbody
   - Set position: setposition Cube 0 1 0
   - Set rotation: setrotation Cube 45 0 0
   - Set scale: setscale Cube 2 2 2
   ```

## System Requirements

- Unity 2021.3 or higher
- [UnityLLMAPI](https://github.com/ziyu/UnityLLMAPI.git) 1.2.0 or higher (automatically installed via Package Manager)

## License

This project is open source under the MIT license.

## Contributing

Issues and Pull Requests are welcome to help improve this project.