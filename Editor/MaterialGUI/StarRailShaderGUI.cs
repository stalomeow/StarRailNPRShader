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

// #define SHADER_GUI_LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using HSR.NPRShader.Editor;
using HSR.NPRShader.Editor.MaterialGUI;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

// To make the full class name shorter, DO NOT add a namespace here.
// Make it internal so it won't pollute the global namespace.

internal class StarRailShaderGUI : ShaderGUI
{
    private static readonly Lazy<GUIStyle> s_ShaderHeaderLabelStyle = new(() =>
    {
        return new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
    });

    // Record the last ShaderGUI object for the material
    private static readonly Dictionary<Material, StarRailShaderGUI> s_LastShaderGUI = new();

    private List<PropertyGroup> m_PropGroups = null;
    private Dictionary<uint, AnimBool> m_ExpandStates = new();
    private SearchField m_Search = new();
    private string m_SearchText = "";
    private Shader m_LastShader;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        Material[] materials = Array.ConvertAll(editor.targets, obj => (Material)obj);
        Array.ForEach(materials, material => s_LastShaderGUI[material] = this); // Record ShaderGUI

        Shader shader = materials[0].shader;
        m_LastShader = shader; // Record Shader
        UpdatePropertyGroups(ref m_PropGroups, shader, properties);

        editor.SetDefaultGUIWidths();

        using (new GUILayout.VerticalScope())
        {
            // Shader Header
            GUILayout.Space(5);
            EditorGUILayout.LabelField(shader.name, s_ShaderHeaderLabelStyle.Value);

            // Search Field
            GUILayout.Space(5);
            m_SearchText = m_Search.OnGUI(m_SearchText);
            GUILayout.Space(5);

            // Property Groups
            // 对齐 Slider 右侧输入框和其他输入框的宽度
            using (new MemberValueScope<float>(() => EditorGUIUtility.labelWidth, width => width - 5f))
            {
                foreach (PropertyGroup group in m_PropGroups)
                {
                    group.OnGUI(editor, m_ExpandStates, m_SearchText);
                }
            }

            // Extra Properties
            CoreEditorUtils.DrawSplitter();
            GUILayout.Space(5);
        }

        if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
        {
            editor.RenderQueueField();
        }

