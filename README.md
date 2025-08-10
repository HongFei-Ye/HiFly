# HiFly

HiFly 是一个基于 .NET 9 与 .NET Aspire 的现代化云原生解决方案，内置身份认证、AI 聊天与数据库管理 UI，支持可观测性、健康检查与统一服务默认配置。

## ✨ 特性

- .NET Aspire 应用编排与可观测性（OpenTelemetry）
- 基于 OpenIddict 的 OpenID Connect / OAuth 2.0 认证与授权
- BootstrapBlazor 组件与统一布局
- 可插拔的数据库管理能力
- AI Chat 组件集成
- 健康检查、服务发现与 HTTP 客户端弹性配置

## 🏗️ 项目架构

本解决方案采用分层/多项目架构，主要组件如下：

### 核心项目
- [HiFly.AppHost](HiFly.AppHost/HiFly.AppHost.csproj)：.NET Aspire 应用主机，负责服务编排与依赖管理
- [HiFly.ApiService](HiFly.ApiService/HiFly.ApiService.csproj)：Web API 服务，提供后端接口
- [HiFly.Web](HiFly.Web/HiFly.Web.csproj)：主 Web 应用程序前端
- [HiFly.ServiceDefaults](HiFly.ServiceDefaults/HiFly.ServiceDefaults.csproj)：共享服务默认配置（健康检查、遥测、服务发现等）

### 类库项目（HiFly.ClassLibrarys）
- [HiFly.DatabaseManager](HiFly.ClassLibrarys/HiFly.DatabaseManager/)：数据库管理核心库
- [HiFly.Identity](HiFly.ClassLibrarys/HiFly.Identity/)：身份认证核心库
- [HiFly.Openiddict](HiFly.ClassLibrarys/HiFly.Openiddict/HiFly.Openiddict.csproj)：OpenIddict 集成

### Razor 类库（HiFly.RazorClassLibrarys）
- [HiFly.BbLayout](HiFly.RazorClassLibrarys/HiFly.BbLayout/HiFly.BbLayout.csproj)：BootstrapBlazor 布局组件
- [HiFly.BbTables](HiFly.RazorClassLibrarys/HiFly.BbTables/)：BootstrapBlazor 表格组件
- [HiFly.BbDatabaseManagement](HiFly.RazorClassLibrarys/HiFly.BbDatabaseManagement/HiFly.BbDatabaseManagement.csproj)：数据库管理 UI
- [HiFly.OpeniddictBbUI](HiFly.RazorClassLibrarys/HiFly.OpeniddictBbUI/HiFly.OpeniddictBbUI.csproj)：OpenIddict 管理界面
- [HiFly.RippleSpa](HiFly.RazorClassLibrarys/HiFly.RippleSpa/)：前端 SPA 资源与样式

### AI 聊天模块（HiFly.AiChat）
- [HiFly.BbAiChat](HiFly.AiChat/HiFly.BbAiChat/)：AI 聊天 BootstrapBlazor 组件

### 测试项目
- [HiFly.Tests](HiFly.Tests/HiFly.Tests.csproj)：MSTest 单元与集成测试

## 🛠️ 环境要求

- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022 17.14+ 或 VS Code（建议安装 C# Dev Kit）
- Redis（用于缓存，可选）
- Docker Desktop（用于容器化发布，可选）

## 📦 快速开始

1) 克隆并进入目录
```bash
git clone <repository-url>
cd HiFly
```

2) 还原依赖
```bash
dotnet restore
```

3) 信任本地 HTTPS 证书（首次本机运行建议）
```bash
dotnet dev-certs https --trust
```

4) 运行 AppHost（Aspire 会编排各服务）
```bash
dotnet run --project HiFly.AppHost
```

5) 访问
- Web 与 API：请以 Aspire Dashboard 中显示的端口与链接为准
- Aspire Dashboard：通常可在控制台输出中查看具体地址（示例：https://localhost:15000）

## 🏃 构建与测试

- 构建
```bash
dotnet build
```

- 运行测试
```bash
dotnet test HiFly.Tests
```

## 🚀 部署

使用 .NET Aspire 的容器化发布（需本机已安装 Docker）：
```bash
dotnet publish --os linux --arch x64 /t:PublishContainer
```

## 📁 目录结构

```
HiFly/
├── HiFly.AppHost/              # Aspire 应用主机
├── HiFly.ApiService/           # Web API 服务
├── HiFly.Web/                  # 主 Web 应用
├── HiFly.ServiceDefaults/      # 共享服务配置
├── HiFly.ClassLibrarys/        # 核心类库
│   ├── HiFly.DatabaseManager/
│   ├── HiFly.Identity/
│   └── HiFly.Openiddict/
├── HiFly.RazorClassLibrarys/   # Razor 组件库
│   ├── HiFly.BbLayout/
│   ├── HiFly.BbTables/
│   ├── HiFly.BbDatabaseManagement/
│   ├── HiFly.OpeniddictBbUI/
│   └── HiFly.RippleSpa/
├── HiFly.AiChat/               # AI 聊天模块
│   └── HiFly.BbAiChat/
└── HiFly.Tests/                # 测试项目
```

## 🔧 配置说明

### 服务默认配置
[HiFly.ServiceDefaults](HiFly.ServiceDefaults/Extensions.cs) 提供统一服务配置，包括：
- 健康检查端点（/health, /alive）
- OpenTelemetry 遥测
- 服务发现
- HTTP 客户端弹性策略（重试/超时/断路）

### 身份认证
基于 OpenIddict，支持：
- OpenID Connect
- OAuth 2.0
- JWT Token
- 用户身份管理

## 🤝 贡献

- Fork 仓库
- 创建分支：`git checkout -b feature/awesome`
- 提交变更：`git commit -m "feat: awesome feature"`
- 推送分支并创建 Pull Request

## 📄 许可证

本项目基于 [LICENSE.txt](LICENSE.txt) 许可证开源。
