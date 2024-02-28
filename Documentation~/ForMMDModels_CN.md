# 关于 MMD 模型

需要额外加几个步骤：

- 在材质球上面把 `Model Type` 换成 `MMD`。
- 在挂有 SkinnedMeshRenderer 的游戏物体上加一个 `SyncMMDHeadBone` 组件。

    ![sync-mmd-head-bone](../Screenshots~/_sync_mmd_head_bone.png)

    可以自行设置头骨骼方向的值。另外，组件的菜单里还提供了两个预设。

    ![sync-mmd-head-bone-ex](../Screenshots~/_sync_mmd_head_bone_ex.png)

**注意：** MMD 模型缺少一些细节信息，所以最后渲染出来可能没有想象中那么好。
