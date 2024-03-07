# 使用资产处理器

在导入资产后，如果它的路径满足一定要求，资产处理器会自动将预设应用到该资产上，不需要再手动修改资产的设置。对于模型，还能自动平滑它的法线。

可以在 `Project Settings/StarRail NPR Shader/HSR Asset Processors` 中配置资产处理器。

![资产处理器](../_img/asset-processor.png)

- `Match Mode`：资产的匹配方式。

    - `Name Glob`：`Path Pattern` 使用类似 Unix Glob 的语法，忽略大小写，匹配资产的名称（包含扩展名）。

        - `*`：匹配 0 个或多个字符。
        - `?`：匹配 1 个字符。
        - `|`：分割多个 Glob。例如，`a.* | b.*` 表示匹配 `a.*` 或 `b.*` 中任意一个。

    - `Regex`：将 `Path Pattern` 作为正则表达式，匹配资产完整路径。
    - `Equals`：资产完整路径与 `Path Pattern` 相等，则匹配成功。
    - `Contains`：资产完整路径包含 `Path Pattern`，则匹配成功。
    - `Starts With`：资产完整路径以 `Path Pattern` 开头，则匹配成功。
    - `Ends With`：资产完整路径以 `Path Pattern` 结尾，则匹配成功。

- `Path Pattern`：模式字符串。
- `Ignore Case`：匹配时是否忽略大小写。
- `Custom Preset`：自定义预设。如果为空则使用默认的预设。
- `Smooth Normal Store Mode`：模型平滑法线的保存方式。
