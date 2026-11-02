# MyFrame

一套自研的 Unity 游戏框架。包含**动画、技能、战斗、GOAP、节点树、对象池、UI、存档、事件、时间**十个子系统，全部收敛在单一 `MyFrame.asmdef` 内，业务层不依赖任何第三方框架。

- Unity **2022.3.62f3c1**
- `Assets/MyFrame` 下 **207 个 C# 脚本**，约 **2.6 万行**

> ### ⚠️ 关于 Odin Inspector
> 本仓库**不包含** Odin Inspector 的 DLL。
> 有 6 个脚本依赖它的序列化与检视面板；**阅读代码不受影响**，但要完整编译运行，需要自行安装 Odin Inspector。

---

## 模块

| 目录 | 脚本数 | 内容 |
| --- | --- | --- |
| `Battle` | 77 | 技能播放器（帧事件驱动）、打击检测、相机轨道、技能配置处理器 |
| `Script` | 73 | GOAP 规划器、`BehaviourExecutor`、`StateMachine`、Pool / Save / UI / Res / Event / Time 等系统 |
| `NodeSystemTree` | 21 | 节点树数据结构与运行时 |
| `Animation` | 15 | 基于 Playable 的动画控制器、二维/一维混合树、动画事件与 `AnimationChain` |
| `GOAPEditor` | 11 | GOAP 规划器与配置编辑器 |
| `Editor` | 6 | 框架通用编辑器扩展 |
| `Input` | 4 | 输入绑定与 `KeyData` |
| `Prefab` | — | 框架根节点预制体 |

---

## 核心设计

**动画：脱离AnimationController自制的系统**
用 Playable 手动编排图。转场逻辑写在脚本里而非连线里，便于断点调试；片段与混合数据独立成配置资产，便于跨角色复用；提供统一的动画事件注册与可跨节点类型混排的 `AnimationChain`。

**技能：配置里的每条数据都是「某一帧的事件」**
技能由配置驱动，用累计时间 `Δtime` 推进时间轴而非帧计数，因此一次 Update 可以推进多个逻辑帧，表现与帧率解耦。

**战斗：优先级抢占式行为仲裁**
`PlayerBehaviour` 是 `struct`（动作 / 优先级 / 检查函数 / ID），`BehaviourHandler` 走对象池。高优先级可打断低优先级，也支持用检查函数让「条件满足」覆盖「优先级更高」，避免优先级反转。

**GOAP：AND/OR 树搜索，不是 A\***
启发函数取「最便宜前置条件满足者的代价之和」，效果匹配采用乐观判定。规划生命周期短、决策频率高，因此优先保证**可解释性**与编辑期可视化，而不是全局最优。

**节点树：只提供链路，不内置执行器**
框架负责数据结构与编辑器，执行语义由使用方实现——谁挂载了节点、谁知道自己该怎么执行。

---

## 依赖

| 依赖 | 说明 |
| --- | --- |
| Unity 2022.3.62f3c1 | 见 `ProjectSettings/ProjectVersion.txt` |
| **Odin Inspector** | ❗ 未包含，需自行安装（6 个脚本使用） |
| `com.unity.addressables` 1.22.3 | `Packages/manifest.json` |
| `com.unity.cinemachine` 2.10.7 | 相机系统与相机轨道 |
| `com.unity.textmeshpro` 3.0.7 | UI 文本 |
| `com.unity.timeline` 1.7.7 | 时间轴相关 |

---

## 运行

1. 用 Unity Hub 以 **2022.3.62f3c1** 打开本目录；
2. 安装 Odin Inspector（否则 6 个脚本会报编译错误）；
3. 打开 `Assets/Scenes/SampleScene.unity`。

---

## 目录结构

```
MyFrame/
├── Assets/
│   ├── MyFrame/
│   │   ├── Animation/       # Playable 动画控制器、混合树、动画事件
│   │   ├── Battle/          # 技能播放、打击检测、相机轨道
│   │   ├── Editor/          # 编辑器扩展
│   │   ├── GOAPEditor/      # GOAP 编辑器
│   │   ├── Input/           # 输入绑定
│   │   ├── NodeSystemTree/  # 节点树
│   │   ├── Prefab/          # 根节点预制体
│   │   ├── Script/          # 核心系统（GOAP / StateMachine / BehaviourExecutor / Pool / Save / UI / Res / Event / Time）
│   │   └── MyFrame.asmdef
│   ├── Plugins/             # Odin Inspector（未入库）
│   └── Scenes/
├── Packages/
└── ProjectSettings/
```

---

## 已知限制

- 部分系统（存档、事件）只覆盖了当前项目用到的场景，未做通用化,可能会出现一些小问题；
- 编辑器工具依赖 Odin 的绘制能力，脱离 Odin 后部分面板会失效。
