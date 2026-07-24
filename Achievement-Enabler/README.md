# CK3 Achievement Enabler

在《Crusader Kings III》的游戏过程中开启调试模式或进行切换角色等操作后，维持当前游戏的 Steam 成就资格。成就继续按照游戏原有条件触发。

## 支持版本

- Crusader Kings III `1.19.0.6 (Scribe)`
- Steam build `23530548`
- `ck3.exe` SHA-256：`2D00FF3101EF70B566F2FCBAE292F09263199C80E9DC8F139B82D7D96F83DB86`
- Windows x64、Steam 版

工具会在写入前核对游戏文件哈希和六处原始机器码。版本或机器码不匹配时，程序会在内存写入前结束。

## 下载

从本仓库的 Releases 页面下载与当前 CK3 版本对应的压缩包，解压后运行其中的 `CK3AchievementEnabler.exe`。

## 使用方法

1. 正常启动 CK3，等待游戏进入主菜单。
2. 运行 `CK3AchievementEnabler.exe`。
3. 确认窗口中出现：

   ```text
   Applied all 6 patch sites.
   Read-back verification passed
   CK3 threads resumed.
   ```

4. 返回游戏，开始新游戏或载入存档。
5. 在本次游戏进程中按需开启或关闭调试模式，并进行切换角色等操作。
6. 每次重新启动 CK3 后，再运行一次本工具。

补丁完成后即可关闭工具窗口。内存补丁会在当前 CK3 进程中持续生效，直至游戏退出。

## 已验证行为

- 调试模式开启时，ESC 菜单的金色奖杯右上角可能出现感叹号。
- 关闭调试模式后，奖杯恢复为正常金色状态。
- 开关调试模式后保存的新存档继续显示成就可用。
- 重新载入该存档后，ESC 菜单继续显示允许成就。
- Steam 成就已通过正常游戏条件实际触发。

感叹号是调试状态的界面提示；成就仍由正常游戏条件触发。

## 工作原理

工具会找到当前运行的 `ck3.exe`，读取本次启动的模块基址，再使用“模块基址 + RVA”计算补丁位置。这种定位方式适应 Windows ASLR 产生的加载地址变化。

全部验证通过后，工具会短暂暂停 CK3 的线程并修改当前进程中的 6 处指令，共 16 字节：

- 维持存档及运行时的成就资格；
- 维持游戏事件传给成就系统的资格值；
- 统一 CK3 和 Jomini 的成就可用性查询结果；
- 将新存档的 `can_get_achievements` 元数据保持为 `yes`。

写入后会立即回读验证；中途失败时会尝试回滚本次已经修改的位置。修改范围限于 CK3 当前进程的内存，磁盘上的 `ck3.exe` 和存档保持原样。

## 安全与故障处理

- 每次游戏更新后需要重新定位并编译对应版本。
- 重新启动 CK3 即可清除当前进程中的全部补丁。
- Windows 进程内存写入行为可能触发安全软件的启发式警告。仓库同时提供完整 C# 源代码和 SHA-256，便于审查。
- 运行结果会写入同目录的 `CK3AchievementEnabler-report.txt`。
- 当前验证场景为 CK3 单人游戏。

## 文件

- `CK3AchievementEnabler.exe`：已编译的正式程序。
- `CK3AchievementEnabler.cs`：完整 C# 源代码。
- `SHA256SUMS.txt`：正式程序和源代码的 SHA-256。
- `COPYRIGHT.md`：使用与版权范围。

## 项目性质

本项目是非官方社区工具。Paradox Interactive、Paradox Development Studio 和 Valve 的名称及相关商标归各自权利人所有。
