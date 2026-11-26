# 《埃格道的虚席》

[游戏主页入口](https://tap.cn/l5h0BASes)

《埃格道的虚席》是一款以奥塞梯民俗为底色的叙事解谜游戏。重返故乡的少年纳菲，卷入一场争斗与宴席并行的调查。在奥塞梯民俗的独特氛围中探寻线索，让故事与真相逐步涌现。

## 项目简介

- **类型：** 2D 叙事解谜
- **引擎：** Unity 2022.3.62f3c1
- **预计游玩时长：** 约 10 分钟
- **游戏主页：** [TapTap 聚光灯 GameJam 页面](https://tap.cn/l5h0BASes)

## 工程结构

Unity 工程位于仓库的 `New Unity Project/` 目录中，包含 `Assets/`、`Packages/` 和 `ProjectSettings/`。

| 路径 | 内容 |
| --- | --- |
| `New Unity Project/Assets/Art/` | 角色立绘、场景背景、CG、交互物与 UI 美术资源 |
| `New Unity Project/Assets/Audio/` | 背景音乐（BGM）与音效（SFX） |
| `New Unity Project/Assets/Data/` | 对话、结局、物品和手记线索等游戏数据 |
| `New Unity Project/Assets/Prefabs/` | 对话、背包、手记、结局、情绪条、设置等可复用预制体 |
| `New Unity Project/Assets/Scenes/` | 主菜单、章节场景与桌面特写场景 |
| `New Unity Project/Assets/Scripts/` | 游戏系统与 UI 逻辑，具体模块见下表 |
| `New Unity Project/Assets/Shaders/` | 项目使用的自定义 Shader |
| `New Unity Project/Packages/` | Unity 包依赖清单 |
| `New Unity Project/ProjectSettings/` | Unity 项目与编辑器设置 |
| `提交材料/` | GameJam 提交材料 |

### Scripts 模块

| 模块 | 职责 |
| --- | --- |
| `Audio` | 音乐与音效管理 |
| `Backpack` | 背包界面与物品展示 |
| `Dialogue` | 对话播放与对话界面逻辑 |
| `Ending` | 结局触发与结局展示 |
| `GlobalManager` | 全局状态、场景转场等管理 |
| `Interact` | 场景物件交互与相关触发器 |
| `Items` | 物品数据与物品交互 |
| `Journal` | 手记、线索与相关界面 |
| `MainMenu` | 主菜单逻辑 |
| `Puzzle` | 解谜与组合玩法逻辑 |
| `UI` | 设置、历史回顾、画面适配等界面系统 |

### 场景

工程包含主菜单 `MainMenu`、章节场景 `1-1` 至 `3-3`，以及用于桌面物件调查的 `TableClose-up` 场景（正式版未采用）。

## 在 Unity 中打开

1. 使用 Unity Hub 添加或打开仓库中的 `New Unity Project/` 文件夹。
2. 使用 Unity **2022.3.62f3c1** 打开项目。
3. 在 Build Settings 中选择并运行 `MainMenu` 场景。

## Git 分支

- **`main`**：稳定、可交付版本。
- **`dev`**：日常开发主线，当前 Unity 工程与开发中的功能位于该分支。
- **`feature/*`**：从开发需求拆分出的独立功能分支，完成后合并回开发主线。

需要查看最新开发内容时，请切换到 `dev` 分支。Unity 工程在该分支下仍位于 `New Unity Project/`，游戏资源和代码集中在其中的 `Assets/`。

## 项目文档与资源管理

项目设计文档、世界观、剧情、开发日志和测试记录统一维护于飞书知识库。大型二进制资源使用 Git LFS 管理。

【策划文档】
https://gcnhk6l751ri.feishu.cn/wiki/T9CEwBce1iIqPNkAGLJc98oAnMe?from=from_copylink

【美术需求文档】
https://www.kdocs.cn/l/cniVyYXhd4xW

【10月3日会议-确定选题】
会议主题：确定“情绪涌现（主题与玩法）×色彩涌现（视觉表现）×奥赛梯族民俗（叙事背景）”的大方向与核心主题，安排后续工作。
会议纪要：https://icnwyc1vx8v1.feishu.cn/docx/XV48dgoRSomE9MxUvqZcHNAEnVf?from=from_copylink

【10月5日会议-故事定稿及初步设计】
会议纪要：https://icnwyc1vx8v1.feishu.cn/wiki/TuUGwyf8mieLCmkSvHZc8lsRnkh?from=from_copylink

【10月7日会议-需求对齐（美术）】
会议纪要：https://my.feishu.cn/docx/NM4UdryGvoyvg4xWOhzclGp3nEf?from=from_copylink

【10月10日会议-项目需求对齐与关卡设计确认】
会议纪要：https://icnwyc1vx8v1.feishu.cn/docx/H1sddR03Aop31Zxwh1CcU7BhnIc?from=from_copylink

【10月11日会议-技术需求对接（1）】
https://my.feishu.cn/docx/VPLPdsy1KoxsO4xNhQVcVDzLn3g?from=from_copylink

【10月11日会议-技术需求对接（2）】
https://my.feishu.cn/docx/BFvTdOil7oSy6Dx1439cm7LYntb?from=from_copylink