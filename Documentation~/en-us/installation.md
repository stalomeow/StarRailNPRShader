# Installation

This package requires **Unity >= 2022.3** and is verified on Windows & Android. Since the API of SRP & URP changes frequently, it is recommended **not to use a version that is too high**.

## Install via git URL

1. Select `Add package from git URL...` from the add menu of Package Manager.

    ![Install](../_img/install.png)

2. Enter `https://github.com/stalomeow/StarRailNPRShader.git` in the text box.
3. Select `Add`.

## RenderPipeline Settings

- Use linear color space instead of gamma.
- Use `Forward` or `Forward+` rendering path.
- Disable depth priming.
- Add `Honkai Star Rail` RendererFeature to the renderer.

## Recommended Post-processing Settings

Post-processing is important; be sure to add it.

![Post-processing settings](../_img/postprocessing.png)

## Other Tips

- It is recommended to turn on HDR.
- This project implements its own screen space shadows. Please do not add the `ScreenSpaceShadows` RendererFeature of URP again.
