# Changelog
此文件记录Unity AI Helper的所有重要更改。

格式基于[Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
并且本项目遵循[语义化版本](https://semver.org/lang/zh-CN/)。

## [1.0.0] - 2024-01-21
### 新增
- 基础AI对话功能
  - 支持与AI助手进行自然语言交互
  - 显示对话历史记录
  - 异步处理AI响应
  - 优雅的加载状态显示

- Unity命令系统
  - 创建游戏对象
  - 选择和删除对象
  - 添加组件
  - 设置变换属性（位置、旋转、缩放）
  - 场景保存
  - 项目构建

- 编辑器集成
  - 完整的编辑器窗口UI
  - Undo/Redo支持
  - 错误处理机制

### 优化
- 模块化设计，清晰的代码结构
- 异步操作避免阻塞主线程
- 标准的Unity Package结构

### 依赖
- Unity 2021.3+
- UnityLLMAPI 1.0.0+
