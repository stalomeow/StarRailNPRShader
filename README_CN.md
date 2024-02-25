# StarRailNPRShader

> [!IMPORTANT]
> 求求了，看看 README 吧！

这是基于 Unity URP 的仿星穹铁道渲染 Shader。这不是逆向工程，Shader 代码不可能和游戏里的一模一样，我只是尽力去还原渲染效果。

![花火](Screenshots~/sparkle.png)

<p align="center">↑↑↑ 花火 ↑↑↑</p>

![流萤](Screenshots~/firefly.png)

<p align="center">↑↑↑ 流萤 ↑↑↑</p>

## 角色着色器

- Honkai Star Rail/Character/Body
- Honkai Star Rail/Character/Body (Transparent)
- Honkai Star Rail/Character/EyeShadow
- Honkai Star Rail/Character/Face
- Honkai Star Rail/Character/FaceMask
- Honkai Star Rail/Character/Hair

角色渲染用了 MRT，这个 MRT Pass 是在 URP 的 Forward Pass 之后执行的。使用普通 URP Shader 的透明物体和角色身上的透明物体被分成了两批渲染，可能会出问题。

## 屏幕后处理

- 自定义 Bloom 效果。用的贺甲在 Unite 2018 上分享的方法。
- 自定义 ACES tonemapping。公式是

    $$f(x)=\frac{x(ax+b)}{x(cx+d)+e}$$

    其中 $a,b,c,d,e$ 都是参数。

## 从 git URL 安装

**这个包要求 Unity >= 2022.3。**

![安装](Screenshots~/_install.png)

1. https://github.com/stalomeow/ShaderUtilsForSRP.git
2. https://github.com/stalomeow/StarRailNPRShader.git

## 指南

- 用 linear color space，别用 gamma。
- 开 HDR。
- 关 Depth priming。
- 开 Depth texture 和 Depth prepass。
- 目前只能用 Forward 渲染路径。
- 在 Renderer 上加 Renderer Feature `StarRailForward`。
- 材质球换 Shader 以后记得先重置一下。
- 该项目自己实现了屏幕空间阴影，请不要添加 URP 的 `ScreenSpaceShadows` RendererFeature。

### 推荐的后处理设置

![后处理设置](Screenshots~/_postprocessing.png)

### 逐物体阴影

在角色根物体上添加 `PerObjectShadowCaster` 组件。支持同屏最多 16 个角色阴影。

![逐物体阴影](Screenshots~/_per_obj_shadow.png)

### 使用资源预处理器

资源预处理器能

- 自动平滑角色模型的法线，然后存进切线里。
- 自动处理贴图。

可以在 `Project Settings/Honkai Star Rail/NPR Shader` 中配置需要被预处理的资源的路径模式。默认的路径模式旨在与游戏内资源的命名风格保持一致。

![资源路径模式设置](Screenshots~/_asset_path_patterns.png)

### 使用 HSRMaterialViewer

HSRMaterialViewer 能帮你浏览 `material.json` 文件，以及自动赋值材质的部分属性（不是所有属性）。**这个工具对 Floats 和 Ints 的赋值支持得不好。**

![hsr-mat-viewer](Screenshots~/_hsr_mat_viewer.gif)

### 关于 MMD 模型

需要额外加几个步骤：

- 在材质球上面把 `Model Type` 换成 `MMD`。
- 在挂有 SkinnedMeshRenderer 的游戏物体上加一个 `SyncMMDHeadBone` 组件。

    ![sync-mmd-head-bone](Screenshots~/_sync_mmd_head_bone.png)

    可以自行设置头骨骼方向的值。另外，组件的菜单里还提供了两个预设。

    ![sync-mmd-head-bone-ex](Screenshots~/_sync_mmd_head_bone_ex.png)

**注意：** MMD 模型缺少一些细节信息，所以最后渲染出来可能没有想象中那么好。

## 规则

当使用或者再发行我的代码时，除了遵守 GPL-3.0 协议，请提供这个代码仓库的链接并注明原作者。

## 特别感谢

- 米哈游
- 知乎上相关的文章
- 哔哩哔哩上相关的视频
- °Nya°222

## 常见问题

### 为啥没有描边/边缘光？

在材质球上面修改 `Model Scale`。