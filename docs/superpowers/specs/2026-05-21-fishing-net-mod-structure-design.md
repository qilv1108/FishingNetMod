# FishingNetMod 项目结构设计

## 目标

创建一个独立的 SMAPI C# 模组项目骨架，用于后续实现捕鱼网功能。第一步只建立可编译、可被 SMAPI 加载的基础结构，不实现捕鱼逻辑。

## 项目位置

在当前工作目录下创建：

```text
FishingNetMod/
├── FishingNetMod.csproj
├── manifest.json
└── ModEntry.cs
```

## 构建配置

`FishingNetMod.csproj` 使用 `Microsoft.NET.Sdk`，目标框架为 `net6.0`，版本为 `0.1.0`。

项目引用 `Pathoschild.Stardew.ModBuildConfig` NuGet 包，由 SMAPI 官方构建配置自动处理：

- Stardew Valley、SMAPI、MonoGame 等依赖引用；
- 本地游戏路径检测；
- 构建后部署到游戏 `Mods` 目录；
- 发布 zip 生成。

## manifest.json

模组清单使用以下基础信息：

- `Name`: `Fishing Net Mod`
- `Author`: `ChenJianCan`
- `Version`: `%ProjectVersion%`
- `Description`: `Adds fishing net tools to Stardew Valley.`
- `UniqueID`: `ChenJianCan.FishingNetMod`
- `EntryDll`: `FishingNetMod.dll`
- `MinimumApiVersion`: `4.0.0`

`%ProjectVersion%` 由 `Pathoschild.Stardew.ModBuildConfig` 在构建输出中替换为 `.csproj` 的版本号。

## ModEntry.cs

入口代码定义命名空间 `FishingNetMod`，类 `ModEntry : Mod`，并实现：

```csharp
public override void Entry(IModHelper helper)
```

第一步仅在加载时输出一条日志：`Fishing Net Mod loaded.`，用于确认 SMAPI 能加载入口 DLL。

## 兼容性

使用 `.NET 6` 对齐 Stardew Valley 1.6 与 SMAPI 4.x 的模组开发要求。依赖由官方构建包解析，不手动绑定本机游戏 DLL 路径。

## 验证方式

创建文件后运行：

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

成功标准：项目编译通过，生成 `FishingNetMod.dll`，且没有缺失 SMAPI 或游戏依赖的错误。
