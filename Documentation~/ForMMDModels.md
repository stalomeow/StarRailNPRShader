# For MMD models

Some extra steps must be done:

- Switch `Model Type` to `MMD` on each material.
- Add component `SyncMMDHeadBone` to the GameObject to which SkinnedMeshRenderer is attached.

    ![sync-mmd-head-bone](../Screenshots~/_sync_mmd_head_bone.png)

    You can override the direction values of the head bone. Besides, two presets are provided in the context menu of the component.

    ![sync-mmd-head-bone-ex](../Screenshots~/_sync_mmd_head_bone_ex.png)

**Note that** MMD models do not contain some detailed information so the final rendering result may not fully meet your expectation.
