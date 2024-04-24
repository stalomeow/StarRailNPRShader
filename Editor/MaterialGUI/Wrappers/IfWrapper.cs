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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;

namespace HSR.NPRShader.Editor.MaterialGUI.Wrappers
{
    internal class IfWrapper : MaterialPropertyWrapper
    {
        private readonly IConditionNode m_Condition;

        public IfWrapper(string rawArgs) : base(rawArgs)
        {
            m_Condition = ParseCondition(rawArgs);
        }

        public override bool CanDrawProperty(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return editor.targets.Cast<Material>().Any(material => m_Condition.Evaluate(material));
        }

        private static IConditionNode ParseCondition(string arg)
        {
            List<Token> tokens = Lexer.Shared.Tokenize(arg);
            // Debug.Log(string.Join(", ", tokens.Select(t => $"'{t.Raw}'")));
            IConditionNode node = Parser.Shared.Parse(tokens);
            // Debug.Log(node);
            return node;
        }

        private interface IConditionNode
        {
            bool Evaluate(Material material);
        }

        private class TrueNode : IConditionNode
        {
            public bool Evaluate(Material material)
            {
                return true;
            }

            public override string ToString()
            {
                return "true";
            }
        }

        private class FalseNode : IConditionNode
        {
            public bool Evaluate(Material material)
            {
                return false;
            }

            public override string ToString()
            {
                return "false";
            }
        }

        private class KeywordNode : IConditionNode
        {
            private readonly string m_Keyword;

            public KeywordNode(string keyword)
            {
                m_Keyword = keyword;
            }

            public bool Evaluate(Material material)
            {
                return material.IsKeywordEnabled(m_Keyword);
            }

            public override string ToString()
            {
                return m_Keyword;
            }
        }

        private class Op_NotNode : IConditionNode
        {
            private readonly IConditionNode m_Node;

            public Op_NotNode(IConditionNode node)
            {
                m_Node = node;
            }

            public bool Evaluate(Material material)
            {
                return !m_Node.Evaluate(material);
            }

            public override string ToString()
            {
                return $"!({m_Node})";
            }
        }

        private class Op_AndNode : IConditionNode
        {
            private readonly IConditionNode m_LeftNode;
            private readonly IConditionNode m_RightNode;

            public Op_AndNode(IConditionNode leftNode, IConditionNode rightNode)
            {
                m_LeftNode = leftNode;
                m_RightNode = rightNode;
            }

            public bool Evaluate(Material material)
            {
                return m_LeftNode.Evaluate(material) && m_RightNode.Evaluate(material);
            }

            public override string ToString()
            {
                return $"({m_LeftNode}) && ({m_RightNode})";
            }
        }

        private class Op_OrNode : IConditionNode
        {
            private readonly IConditionNode m_LeftNode;
            private readonly IConditionNode m_RightNode;

            public Op_OrNode(IConditionNode leftNode, IConditionNode rightNode)
            {
                m_LeftNode = leftNode;
                m_RightNode = rightNode;
            }

            public bool Evaluate(Material material)
            {
                return m_LeftNode.Evaluate(material) || m_RightNode.Evaluate(material);
            }

            public override string ToString()
            {
                return $"({m_LeftNode}) || ({m_RightNode})";
            }
        }

        [Flags]
        private enum TokenType
        {
            Keyword = 1 << 0,
            Not = 1 << 1,
            And = 1 << 2,
            Or = 1 << 3,
            LeftParentheses = 1 << 4,
            RightParentheses = 1 << 5,
            True = 1 << 6,
            False = 1 << 7
        }

        private readonly struct Token
        {
            public readonly string Raw;
            public readonly TokenType Type;
            public readonly string SrcText;
            public readonly int Column;

            public Token(string raw, TokenType type, string srcText, int column)
            {
                Raw = raw;
                Type = type;
                SrcText = srcText;
                Column = column;
            }
        }

        private class InvalidCharacterException : Exception
        {
            public InvalidCharacterException(string text, int pos)
                : base($"Invalid character in '{text}' (column {pos + 1}).") { }
        }

        private class Lexer
        {
            [ThreadStatic] private static Lexer s_Shared;

            public static Lexer Shared => s_Shared ??= new Lexer();

            private readonly StringBuilder m_Cache = new();
            private int m_CurrentPos;
            private string m_SrcText;
            private string m_Text;
            private bool m_IsIdle;

            private char GetCurrentChar()
            {
                return m_Text[m_CurrentPos];
            }

            private Token CreateToken(string raw, TokenType tokenType, int posFirstChar)
            {
                return new Token(raw, tokenType, m_SrcText, posFirstChar + 1);
            }

