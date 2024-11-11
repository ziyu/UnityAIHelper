# Unity AI Helper

Unity AI Helper 是一个基于AI的Unity编辑器扩展，通过自然语言交互来协助Unity开发工作。它能够理解开发者的问题和指令，提供解答并执行相关的Unity操作。

## 功能特点

- 🤖 智能对话：通过自然语言与AI助手交互
- 🛠 Unity命令执行：支持通过对话创建、修改游戏对象
- 📝 开发建议：提供Unity相关的开发建议和最佳实践
- 🔧 编辑器集成：完美集成到Unity编辑器中

## 安装方法

### 通过Unity Package Manager

1. 打开 Window > Package Manager
2. 点击左上角的 "+" 按钮
3. 选择 "Add package from git URL"
4. 输入: `https://github.com/your-repo.git`

### 手动安装

1. 下载最新版本的Unity AI Helper
2. 解压到你的Unity项目的Assets目录下

## 使用方法

1. 打开AI Helper窗口
   - 在Unity菜单中选择 Window > AI Helper

2. 开始对话
   - 在输入框中输入你的问题或指令
   - 点击发送或按Enter键

3. 支持的命令示例
   ```
   - 创建一个新的游戏对象：create cube
   - 选择对象：select Main Camera
   - 添加组件：addcomponent Cube Rigidbody
   - 设置位置：setposition Cube 0 1 0
   - 设置旋转：setrotation Cube 45 0 0
   - 设置缩放：setscale Cube 2 2 2
   ```

## 系统要求

- Unity 2021.3 或更高版本
- UnityLLMAPI 1.0.0 或更高版本

## 许可证

本项目基于 MIT 许可证开源。

## 贡献

欢迎提交Issue和Pull Request来帮助改进这个项目。

## 联系方式

- 邮箱：liziyu1209@gmail.com
- GitHub：[项目主页](https://github.com/your-repo)
