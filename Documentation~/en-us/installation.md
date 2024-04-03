# Installation

This package is verified on Windows & Android.

## Install via git URL

Install these packages **in order**. The second one requires Unity >= 2022.3, but it is recommended not to use a version that is too high.

1. https://github.com/stalomeow/ShaderUtilsForSRP.git
2. https://github.com/stalomeow/StarRailNPRShader.git

![Install](../_img/install.png)

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
