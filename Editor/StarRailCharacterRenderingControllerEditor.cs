/*
 * StarRailNPRShader - Fan-made shaders for Unity URP attempting to replicate
 * the shading of Honkai: Star Rail.
 * https://github.com/stalomeow/StarRailNPRShader
 *
 * Copyright (C) 2023 Stalo <stalowork@163.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;

namespace HSR.NPRShader.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StarRailCharacterRenderingController))]
    internal class StarRailCharacterRenderingControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty m_RampCoolWarmMix;
        private SerializedProperty m_DitherAlpha;
        private SerializedProperty m_ExCheekIntensity;
        private SerializedProperty m_ExShyIntensity;
        private SerializedProperty m_ExShadowIntensity;
        private SerializedProperty m_IsCastingShadow;

        private SerializedProperty m_MMDHeadBone;
        private SerializedProperty m_MMDHeadBoneForward;
        private SerializedProperty m_MMDHeadBoneUp;
        private SerializedProperty m_MMDHeadBoneRight;

        private AnimBool m_IsExpandedExpression;
        private AnimBool m_IsExpandedMMDHeadBoneSync;

        private void OnEnable()
        {
            m_RampCoolWarmMix = serializedObject.FindProperty(nameof(m_RampCoolWarmMix));
            m_DitherAlpha = serializedObject.FindProperty(nameof(m_DitherAlpha));
            m_ExCheekIntensity = serializedObject.FindProperty(nameof(m_ExCheekIntensity));
            m_ExShyIntensity = serializedObject.FindProperty(nameof(m_ExShyIntensity));
            m_ExShadowIntensity = serializedObject.FindProperty(nameof(m_ExShadowIntensity));
            m_IsCastingShadow = serializedObject.FindProperty(nameof(m_IsCastingShadow));

            m_MMDHeadBone = serializedObject.FindProperty(nameof(m_MMDHeadBone));
            m_MMDHeadBoneForward = serializedObject.FindProperty(nameof(m_MMDHeadBoneForward));
            m_MMDHeadBoneUp = serializedObject.FindProperty(nameof(m_MMDHeadBoneUp));
            m_MMDHeadBoneRight = serializedObject.FindProperty(nameof(m_MMDHeadBoneRight));
        }

        private void OnDisable()
        {
            m_IsExpandedExpression?.valueChanged.RemoveAllListeners();
            m_IsExpandedExpression = null;

            m_IsExpandedMMDHeadBoneSync?.valueChanged.RemoveAllListeners();
            m_IsExpandedMMDHeadBoneSync = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_RampCoolWarmMix, EditorGUIUtility.TrTextContent("Ramp (Cool - Warm)"));
            EditorGUILayout.PropertyField(m_DitherAlpha, EditorGUIUtility.TrTextContent("Dither Alpha"));
            EditorGUILayout.PropertyField(m_IsCastingShadow, EditorGUIUtility.TrTextContent("Cast Shadows"));
            EditorGUILayout.Space();

            SectionGUI("Expression", m_ExCheekIntensity, ref m_IsExpandedExpression, () =>
            {
                EditorGUILayout.PropertyField(m_ExCheekIntensity, EditorGUIUtility.TrTextContent("Cheek"));
                EditorGUILayout.PropertyField(m_ExShyIntensity, EditorGUIUtility.TrTextContent("Shy"));
                EditorGUILayout.PropertyField(m_ExShadowIntensity, EditorGUIUtility.TrTextContent("Shadow"));
            });

            SectionGUI("MMD Head Bone Sync", m_MMDHeadBone, ref m_IsExpandedMMDHeadBoneSync, () =>
            {
                EditorGUILayout.PropertyField(m_MMDHeadBone, EditorGUIUtility.TrTextContent("Head Bone"));
                EditorGUILayout.HelpBox("The head directions of MMD models need to be passed externally, while other models do not require this external input.", MessageType.Info);

                EditorGUILayout.PropertyField(m_MMDHeadBoneForward, EditorGUIUtility.TrTextContent("Forward Vector"));
                EditorGUILayout.PropertyField(m_MMDHeadBoneUp, EditorGUIUtility.TrTextContent("Up Vector"));
                EditorGUILayout.PropertyField(m_MMDHeadBoneRight, EditorGUIUtility.TrTextContent("Right Vector"));
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void SectionGUI(string title, SerializedProperty mainProp, ref AnimBool isExpandedAnim, Action guiAction)
        {
            isExpandedAnim ??= new AnimBool(mainProp.isExpanded, Repaint);

            mainProp.isExpanded = CoreEditorUtils.DrawHeaderFoldout(title, mainProp.isExpanded);
            isExpandedAnim.target = mainProp.isExpanded;

            if (EditorGUILayout.BeginFadeGroup(isExpandedAnim.faded))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    guiAction?.Invoke();
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndFadeGroup();
        }
    }
}
