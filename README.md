<div align="center">

![Logo](logo.png)

<h1 align="center">JIYU Killer</h1>

<h3 align="center">极域电子教室杀手</h3>

***

[![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-12.0-blue?style=for-the-badge)](https://docs.microsoft.com/zh-cn/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge)](https://www.microsoft.com/windows/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Stars](https://img.shields.io/github/stars/JIYU-Killer?color=yellow\&style=for-the-badge)](https://github.com/liang-rising-star)
[![Downloads](https://img.shields.io/github/downloads/JIYU-Killer/total?color=orange\&style=for-the-badge)](https://github.com/liang-rising-star/JIYU-Killer)

***

</div>

## ⚠️ 免责声明（必读）

| **⚠️ 重要提示**                                    |
| :--------------------------------------------- |
| **本项目仅供计算机底层技术学习与开源交流使用**                      |
| **仅可在本人拥有完全使用权、授权合法的设备上运行**                    |
| **🚫 禁止违规滥用、恶意破坏管理秩序**                         |
| **因私自使用、滥用本程序产生的一切风险与法律责任，均由使用者自行承担，与本项目作者无关** |

若本项目无意间侵犯到您的合法权益，请及时联系作者，我方将第一时间下架、删除相关内容。

**使用本程序即表示您同意以上所有条款**

***

## 一、使用教程

### 1.1 快速开始

本程序是一个后台运行的托盘程序，运行后会：

- 在系统托盘显示一个图标
- 自动定时查杀极域电子教室相关进程
- 伪装成极域电子教室的样子

### 1.2 运行方式

**方式一：自包含版本（推荐）**

如果你不确定目标电脑是否有 .NET 8 运行环境，可以使用自包含版本：

```
publish_selfcontained\JIYU-killer.exe
```

**方式二：框架依赖版本**

体积更小，但需要目标电脑安装 .NET 8 运行时：

```
publish\JIYU-killer.exe
```

### 1.3 管理员模式

建议以管理员身份运行，可以获得更强力的查杀能力：

```powershell
# 右键以管理员身份运行
# 或使用命令
Start-Process -FilePath ".\JIYU-killer.exe" -Verb RunAs
```

### 1.4 右键菜单功能

程序托盘图标右键菜单包含以下功能：

- **设置** - 点击退出程序（需要输入密码）
- **举手** - 伪功能按钮
- **发送消息** - 伪功能按钮
- **查看文件** - 伪功能按钮
- **显示浮动工具栏** - 伪功能复选框
- **监控时显示通知消息** - 伪功能复选框
- **关于** - 伪功能按钮
- **帮助** - 伪功能按钮
- **退出** - 点击退出程序（需要输入密码）

### 1.5 退出程序

点击"设置"或"退出"按钮后，会弹出密码输入框：

- 输入任意密码即可退出程序
- 点击"取消"或关闭窗口则不退出

退出时输入的密码会自动保存到 `data.json` 文件中。

## 二、功能特性

- ✅ 后台托盘驻留，高仿原创橙色图标，隐蔽伪装
- ✅ 管理员模式：自动安装本地证书、加载签名内核驱动，底层解除极域防护并终止进程
- ✅ 非管理员模式：自动降级，后台定时查杀极域进程
- ✅ 所有驱动、证书临时运行，重启全无残留，极域自动恢复
- ✅ 全自主开发+原创图标，可直接开源无侵权，稳定不蓝屏
- ✅ 伪装的右键菜单，模拟极域电子教室界面
- ✅ 退出时自动保存密码到本地文件

## 三、开发指南

### 3.1 环境要求

- Windows 10/11 操作系统
- .NET 8 SDK（开发用）
- .NET 8 运行时（运行框架依赖版本需要）

### 3.2 安装 .NET 8 SDK

从微软官网下载并安装 .NET 8 SDK：

```
https://dotnet.microsoft.com/download/dotnet/8.0
```

安装完成后，在命令行执行以下命令验证：

```powershell
dotnet --version
```

应该显示类似 `8.0.xxx` 的版本号。

### 3.3 项目结构

```
JIYU-killer/
├── Program.cs                 # 程序入口
├── TrayApplicationContext.cs  # 核心功能实现
├── JIYU-killer.csproj        # 项目文件
├── logo.ico                  # 程序图标
├── logo.png                  # 项目图标
└── publish/                  # 框架依赖版本输出目录
└── publish_selfcontained/    # 自包含版本输出目录
```

### 3.4 编译项目

```powershell
# 编译框架依赖版本
dotnet publish --configuration Release --output "publish" --self-contained false

# 编译自包含版本
dotnet publish --configuration Release --output "publish_selfcontained" --self-contained true --runtime win-x64
```

### 3.5 编译结果说明

| 版本类型 | 输出目录                    | 大小       | 需要 .NET 运行时 |
| ---- | ----------------------- | -------- | ----------- |
| 框架依赖 | publish/                | \~437KB  | 需要          |
| 自包含  | publish\_selfcontained/ | \~74.7MB | 不需要         |

### 3.6 运行调试

```powershell
# 在开发目录直接运行
dotnet run
```

## 四、配置文件与资源说明

### 4.1 托盘图标更换

程序运行时会自动从发布目录加载图标，优先级如下：

1. **葫芦侠图标**（优先）
   - 文件名：`葫芦侠.ico`
   - 如果存在则优先加载
2. **默认图标**
   - 文件名：`logo.ico`
   - 如果没有葫芦侠.ico，则加载此文件
3. **系统图标**
   - 如果以上都不存在，使用系统默认图标

**更换方法：**

```
将自定义的 .ico 文件复制到发布目录，并重命名为 "葫芦侠.ico" 即可。
```

### 4.2 程序图标（exe图标）更换

项目编译时会使用 `logo.ico` 作为生成的 exe 程序图标。

**更换方法：**

1. 准备一个 `.ico` 格式的图标文件
2. 将其重命名为 `logo.ico`
3. 替换项目根目录下的 `logo.ico` 文件
4. 重新编译项目

也可在 `JIYU-killer.csproj` 中修改图标路径：

```xml
<ApplicationIcon>你的图标.ico</ApplicationIcon>
```

### 4.3 自定义悬浮文字

在发布目录创建 `config.txt` 文件，输入自定义的悬浮文字（可选）。

## 五、常见问题

### Q: 程序运行时没有反应？

A: 检查是否以管理员身份运行，查看同目录下的 `jiyu_killer.log` 日志文件。

### Q: 极域进程无法查杀？

A: 尝试以管理员身份运行程序。

### Q: 退出密码忘记了？

A: 查看同目录下的 `data.json` 文件，里面保存了上次使用的密码。

## 六、开源协议

本项目基于 **MIT License** 开源。

可自由学习、引用、二次开发，遵守协议约束即可。

***

**版本信息**：v1.0.0