# 配置一个角色

## 材质

- 在设置 Material 之前，必须先借助资产处理器处理贴图、平滑法线。
- Material 换 Shader 以后记得先 Reset 一下。
- 没有描边/边缘光的话，在 Material 上面调整一下 `Model Scale`。

## 角色渲染控制器

在角色的根物体上添加 `StarRail Character Rendering Controller` 组件。

![角色渲染控制器](../_img/character-rendering-controller.png)

利用该组件可以很方便地控制一些渲染参数。相关的 C# API 如下：

### Properties

|Name|Description|
|:-|:-|
|RampCoolWarmMix|冷暖 Ramp 图的混合程度。0 是冷，1 是暖。范围 $[0, 1]$。|
|DitherAlpha|角色的透明度。范围 $[0, 1]$。|
|ExpressionCheekIntensity|脸颊泛红程度。范围 $[0, 1]$。|
|ExpressionShyIntensity|害羞程度。范围 $[0, 1]$。|
|ExpressionShadowIntensity|黑脸程度。范围 $[0, 1]$。|
|IsCastingShadow|是否投射阴影。|
|PropertyBlock|控制器使用的 `MaterialPropertyBlock`（只读）。|

### Methods

|Name|Description|
|:-|:-|
|UpdateRendererList|更新控制器内部缓存的 `Renderer` 列表。|

## 关于 MMD 模型

需要额外加几个步骤：

- 在材质球上面把 `Model Type` 换成 `MMD`。
- 将头部骨骼的 `Transform` 拖给 `Head Bone` 字段。

    ![同步头骨骼](../_img/mmd-head-bone-sync.png)

**注意：** MMD 模型缺少一些细节信息，所以最后渲染出来可能没有想象中那么好。