            public List<Token> Tokenize(string text)
            {
                // Initialize
                m_Cache.Clear();
                m_CurrentPos = 0;
                m_SrcText = text;
                m_Text = text + ' '; // Add a white space to do cleanup
                m_IsIdle = true;

                // Process
                var results = new List<Token>();

                while (m_CurrentPos < m_Text.Length)
                {
                    if (m_IsIdle)
                    {
                        ProcessIdle(results);
                    }
                    else
                    {
                        ProcessWord(results);
                    }
                }

                return results;
            }

            private void ProcessIdle(List<Token> results)
            {
                m_Cache.Clear();
                char c = GetCurrentChar();

                if (char.IsWhiteSpace(c))
                {
                    m_CurrentPos++;
                    return;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    m_IsIdle = false;
                    return;
                }

                switch (c)
                {
                    case '(':
                        results.Add(CreateToken("(", TokenType.LeftParentheses, m_CurrentPos));
                        m_CurrentPos++;
                        break;

                    case ')':
                        results.Add(CreateToken(")", TokenType.RightParentheses, m_CurrentPos));
                        m_CurrentPos++;
                        break;

                    default:
                        throw new InvalidCharacterException(m_SrcText, m_CurrentPos);
                }
            }

            private void ProcessWord(List<Token> results)
            {
                char c = GetCurrentChar();

                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    m_Cache.Append(c);
                    m_CurrentPos++;
                    return;
                }

                string word = m_Cache.ToString();
                TokenType tokenType = word switch
                {
                    "not" => TokenType.Not,
                    "and" => TokenType.And,
                    "or" => TokenType.Or,
                    "true" => TokenType.True,
                    "false" => TokenType.False,
                    _ => TokenType.Keyword
                };
                int posFirstChar = m_CurrentPos - word.Length;
                results.Add(CreateToken(word, tokenType, posFirstChar));

                m_IsIdle = true;
            }
        }

        private class ExpectTokenException : Exception
        {
            public ExpectTokenException(TokenType expectTokenType)
                : base($"Expect token '{expectTokenType}' but nothing was found.") { }

            public ExpectTokenException(TokenType expectTokenType, Token token)
                : base($"Expect token '{expectTokenType}' in '{token.SrcText}' (column {token.Column}) but '{token.Type}' was found.") { }
        }

        private struct BinaryOperator
        {
            public readonly TokenType TokenType;
            public readonly uint Precedence;
            public readonly Func<IConditionNode, IConditionNode, IConditionNode> Combine; // Can Be Null

            public BinaryOperator(
                TokenType tokenType,
                uint precedence,
                Func<IConditionNode, IConditionNode, IConditionNode> combine)
            {
                TokenType = tokenType;
                Precedence = precedence;
                Combine = combine;
            }

            public static readonly BinaryOperator And = new(TokenType.And, 2,
                (left, right) => new Op_AndNode(left, right));

            public static readonly BinaryOperator Or = new(TokenType.Or, 1,
                (left, right) => new Op_OrNode(left, right));

            public static readonly BinaryOperator EOF = new(0, 0, null);
        }

        private class Parser
        {
            // condition
            //   : keyword_token
            //   | 'true'
            //   | 'false'
            //   ;

            // parentheses_expr
            //   : '(' expr ')'
            //   ;

            // not_expr
            //   : 'not' condition
            //   | 'not' parentheses_expr
            //   | 'not' not_expr
            //   ;

            // expr
            //   : condition
            //   | parentheses_expr
            //   | not_expr
            //   | expr 'and' expr
            //   | expr 'or' expr
            //   ;

            [ThreadStatic] private static Parser s_Shared;

            public static Parser Shared => s_Shared ??= new Parser();

            private readonly Func<IConditionNode>[] m_NotExprSubParsers;
            private readonly Action<Stack<IConditionNode>>[] m_ExprSubParsers;
            private readonly BinaryOperator[] m_BinaryOperators;

            public Parser()
            {
                m_NotExprSubParsers = new Func<IConditionNode>[]
                {
                    ParseNotExpr1,
                    ParseNotExpr2,
                    ParseNotExpr3,
                };

                m_ExprSubParsers = new Action<Stack<IConditionNode>>[]
                {
                    ParseExpr1,
                    ParseExpr2,
                    ParseExpr3,
                };

                m_BinaryOperators = new BinaryOperator[]
                {
                    BinaryOperator.And,
                    BinaryOperator.Or
                };
            }

            private int m_CurrentPos;
            private List<Token> m_Tokens;

            public IConditionNode Parse(List<Token> tokens)
            {
                if (tokens.Count == 0)
                {
                    return new FalseNode();
                }

                // Initialize
                m_CurrentPos = 0;
                m_Tokens = tokens;

                // Parse
                try
                {
                    IConditionNode node = ParseExpr();
                    Assert.IsFalse(m_CurrentPos < m_Tokens.Count);
                    return node;
                }
                catch
                {
                    Debug.LogError($"Can not parse expression '{tokens[0].SrcText}'.");
                    throw;
                }
            }

