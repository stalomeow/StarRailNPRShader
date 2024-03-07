# A rough flow chart of this pipeline

The rendering of character takes advantage of MRT whose pass is always executed after UniversalForward. Transparent objects using normal URP Shader and transparent objects on characters are divided into two groups for rendering, which may cause problems.

``` mermaid
flowchart TD
    URPShadowCaster[URP ShadowCaster] --> StarRail1
    
    subgraph StarRail1[StarRailForward]
        PerObjShadowCaster([MainLight PerObjectShadowCaster])
    end

    StarRail1 --> URPDepthPrepass[URP DepthPrepass]
    URPDepthPrepass --> StarRail2

    subgraph StarRail2[StarRailForward]
        ScreenSpaceShadow([Generate ScreenSpaceShadowMap]) --> ScreenSpaceShadowKeyword
        ScreenSpaceShadowKeyword([_MAIN_LIGHT_SHADOWS_SCREEN])
    end

    StarRail2 --> URPOpaque[URP Opaque]
    URPOpaque --> StarRail3

    subgraph StarRail3[StarRailForward]
        CascadedShadow(["_MAIN_LIGHT_SHADOWS_CASCADE"])
        CascadedShadow --> SROpaque

        subgraph SROpaque["Opaque (MRT)"]
            direction LR
            SROpaque1([Opaque 1]) --> SROpaque2([Opaque 2])
            SROpaque2 --> SROpaque3([Opaque 3])
            SROpaque3 --> SROpaqueOutline([Outline])
        end
    end

    StarRail3 --> URPSkybox[URP Skybox]
    URPSkybox --> URPTransparent[URP Transparent]
    URPTransparent --> StarRail4

    subgraph StarRail4[StarRailForward]
        SRTransparent(["Transparent (MRT)"]) --> SRPost
    
        subgraph SRPost[PostProcess]
            direction LR
            SRBloom([Bloom]) --> SRTonemapping([Tonemapping])
        end
    end

    StarRail4 --> URPPost[URP PostProcess]
```
