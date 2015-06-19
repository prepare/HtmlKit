﻿//
// HtmlTokenizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//



namespace HtmlKit
{

    partial class HtmlTokenizer
    {
        /// <summary>
        /// 8.2.4.44 Bogus comment state
        /// </summary>
        void R44_BogusComment()
        {

            char c;

            if (data.Length > 0)
            {
                c = data[data.Length - 1];
                data.Length = 1;
                data[0] = c;
            }


            do
            {
                if (!ReadNext(out c))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    break;
                }
                if (c == '>')
                {
                    break;
                }
                else
                {
                    data.Append(c == '\0' ? '\uFFFD' : c);
                }
            } while (true);

            EmitCommentToken(data.ToString());
        }
        /// <summary>
        /// 8.2.4.45 Markup declaration open state
        /// </summary>
        void R45_MarkupDeclarationOpen()
        {
            int count = 0;
            char c = '\0';

            while (count < 2)
            {
                if(!ReadNext(out c))                 
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    EmitDataToken();
                    return;
                }

                if (c != '-')
                    break;

                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                ReadNext();
                count++;
            }


            if (count == 2)
            {
                TokenizerState = HtmlTokenizerState.CommentStart;
                name.Length = 0;
                return;
            }

            if (count == 1)
            {
                // parse error
                TokenizerState = HtmlTokenizerState.BogusComment;
                return;
            }

            if (c == 'D' || c == 'd')
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                name.Append(c);
                ReadNext();
                count = 1;

                while (count < 7)
                {
                    if (!ReadNext(out c))
                    {
                        TokenizerState = HtmlTokenizerState.EndOfFile;
                        EmitDataToken();
                        return;
                    }

                    if (ToLower(c) != DocType[count])
                        break;

                    // Note: we save the data in case we hit a parse error and have to emit a data token
                    data.Append(c);
                    name.Append(c);
                    count++;
                }

                if (count == 7)
                {
                    doctype = CreateDocTypeToken(name.ToString());
                    TokenizerState = HtmlTokenizerState.DocType;
                    name.Length = 0;
                    return;
                }

                name.Length = 0;
            }
            else if (c == '[')
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                ReadNext();
                count = 1;

                while (count < 7)
                {
                    if (!ReadNext(out c))
                    {
                        TokenizerState = HtmlTokenizerState.EndOfFile;
                        EmitDataToken();
                        return;
                    }

                    if (c != CData[count])
                        break;

                    // Note: we save the data in case we hit a parse error and have to emit a data token
                    data.Append(c);
                    count++;
                }

                if (count == 7)
                {
                    TokenizerState = HtmlTokenizerState.CDataSection;
                    return;
                }
            }

            // parse error
            TokenizerState = HtmlTokenizerState.BogusComment;


        }
        /// <summary>
        /// 8.2.4.46 Comment start state
        /// </summary>
        void R46_CommentStart()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.Data;

                EmitCommentToken(string.Empty);
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.CommentStartDash;
                    break;
                case '>': // parse error
                    TokenizerState = HtmlTokenizerState.Data;
                    EmitCommentToken(string.Empty);
                    return;
                default: // parse error
                    TokenizerState = HtmlTokenizerState.Comment;
                    name.Append(c == '\0' ? '\uFFFD' : c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.47 Comment start dash state
        /// </summary>
        void R47_CommentStartDash()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.Data;
                EmitCommentTokenFromNameBuffer();
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.CommentEnd;
                    break;
                case '>': // parse error
                    TokenizerState = HtmlTokenizerState.Data;
                    EmitCommentTokenFromNameBuffer();
                    return;
                default: // parse error
                    TokenizerState = HtmlTokenizerState.Comment;
                    name.Append('-');
                    name.Append(c == '\0' ? '\uFFFD' : c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.48 Comment state
        /// </summary>
        void R48_Comment()
        {

            do
            {

                char c;
                if (!ReadNext(out c))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    EmitCommentTokenFromNameBuffer();
                    return;
                }


                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '-':
                        TokenizerState = HtmlTokenizerState.CommentEndDash;
                        return;
                    default:
                        name.Append(c == '\0' ? '\uFFFD' : c);
                        break;
                }
            } while (true);
        }

        // FIXME: this is exactly the same as ReadCommentStartDash
        /// <summary>
        /// 8.2.4.49 Comment end dash state
        /// </summary>
        void R49_CommentEndDash()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.Data;
                EmitCommentTokenFromNameBuffer();
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.CommentEnd;
                    break;
                case '>': // parse error
                    TokenizerState = HtmlTokenizerState.Data;
                    EmitCommentTokenFromNameBuffer();
                    return;
                default: // parse error
                    TokenizerState = HtmlTokenizerState.Comment;
                    name.Append('-');
                    name.Append(c == '\0' ? '\uFFFD' : c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.50 Comment end state
        /// </summary>
        void R50_CommentEnd()
        {
            do
            {

                char c;
                if (!ReadNext(out c))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    EmitCommentTokenFromNameBuffer();
                    return;
                }



                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '>':
                        TokenizerState = HtmlTokenizerState.Data;
                        EmitCommentTokenFromNameBuffer();
                        return;
                    case '!': // parse error
                        TokenizerState = HtmlTokenizerState.CommentEndBang;
                        return;
                    case '-':
                        name.Append('-');
                        break;
                    default:
                        TokenizerState = HtmlTokenizerState.Comment;
                        name.Append(c == '\0' ? '\uFFFD' : c);
                        return;
                }
            } while (true);
        }
        /// <summary>
        /// 8.2.4.51 Comment end bang state
        /// </summary>
        void R51_CommentEndBang()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.EndOfFile;
                EmitCommentTokenFromNameBuffer();
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.CommentEndDash;
                    name.Append("--!");
                    break;
                case '>':
                    TokenizerState = HtmlTokenizerState.Data;
                    EmitCommentTokenFromNameBuffer();
                    return;
                default: // parse error
                    TokenizerState = HtmlTokenizerState.Comment;
                    name.Append("--!");
                    name.Append(c == '\0' ? '\uFFFD' : c);
                    break;
            }
        }

    }
}