            private Token ExpectToken(TokenType tokenType, bool eat = true)
            {
                if (m_CurrentPos >= m_Tokens.Count)
                {
                    throw new ExpectTokenException(tokenType);
                }

                Token token = m_Tokens[m_CurrentPos];

                if ((token.Type & tokenType) == 0)
                {
                    throw new ExpectTokenException(tokenType, token);
                }

                if (eat)
                {
                    m_CurrentPos++;
                }

                return token;
            }

            private IConditionNode ParseCondition()
            {
                Token token = ExpectToken(TokenType.Keyword | TokenType.True | TokenType.False);
                return token.Type switch
                {
                    TokenType.Keyword => new KeywordNode(token.Raw),
                    TokenType.True => new TrueNode(),
                    TokenType.False => new FalseNode(),
                    _ => null // Unreachable
                };
            }

            private IConditionNode ParseParenthesesExpr()
            {
                ExpectToken(TokenType.LeftParentheses);
                IConditionNode node = ParseExpr();
                ExpectToken(TokenType.RightParentheses);
                return node;
            }

            private IConditionNode ParseNotExpr()
            {
                Exception lastException = null;

                foreach (Func<IConditionNode> subParser in m_NotExprSubParsers)
                {
                    int currentPos = m_CurrentPos;

                    try
                    {
                        return subParser();
                    }
                    catch (ExpectTokenException e)
                    {
                        m_CurrentPos = currentPos;
                        lastException = e;
                    }
                }

                throw lastException ?? new Exception("Unknown error. (This is likely a bug of the parser)");
            }

            private IConditionNode ParseNotExpr1()
            {
                ExpectToken(TokenType.Not);
                IConditionNode node = ParseCondition();
                return new Op_NotNode(node);
            }

            private IConditionNode ParseNotExpr2()
            {
                ExpectToken(TokenType.Not);
                IConditionNode node = ParseParenthesesExpr();
                return new Op_NotNode(node);
            }

            private IConditionNode ParseNotExpr3()
            {
                ExpectToken(TokenType.Not);
                IConditionNode node = ParseNotExpr();
                return new Op_NotNode(node);
            }

            private IConditionNode ParseExpr(
                Stack<IConditionNode> nodeStack = null,
                Stack<BinaryOperator> opStack = null)
            {
                nodeStack ??= new Stack<IConditionNode>();
                opStack ??= new Stack<BinaryOperator>();

                Exception lastException = null;

                foreach (Action<Stack<IConditionNode>> subParser in m_ExprSubParsers)
                {
                    int currentPos = m_CurrentPos;

                    try
                    {
                        subParser(nodeStack);
                        ParseExprLR(nodeStack, opStack);
                        ReduceOperators(nodeStack, opStack, BinaryOperator.EOF);

                        Assert.IsTrue(nodeStack.Count == 1);
                        Assert.IsTrue(opStack.Count == 0);

                        return nodeStack.Pop();
                    }
                    catch (ExpectTokenException e)
                    {
                        m_CurrentPos = currentPos;
                        lastException = e;
                    }
                }

                throw lastException ?? new Exception("Unknown error. (This is likely a bug of the parser)");
            }

            private void ParseExpr1(Stack<IConditionNode> nodeStack)
            {
                IConditionNode node = ParseCondition();
                nodeStack.Push(node);
            }

            private void ParseExpr2(Stack<IConditionNode> nodeStack)
            {
                IConditionNode node = ParseParenthesesExpr();
                nodeStack.Push(node);
            }

            private void ParseExpr3(Stack<IConditionNode> nodeStack)
            {
                IConditionNode node = ParseNotExpr();
                nodeStack.Push(node);
            }

            private void ParseExprLR(Stack<IConditionNode> nodeStack, Stack<BinaryOperator> opStack)
            {
                foreach (BinaryOperator binaryOp in m_BinaryOperators)
                {
                    int currentPos = m_CurrentPos;

                    try
                    {
                        ExpectToken(binaryOp.TokenType);
                        ReduceOperators(nodeStack, opStack, binaryOp);

                        opStack.Push(binaryOp);
                        nodeStack.Push(ParseExpr(nodeStack, opStack));
                        break;
                    }
                    catch (ExpectTokenException)
                    {
                        m_CurrentPos = currentPos;
                    }
                }
            }

            private static void ReduceOperators(Stack<IConditionNode> nodeStack, Stack<BinaryOperator> opStack, BinaryOperator nextOp)
            {
                while (opStack.TryPeek(out BinaryOperator lastOp))
                {
                    if (lastOp.Precedence <= nextOp.Precedence)
                    {
                        break;
                    }

                    opStack.Pop();

                    IConditionNode rightNode = nodeStack.Pop();
                    IConditionNode leftNode = nodeStack.Pop();

                    if (lastOp.Combine != null)
                    {
                        nodeStack.Push(lastOp.Combine(leftNode, rightNode));
                    }
                }
            }
        }
    }
}
