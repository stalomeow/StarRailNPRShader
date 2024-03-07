# StarRail NPR Shader

<img alt="GitHub Release" src="https://img.shields.io/github/v/release/stalomeow/StarRailNPRShader?style=for-the-badge"> <img alt="GitHub Release Date" src="https://img.shields.io/github/release-date/stalomeow/StarRailNPRShader?style=for-the-badge"> <img alt="GitHub License" src="https://img.shields.io/github/license/stalomeow/StarRailNPRShader?style=for-the-badge">

[English](README.md) | [简体中文](Documentation~/zh-cn/README.md)

Fan-made shaders for Unity URP attempting to replicate the shading of Honkai: Star Rail. The shaders are not 100% accurate because this project is not a reverse engineering - what I do is to replicate the in-game looks to the best of my ability.

![sparkle](Documentation~/_img/sparkle.png)

<p align="center">↑↑↑ Sparkle ↑↑↑</p>

![firefly](Documentation~/_img/firefly.png)

<p align="center">↑↑↑ Firefly ↑↑↑</p>

## Features

### Character shaders

- Honkai Star Rail/Character/Body
- Honkai Star Rail/Character/Body (Transparent)
- Honkai Star Rail/Character/EyeShadow
- Honkai Star Rail/Character/Face
- Honkai Star Rail/Character/FaceMask
- Honkai Star Rail/Character/Hair

### Rendering

- Both Game model and MMD model.
- Both `Forward` and `Forward+` rendering paths.
- A single `RendererFeature` to manage all custom passes.
- Provide C# API to control some rendering behavior.
- Characters receive only scene shadows and ignore self-shadows.
- Per-object shadow, supporting up to 16 shadows on the same screen.
- Custom bloom using the method shared by Jack He in Unite 2018.
- Custom ACES tonemapping. The formula is

    $$f(x)=\frac{x(ax+b)}{x(cx+d)+e}$$

    where $a,b,c,d,e$ are all parameters.

### Editor

- Customized material editor.
- Configurable asset processor integrated with Unity preset system.
- Automatically smooth normal.
- Automatic material setup.
- Build processor and Shader stripper.
- `material.json` inspector.

## Documentation

It is recommended to read them in order.

- [Installation](Documentation~/en-us/installation.md)
- [A rough flow chart of this pipeline](Documentation~/en-us/a-rough-flow-chart-of-this-pipeline.md)
- [Working with asset processor](Documentation~/en-us/working-with-asset-processor.md)
- [Setup a character](Documentation~/en-us/setup-a-character.md)
- [Automatic material setup](Documentation~/en-us/automatic-material-setup.md)

## Special thanks

- miHoYo / HoYoverse
- Razmoth
- °Nya°222
