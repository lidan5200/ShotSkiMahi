# ShotSkiMahiD 项目 Code Wiki

## 1. 项目概述

ShotSkiMahiD 是一个基于 WPF 的工业控制系统应用程序，用于管理和监控 ShotSki Mahi 组装系统。该系统主要功能包括：

- PLC 设备通信与控制
- 扫码枪数据采集与验证
- MES 系统 API 集成
- 生产数据统计与分析
- 实时监控与状态显示
- 系统日志管理

## 2. 项目结构

项目采用典型的 WPF 应用程序架构，遵循 MVVM 设计模式，主要分为以下几个模块：

```
├── Behaviors/           # WPF 行为类
├── Converters/          # 数据转换器
├── Models/              # 数据模型
├── Services/            # 核心服务
├── ShotSkiConfig/       # 配置文件
├── Validators/          # 数据验证器
├── ViewModels/          # 视图模型
├── Views/               # 视图（UI）
├── cfg/                 # 配置工具与文件
├── App.xaml             # 应用程序入口
├── App.xaml.cs          # 应用程序初始化
└── MainWindow.xaml      # 主窗口
```

## 3. 核心模块与职责

### 3.1 服务层 (Services)

| 服务名称 | 主要职责 | 文件路径 |
|---------|---------|----------|
| PlcService | PLC 设备通信，包括连接、读取和写入寄存器 | [PlcService.cs](file:///workspace/Services/PlcService.cs) |
| ScanService | 扫码枪通信与数据采集 | [ScanService.cs](file:///workspace/Services/ScanService.cs) |
| MesApiService | MES 系统 API 调用与集成 | [MesApiService.cs](file:///workspace/Services/MesApiService.cs) |
| ProcessController | 业务流程控制与状态管理 | [ProcessController.cs](file:///workspace/Services/ProcessController.cs) |
| ConfigService | 配置文件加载与管理 | [ConfigService.cs](file:///workspace/Services/ConfigService.cs) |
| LogService | 系统日志记录与管理 | [LogService.cs](file:///workspace/Services/LogService.cs) |
| ProductionStatsService | 生产数据统计与分析 | [ProductionStatsService.cs](file:///workspace/Services/ProductionStatsService.cs) |

### 3.2 模型层 (Models)

| 模型名称 | 主要职责 | 文件路径 |
|---------|---------|----------|
| ApiResponse | API 响应数据模型 | [ApiResponse.cs](file:///workspace/Models/ApiResponse.cs) |
| MesConfig | MES 系统配置模型 | [MesConfig.cs](file:///workspace/Models/MesConfig.cs) |
| PlcData | PLC 数据模型 | [PlcData.cs](file:///workspace/Models/PlcData.cs) |
| ProcessStatus | 流程状态数据模型 | [ProcessStatus.cs](file:///workspace/Models/ProcessStatus.cs) |
| ProductionStats | 生产统计数据模型 | [ProductionStats.cs](file:///workspace/Models/ProductionStats.cs) |
| SystemConfig | 系统配置数据模型 | [SystemConfig.cs](file:///workspace/Models/SystemConfig.cs) |

### 3.3 视图模型层 (ViewModels)

| 视图模型 | 主要职责 | 文件路径 |
|---------|---------|----------|
| MainViewModel | 主窗口视图模型，管理系统状态与用户交互 | [MainViewModel.cs](file:///workspace/ViewModels/MainViewModel.cs) |
| LogViewerViewModel | 日志查看器视图模型 | [LogViewerViewModel.cs](file:///workspace/ViewModels/LogViewerViewModel.cs) |

### 3.4 视图层 (Views)

| 视图 | 主要职责 | 文件路径 |
|-----|---------|----------|
| MainWindow | 主窗口，显示系统状态与操作界面 | [MainWindow.xaml](file:///workspace/Views/MainWindow.xaml) |
| LogViewerWindow | 日志查看窗口 | [LogViewerWindow.xaml](file:///workspace/Views/LogViewerWindow.xaml) |

## 4. 关键类与函数

### 4.1 App 类

**主要职责**：应用程序入口，负责初始化依赖注入容器和启动主窗口。

**关键函数**：
- `ConfigureServices()`: 配置依赖注入服务，初始化各种服务实例
- `LaunchMesConfigTool()`: 启动 MES 配置工具

### 4.2 MainViewModel 类

**主要职责**：管理系统状态，处理用户交互，显示实时数据。

**关键函数**：
- `StartAsync()`: 启动系统，连接设备并开始监控
- `Stop()`: 停止系统监控
- `Reset()`: 复位系统状态
- `EmergencyStop()`: 紧急停止系统
- `OnProcessStateChanged()`: 处理流程状态变化
- `UpdateAssemblyStatus()`: 更新组装状态显示

### 4.3 ProcessController 类

**主要职责**：控制业务流程，协调各服务之间的交互。

**关键函数**：
- `StartMonitoring()`: 开始监控 PLC 信号
- `MonitorPlcSignals()`: 监控 PLC 信号变化
- `OnScan1DataReceived()`: 处理扫码枪1数据
- `OnScan2DataReceived()`: 处理扫码枪2数据
- `ReadTestDataAndCompleteAsync()`: 读取测试数据并完成流程
- `UpdateProcessState()`: 更新流程状态

### 4.4 PlcService 类

**主要职责**：与 PLC 设备通信，读取和写入寄存器。

**关键函数**：
- `ConnectPlc1()`: 连接 PLC1
- `ConnectPlc2()`: 连接 PLC2
- `ReadPlc1Register()`: 读取 PLC1 寄存器
- `ReadPlc2Register()`: 读取 PLC2 寄存器
- `WritePlc1Register()`: 写入 PLC1 寄存器
- `WritePlc2Register()`: 写入 PLC2 寄存器

### 4.5 MesApiService 类

**主要职责**：与 MES 系统 API 通信，执行各种业务操作。

**关键函数**：
- `StartAsync()`: 调用 MES Start 接口
- `GetSfcKeyAsync()`: 调用 MES GetSfcKey 接口
- `AddSfcKeyAsync()`: 调用 MES AddSfcKey 接口
- `TestDataCollect2MainChildAsync()`: 调用 MES TestDataCollect 接口
- `CompleteAsync()`: 调用 MES Complete 接口

## 5. 系统架构与流程

### 5.1 系统架构

ShotSkiMahiD 采用分层架构设计：

1. **视图层**：WPF 窗口和控件，负责用户界面显示
2. **视图模型层**：MVVM 模式中的 ViewModel，处理业务逻辑和数据绑定
3. **服务层**：核心业务逻辑，包括设备通信、API 调用和流程控制
4. **模型层**：数据模型，定义系统中的数据结构
5. **配置层**：系统配置，包括设备参数和 API 配置

### 5.2 主要业务流程

1. **系统启动流程**：
   - 初始化服务容器
   - 加载配置文件
   - 连接 PLC 和扫码枪设备
   - 启动监控线程

2. **组装流程**：
   - 扫码枪1扫描产品条码（SFC）
   - 调用 MES Start 接口验证 SFC
   - PLC 发送 D3003 信号触发扫码枪2
   - 扫码枪2扫描小件条码（磁铁码）
   - 调用 MES GetSfcKey 接口验证磁铁码
   - PLC 发送 D3005 信号开始压合
   - PLC 发送 D3007 信号完成压合
   - 读取压合参数（时间和温度）
   - 调用 MES AddSfcKey 接口绑定 SFC 和磁铁码
   - 调用 MES TestDataCollect 接口上传测试数据
   - 调用 MES Complete 接口完成流程

3. **异常处理流程**：
   - 捕获并记录异常
   - 更新流程状态为错误
   - 发送错误通知
   - 复位流程

## 6. 依赖关系

| 依赖项 | 用途 | 来源 |
|-------|-----|------|
| HslCommunication | PLC 通信库 | 外部依赖 |
| CommunityToolkit.Mvvm | MVVM 工具库 | 外部依赖 |
| FluentValidation | 数据验证库 | 外部依赖 |
| Newtonsoft.Json | JSON 处理库 | 外部依赖 |
| JSONPostSendReceive.dll | API 通信库 | 本地引用 |
| ReadWriteLogIni.dll | INI 文件读写库 | 本地引用 |

## 7. 配置与部署

### 7.1 配置文件

| 配置文件 | 用途 | 路径 |
|---------|-----|------|
| setting.ini | 系统基本配置 | [ShotSkiConfig/setting.ini](file:///workspace/ShotSkiConfig/setting.ini) |
| mes_config.ini | MES 系统配置 | [cfg/mes_config.ini](file:///workspace/cfg/mes_config.ini) |
| local_config.ini | 本地配置 | [cfg/local_config.ini](file:///workspace/cfg/local_config.ini) |

### 7.2 部署步骤

1. 确保目标机器安装了 .NET Framework
2. 复制所有文件到目标目录
3. 配置 setting.ini 和 mes_config.ini 文件
4. 运行 ShotSkiMahiD.exe 启动应用程序

## 8. 运行方式

### 8.1 启动应用程序

1. 双击 ShotSkiMahiD.exe 启动应用程序
2. 系统会自动启动 MES 配置工具
3. 在主界面点击 "启动" 按钮连接设备并开始监控

### 8.2 操作流程

1. **启动系统**：点击 "启动" 按钮，系统会自动连接 PLC 和扫码枪
2. **监控状态**：系统会实时显示设备连接状态和流程状态
3. **扫码操作**：使用扫码枪扫描产品条码和小件条码
4. **查看日志**：点击 "查看日志" 按钮查看系统日志
5. **统计管理**：可编辑和重置生产统计数据
6. **紧急停止**：遇到紧急情况时点击 "紧急停止" 按钮

### 8.3 系统状态

| 状态 | 描述 |
|-----|------|
| 空闲 | 系统未启动或已复位 |
| 等待扫码枪1 | 等待产品条码扫描 |
| 调用Start接口 | 正在调用 MES Start API |
| 等待D3003信号 | 等待 PLC 扫码信号 |
| 等待扫码枪2 | 等待小件条码扫描 |
| 调用GetSfcKey接口 | 正在获取 SFC Key |
| 等待D3005信号 | 等待压合开始信号 |
| 等待D3007信号 | 等待压合完成信号 |
| 读取测试数据 | 正在读取压合参数 |
| 调用AddSfcKey接口 | 正在绑定 SFC Key |
| 调用TestDataCollect接口 | 正在上传测试数据 |
| 调用Complete接口 | 正在完成流程 |
| 流程完成 | 组装流程已完成 |
| 错误 | 流程异常 |

## 9. 故障排除

### 9.1 常见问题

| 问题 | 可能原因 | 解决方案 |
|-----|---------|----------|
| PLC 连接失败 | IP 地址或端口配置错误 | 检查配置文件中的 PLC 配置 |
| 扫码枪无响应 | 扫码枪未连接或配置错误 | 检查扫码枪连接和配置 |
| MES API 调用失败 | 网络连接问题或 API 配置错误 | 检查网络连接和 MES 配置 |
| 流程卡在某个状态 | PLC 信号未触发或设备故障 | 检查 PLC 设备和信号线路 |

### 9.2 日志查看

系统日志存储在 `Logs` 目录中，可通过以下方式查看：
1. 在主界面点击 "查看日志" 按钮打开日志查看器
2. 直接查看 `Logs` 目录中的日志文件

## 10. 代码风格与规范

- 命名规范：采用 PascalCase 命名类和方法，camelCase 命名变量
- 代码结构：遵循 C# 代码规范，使用适当的缩进和空白
- 异常处理：使用 try-catch 块捕获和处理异常
- 日志记录：使用 LogService 记录系统事件和错误
- 依赖注入：使用 Microsoft.Extensions.DependencyInjection 进行依赖注入

## 11. 总结

ShotSkiMahiD 是一个功能完善的工业控制系统应用程序，通过集成 PLC 通信、扫码枪数据采集和 MES 系统 API，实现了 ShotSki Mahi 组装过程的自动化控制和监控。系统采用分层架构设计，代码结构清晰，功能模块化，便于维护和扩展。

该系统的核心价值在于：
- 提高组装过程的自动化程度
- 确保产品质量和可追溯性
- 提供实时监控和数据统计
- 简化操作流程，提高生产效率

通过本 Code Wiki 文档，开发人员可以快速了解系统架构和功能实现，为后续的维护和扩展提供参考。