        editor.EnableInstancingField();
        editor.DoubleSidedGIField();
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        m_PropGroups = null;
        m_ExpandStates.Clear();
        m_SearchText = string.Empty;
    }

    public override void OnClosed(Material material)
    {
        if (s_LastShaderGUI.TryGetValue(material, out var shaderGUI) && shaderGUI == this)
        {
            s_LastShaderGUI.Remove(material);
        }

        base.OnClosed(material);
    }

    public override void ValidateMaterial(Material material)
    {
        base.ValidateMaterial(material);

        // !!! When user undoes an action, Unity creates a new instance to call this method.
        // !!! That instance is not the one actually drawing the inspector.
        // !!! We should rebuild all groups to sync external changes.

        if (s_LastShaderGUI.TryGetValue(material, out var shaderGUI) && shaderGUI != this)
        {
            shaderGUI.m_PropGroups = null;

            if (shaderGUI.m_LastShader != material.shader)
            {
                // The user possibly undoes a shader-assignment.

                shaderGUI.m_ExpandStates.Clear();
                shaderGUI.m_SearchText = string.Empty;
            }
        }
    }

    private class PropertyGroup
    {
        private readonly GUIContent m_Title;
        private readonly string m_Helps;
        private readonly uint m_BitExpanded;
        private readonly List<MaterialProperty> m_Properties;
        private readonly List<List<MaterialPropertyWrapper>> m_PropertyWrappers;

        public PropertyGroup() : this(null, null, 0) { }

        public PropertyGroup(string title, string helps, uint bitExpanded)
        {
            m_Title = new GUIContent(title, helps ?? string.Empty);
            m_Helps = helps;
            m_BitExpanded = bitExpanded;
            m_Properties = new List<MaterialProperty>();
            m_PropertyWrappers = new List<List<MaterialPropertyWrapper>>();
        }

        public bool IsDefaultGroup => string.IsNullOrWhiteSpace(m_Title.text) || (m_BitExpanded == 0);

        public int PropertyCount => m_Properties.Count;

        public bool TryAddProperty(MaterialProperty property, List<MaterialPropertyWrapper> wrappers)
        {
            if ((property.flags & MaterialProperty.PropFlags.HideInInspector) != 0)
            {
                // 属性被隐藏
                return false;
            }

            m_Properties.Add(property);
            m_PropertyWrappers.Add(wrappers);
            return true;
        }

        public void ReloadProperties(IDictionary<string, MaterialProperty> propMap)
        {
            for (int i = 0; i < m_Properties.Count; i++)
            {
                m_Properties[i] = propMap[m_Properties[i].name];
            }
        }

        public bool OnGUI(
            MaterialEditor editor,
            IDictionary<uint, AnimBool> expandStates,
            string searchText,
            StringComparison searchComparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            Action propDrawer = CreateFilteredPropertyDrawer(editor, searchText, searchComparisonType);

            if (propDrawer == null)
            {
                // 没有属性需要绘制
                return false;
            }

            if (IsDefaultGroup)
            {
                propDrawer();
                GUILayout.Space(10);
                return true;
            }

            int outerIndentLevel = EditorGUI.indentLevel;

            using (MaterialHeaderScope headerScope = new(m_Title, m_BitExpanded, editor, spaceAtEnd: false))
            {
                // FIX: 低版本 SRP 的 MaterialHeaderScope 会在展开后增加缩进。
                using (new MemberValueScope<int>(() => EditorGUI.indentLevel, outerIndentLevel))
                {
                    if (expandStates.TryGetValue(m_BitExpanded, out AnimBool isExpanded))
                    {
                        isExpanded.target = headerScope.expanded;
                    }
                    else
                    {
                        isExpanded = new AnimBool(headerScope.expanded);
                        expandStates.Add(m_BitExpanded, isExpanded);
                    }

                    isExpanded.valueChanged.RemoveAllListeners();
                    isExpanded.valueChanged.AddListener(editor.Repaint); // 变化后需要重新绘制，不然动画很卡

                    if (EditorGUILayout.BeginFadeGroup(isExpanded.faded))
                    {
                        GUILayout.Space(5);

                        if (!string.IsNullOrEmpty(m_Helps))
                        {
                            EditorGUILayout.HelpBox(m_Helps, MessageType.None);
                            GUILayout.Space(5);
                        }

                        propDrawer();
                        GUILayout.Space(10);
                    }

                    EditorGUILayout.EndFadeGroup();
                }
            }

            return true;
        }

        private Action CreateFilteredPropertyDrawer(
            MaterialEditor editor,
            string searchText,
            StringComparison searchComparisonType)
        {
            bool enableSearch = !string.IsNullOrWhiteSpace(searchText);
            List<(MaterialProperty prop, float height, List<MaterialPropertyWrapper> wrappers)> validProps = new();

            for (int i = 0; i < m_Properties.Count; i++)
            {
                MaterialProperty prop = m_Properties[i];
                List<MaterialPropertyWrapper> wrappers = m_PropertyWrappers[i];

                if (wrappers.Any(wrapper => !wrapper.CanDrawProperty(prop, prop.displayName, editor)))
                {
                    continue;
                }

                float height = editor.GetPropertyHeight(prop, prop.displayName);

                if (height <= 0)
                {
                    // 属性没有高度
                    // 由于 GetControlRect 返回的 rect 的高度有一个最小值，所以需要提前剔除高度为 0 的属性
                    continue;
                }

                if (enableSearch && !prop.displayName.Contains(searchText, searchComparisonType))
                {
                    // 属性不符合搜索
                    continue;
                }

                validProps.Add((prop, height, wrappers));
            }

            return validProps.Count == 0 ? null : () =>
            {
                foreach ((MaterialProperty prop, float height, List<MaterialPropertyWrapper> wrappers) in validProps)
                {
                    // Hook
                    for (int i = 0; i < wrappers.Count; i++)
                    {
                        wrappers[i].OnWillDrawProperty(prop, prop.displayName, editor);
                    }

                    // Draw
                    Rect rect = EditorGUILayout.GetControlRect(true, height, EditorStyles.layerMaskField);
                    editor.ShaderProperty(rect, prop, prop.displayName);

                    // Hook
                    for (int i = wrappers.Count - 1; i >= 0; i--)
                    {
                        wrappers[i].OnDidDrawProperty(prop, prop.displayName, editor);
                    }
                }
            };
        }
    }

    private static void UpdatePropertyGroups(
        ref List<PropertyGroup> groups,
        Shader shader,
        MaterialProperty[] properties)
    {
        if (groups != null)
        {
            Dictionary<string, MaterialProperty> propMap = properties.ToDictionary(
                prop => prop.name,
                prop => prop);
            groups.ForEach(group => group.ReloadProperties(propMap));
            return;
        }

        groups = new List<PropertyGroup>() { new PropertyGroup() }; // With One Default Group

        for (var i = 0; i < properties.Length; i++)
        {
            ParseCustomAttributes(shader, i, out string headerTitle, out string headerHelps,
                out List<MaterialPropertyWrapper> propWrappers);

            if (!string.IsNullOrWhiteSpace(headerTitle))
            {
                // 创建新的组
                uint bitExpanded = 1u << (groups.Count - 1);
                groups.Add(new PropertyGroup(headerTitle, headerHelps, bitExpanded));
            }

            groups[^1].TryAddProperty(properties[i], propWrappers);
        }

        // 必须在最后移除空的组，保证前面 bitExpanded 的正确性
        groups.RemoveAll(g => g.PropertyCount == 0);
        LogToConsole("Rebuild Groups");
    }

    public static readonly string HeaderFoldoutAttrName = "HeaderFoldout";
    private static readonly Dictionary<string, Type> s_MatPropWrapperTypes = new();

    static StarRailShaderGUI()
    {
        Type[] wrapperCtorArgTypes = { typeof(string) };

        foreach (Type type in TypeCache.GetTypesDerivedFrom<MaterialPropertyWrapper>())
        {
            if (type.IsGenericType || type.IsAbstract || type.GetConstructor(wrapperCtorArgTypes) == null)
            {
                continue;
            }

            s_MatPropWrapperTypes[type.Name] = type;

            if (type.Name.Length > 7 && type.Name.EndsWith("Wrapper"))
            {
                s_MatPropWrapperTypes[type.Name[..^7]] = type;
            }
        }

        LogToConsole("Found Wrappers:", string.Join('\n', s_MatPropWrapperTypes.Keys));
    }

    private static void ParseCustomAttributes(
        Shader shader,
        int propertyIndex,
        out string headerTitle,
        out string headerHelps,
        out List<MaterialPropertyWrapper> propWrappers)
    {
        headerTitle = null;
        headerHelps = null;
        propWrappers = new List<MaterialPropertyWrapper>();

        foreach (string attr in shader.GetPropertyAttributes(propertyIndex))
        {
            Match match = Regex.Match(attr, @"^(.+?)\((.+)\)$");

            (string attrName, string rawArgs) = match.Success
                ? (match.Groups[1].Value, match.Groups[2].Value)
                : (attr, string.Empty);

            if (attrName == HeaderFoldoutAttrName)
            {
                string[] args = rawArgs.Split(',');

                if (args.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim();
                }

                headerTitle = args[0];
                headerHelps = args.Length > 1 ? string.Join(", ", args, 1, args.Length - 1) : null;
            }
            else if (s_MatPropWrapperTypes.TryGetValue(attrName, out Type wrapperType))
            {
                object wrapper = Activator.CreateInstance(wrapperType, rawArgs);
                propWrappers.Add(wrapper as MaterialPropertyWrapper);
            }
        }
    }

    [Conditional("SHADER_GUI_LOG")]
    private static void LogToConsole(params object[] messages)
    {
        Debug.LogWarning(string.Join('\n', messages));
    }
}
