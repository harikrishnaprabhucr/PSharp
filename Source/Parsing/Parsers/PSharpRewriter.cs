﻿//-----------------------------------------------------------------------
// <copyright file="PSharpRewriter.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# rewriter.
    /// </summary>
    internal class PSharpRewriter : BaseParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public PSharpRewriter(List<Token> tokens)
            : base(tokens)
        {
            
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            var token = base.Tokens[base.Index];
            if (token.Type == TokenType.EventDecl)
            {
                this.RewriteEventDeclaration();
            }
            else if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineDeclaration();
            }
            else if (token.Type == TokenType.StateDecl)
            {
                this.RewriteStateDeclaration();
            }
            else if (token.Type == TokenType.OnAction)
            {
                this.RewriteStateActionDeclaration();
            }
            else if (token.Type == TokenType.ActionDecl)
            {
                this.RewriteActionDeclaration();
            }
            else if (token.Type == TokenType.RaiseEvent)
            {
                this.RewriteRaiseStatement();
            }
            else if (token.Type == TokenType.DeleteMachine)
            {
                this.RewriteDeleteStatement();
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the event declaration.
        /// </summary>
        private void RewriteEventDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                throw new ParsingException("parser: identifier expected.");
            }

            var identifier = base.Tokens[base.Index].String;

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token("Event"));
            }
            else
            {
                throw new ParsingException("parser: semicolon expected.");
            }

            base.Index = replaceIdx;
            base.Index++;

            var eventBody = "\n";
            eventBody += "\t{\n";
            eventBody += "\t\tpublic " + identifier + "()\n";
            eventBody += "\t\t\t: base()\n";
            eventBody += "\t\t{ }\n";
            eventBody += "\n";
            eventBody += "\t\tpublic " + identifier + "(Object payload)\n";
            eventBody += "\t\t\t: base(payload)\n";
            eventBody += "\t\t{ }\n";
            eventBody += "\t}";

            base.Tokens.Insert(base.Index, new Token(eventBody));
        }

        /// <summary>
        /// Rewrites the machine declaration.
        /// </summary>
        private void RewriteMachineDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.None)
            {
                base.CurrentMachine = base.Tokens[base.Index].String;
            }
            else
            {
                throw new ParsingException("parser: machine identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.MachineLeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("Machine"));

                base.Index = replaceIdx;
                base.Index++;
            }
            else if (base.Tokens[base.Index].Type != TokenType.Doublecolon)
            {
                throw new ParsingException("parser: doublecolon expected.");
            }
        }

        /// <summary>
        /// Rewrites the state declaration.
        /// </summary>
        private void RewriteStateDeclaration()
        {
            base.Tokens[base.Index] = new Token("class", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.None)
            {
                base.CurrentState = base.Tokens[base.Index].String;
            }
            else
            {
                throw new ParsingException("parser: state identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(":", TokenType.Doublecolon));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
                replaceIdx++;

                this.Tokens.Insert(replaceIdx, new Token("State"));

                base.Index = replaceIdx;
                base.Index++;
            }
            else
            {
                throw new ParsingException("parser: left curly bracket expected.");
            }
        }

        /// <summary>
        /// Rewrites the state action declaration.
        /// </summary>
        private void RewriteStateActionDeclaration()
        {
            var type = GetActionType();
            if (type == ActionType.OnEntry || type == ActionType.OnExit)
            {
                base.Tokens[base.Index] = new Token("protected", TokenType.Private);
                base.Index++;

                base.SkipWhiteSpaceTokens();
                this.RewriteOnActionDeclaration(base.Tokens[base.Index].Type);
            }
            else if (type == ActionType.None)
            {
                throw new ParsingException("parser: no action type identified.");
            }
        }

        /// <summary>
        /// Rewrites the on action declaration.
        /// </summary>
        /// <param name="type">TokenType</param>
        private void RewriteOnActionDeclaration(TokenType type)
        {
            if (type != TokenType.Entry && type != TokenType.Exit)
            {
                throw new ParsingException("parser: expected entry or exit on action type.");
            }

            var replaceIdx = base.Index;

            base.Tokens[replaceIdx] = new Token("override", TokenType.Override);
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token("void"));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(" ", TokenType.WhiteSpace));
            replaceIdx++;

            if (type == TokenType.Entry)
            {
                base.Tokens.Insert(replaceIdx, new Token("OnEntry"));
                replaceIdx++;
            }
            else if (type == TokenType.Exit)
            {
                base.Tokens.Insert(replaceIdx, new Token("OnExit"));
                replaceIdx++;
            }

            base.Tokens.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
            replaceIdx++;

            base.Tokens.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
            replaceIdx++;

            base.Index = replaceIdx;
            base.Index++;

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.DoAction)
            {
                base.Tokens.RemoveAt(base.Index);
            }

            base.Tokens.RemoveAt(base.Index);
        }

        /// <summary>
        /// Rewrites the action declaration.
        /// </summary>
        private void RewriteActionDeclaration()
        {
            base.Tokens[base.Index] = new Token("void", TokenType.ClassDecl);
            base.Index++;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type != TokenType.None)
            {
                throw new ParsingException("parser: identifier expected.");
            }

            base.Index++;
            var replaceIdx = base.Index;

            base.SkipWhiteSpaceTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Tokens.Insert(replaceIdx, new Token("(", TokenType.LeftParenthesis));
                replaceIdx++;

                base.Tokens.Insert(replaceIdx, new Token(")", TokenType.RightParenthesis));
                replaceIdx++;

                base.Index = replaceIdx;
                base.Index++;
            }
            else
            {
                throw new ParsingException("parser: left curly bracket expected.");
            }
        }

        /// <summary>
        /// Rewrites the raise statement.
        /// </summary>
        private void RewriteRaiseStatement()
        {
            base.Tokens[base.Index] = new Token("this.", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Raise"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("new", TokenType.New));
            base.Index++;

            base.SkipWhiteSpaceTokens();
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;
        }

        /// <summary>
        /// Rewrites the delete statement.
        /// </summary>
        private void RewriteDeleteStatement()
        {
            base.Tokens[base.Index] = new Token("this.", TokenType.This);
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("Delete"));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token("(", TokenType.LeftParenthesis));
            base.Index++;

            base.Tokens.Insert(base.Index, new Token(")", TokenType.RightParenthesis));
            base.Index++;

            base.SkipWhiteSpaceTokens();
            base.Index++;
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns the action type of the current state action declaration.
        /// </summary>
        /// <returns>ActionType</returns>
        private ActionType GetActionType()
        {
            var startIdx = base.Index;

            base.Index++;
            base.SkipWhiteSpaceTokens();

            if (base.Tokens[base.Index].Type == TokenType.Entry)
            {
                base.Index = startIdx;
                return ActionType.OnEntry;
            }
            else if (base.Tokens[base.Index].Type == TokenType.Exit)
            {
                base.Index = startIdx;
                return ActionType.OnExit;
            }

            throw new ParsingException("parser: no action type identified.");
        }

        #endregion
    }
}