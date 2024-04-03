# A Rough Flow Chart of This Pipeline

``` mermaid
flowchart TD
    URPShadowCaster[URP ShadowCaster] --> StarRail1

    subgraph StarRail1[Honkai Star Rail]
        PerObjShadowCaster([MainLight PerObjectShadowCaster])
    end

    StarRail1 --> URPDepthPrepass[URP DepthPrepass]
    URPDepthPrepass --> StarRail2

    subgraph StarRail2[Honkai Star Rail]
        ScreenSpaceShadow([Generate ScreenSpaceShadowMap]) --> ScreenSpaceShadowKeyword
        ScreenSpaceShadowKeyword([_MAIN_LIGHT_SHADOWS_SCREEN])
    end

    StarRail2 --> URPOpaque[URP Opaque]
    URPOpaque --> StarRail3

    subgraph StarRail3[Honkai Star Rail]
        CascadedShadow(["_MAIN_LIGHT_SHADOWS_CASCADE"])
        CascadedShadow --> SROpaque

        subgraph SROpaque["Opaque"]
            direction LR
            SROpaque1([Opaque 1]) --> SROpaque2([Opaque 2])
            SROpaque2 --> SROpaque3([Opaque 3])
            SROpaque3 --> SROpaqueOutline([Outline])
        end
    end

    StarRail3 --> URPSkybox[URP Skybox]
    URPSkybox --> URPTransparent[URP Transparent]
    URPTransparent --> StarRail4

    subgraph StarRail4[Honkai Star Rail]
        SRTransparent(["Transparent"]) --> SRPost

        subgraph SRPost[PostProcess]
            direction LR
            SRBloom([Bloom]) --> SRTonemapping([Tonemapping])
        end
    end

    StarRail4 --> URPPost[URP PostProcess]
```

Transparent objects using normal URP Shader and transparent objects on characters are divided into two groups for rendering, which may cause problems.
