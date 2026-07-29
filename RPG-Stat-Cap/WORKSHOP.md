[h1]RPG Stat Caps[/h1]

[h1]简体中文（中国大陆）[/h1]

CK3 的各种修正几乎可以无限叠加。这种机制很适合角色扮演和控制台玩法，但也可能把骑士战斗力等数值推到四位数百分比。本模组限制最终结果，而不是逐项削弱原版增益来源。

[h2]版本 1.0[/h2]

[list]
[*]限制玩家角色的最终骑士战斗力
[*]默认上限为 200%
[*]可选 150%、200%、250%、300% 和 400%
[*]可以关闭上限并移除抵消修正
[*]在修正变化和角色继承后自动校正
[*]统一批量检测；已经正确的修正不会重复写入
[*]支持现有存档
[*]支持简体中文（中国大陆）与 English (United Kingdom)
[/list]

[h2]使用方法[/h2]

打开“决议”窗口，选择“配置 RPG 数值上限”，选择一个预设值并确认。最终数值会在约两秒内完成校正，游戏暂停时同样生效。

[h2]为什么这是真正的硬上限？[/h2]

模组读取玩家角色已经包含特质、宝物、建筑、勋号、事件、控制台工具和其他模组影响的最终骑士战斗力，然后只抵消超过所选上限的部分。它不会单纯降低每点武勇造成的伤害，也不会只削弱几个已知增益来源。

模组绝不会添加正面骑士战斗力修正。自然数值低于或等于所选上限时会保持原值；如果自然数值后来降到上限以下，旧的负面修正会被移除，使角色恢复到自然数值。

[h2]性能[/h2]

一个不可见的计时器每两秒调用一次统一批处理入口。每轮计算使用临时变量；只有在修正缺失、所需修正量发生变化或最终数值没有落在所选上限时，才会更新持久修正。某次修改没有完整生效时，下一轮会自动重试。

[h2]兼容性[/h2]

适用于 CK3 1.19.x (Scribe)，已在 1.19.0.6 验证。不使用 replace_path，也不覆盖原版玩法数据库。

为了在暂停状态下运行动态检测，本模组覆盖 gui/shared/sounds.gui，并保留该游戏版本的原版内容。任何同样覆盖该文件的界面模组或大型转换模组都需要兼容补丁。

本模组面向单人角色扮演存档。

[h2]移除模组[/h2]

选择“不设上限”，等待至少两秒，保存游戏，然后再停用本模组。

[h1]English (United Kingdom)[/h1]

CK3 modifiers can stack almost without limit. This suits role-playing and console-driven campaigns, but can also push values such as Knight Effectiveness into four-digit percentages. This mod caps the final result instead of weakening individual vanilla bonus sources.

[h2]Version 1.0[/h2]

[list]
[*]Caps the player character's final Knight Effectiveness
[*]200% default
[*]150%, 200%, 250%, 300% and 400% presets
[*]Optional no-cap setting that removes the correction
[*]Automatic correction after modifier changes and succession
[*]One shared batch check; an already correct modifier is not rewritten
[*]Works with existing saves
[*]Simplified Chinese (Mainland China) and English (United Kingdom) localisation
[/list]

[h2]How to use[/h2]

Open Decisions, choose “Configure RPG Stat Caps”, select a preset and confirm. The final value is corrected within about two seconds, including while the game is paused.

[h2]What makes this a hard cap?[/h2]

The mod reads the player's final Knight Effectiveness, including bonuses from traits, artefacts, buildings, accolades, events, console tools and other mods. It then offsets only the portion above the selected ceiling. It does not merely reduce damage per Prowess or weaken a few known bonus sources.

The mod never adds a positive Knight Effectiveness modifier. A natural value at or below the selected cap is left unchanged. If the natural value later falls below the cap, the old negative correction is removed so that the character returns to the natural value.

[h2]Performance[/h2]

One invisible timer calls a shared batch entry point every two seconds. Each calculation uses temporary variables. The persistent correction is updated only when it is missing, its required magnitude has changed, or the final value is not at the selected cap. If a write does not apply completely, the next check retries it automatically.

[h2]Compatibility[/h2]

Built for CK3 1.19.x (Scribe) and verified on 1.19.0.6. The mod does not use replace_path or overwrite vanilla gameplay databases.

To keep dynamic checks running while the game is paused, the mod overrides gui/shared/sounds.gui while preserving the vanilla content from this game version. Any interface or total-conversion mod that also overrides this file requires a compatibility patch.

Designed for single-player role-playing campaigns.

[h2]Removing the mod[/h2]

Select “No Cap”, wait at least two seconds, save the game, and then disable the mod.
