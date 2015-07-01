//
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
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#bogus-comment-state"/> 
        /// </summary>
        void R44_BogusComment()
        {

            char c;

            if (data.Length > 0 && data[0] == '<')
            {
                //strip the leading '<' but leave the rest

                var buf = data.ToString();
                for (int i = 1; i < data.Length; ++i)
                {
                    data[i - 1] = data[i];
                }
                data.Length--;

            }

            while (ReadNext(out c))
            {
                switch (c)
                {
                    case '>':
                        TokenizerState = HtmlTokenizerState.s01_Data;
                        EmitCommentToken(data.ToString(), true);
                        return;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        data.Append(c);
                        break;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitCommentToken(data.ToString());
        }
        /// <summary>
        /// 8.2.4.45 Markup declaration open state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#markup-declaration-open-state"/> 
        /// </summary>
        void R45_MarkupDeclarationOpen()
        {
            //TODO: review this method again 
            int count = 0;
            char c = '\0';//?                
            while (count < 2)
            {
                if (!ReadNext(out c))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    EmitDataToken();
                    return;
                }

                if (c != '-')
                {
                    break;
                }
                else
                {
                    // Note: we save the data in case we hit a parse error and have to emit a data token
                    data.Append(c);
                    ReadNext();
                    count++;
                }
            }


            if (count == 2)
            {
                TokenizerState = HtmlTokenizerState.s46_CommentStart;
                name.Length = 0;
                return;
            }

            if (count == 1)
            {  
                ParseError(HtmlTokenizerState.s44_BogusComment);// parse error 
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
                    doctype = CreateDocTypeToken(ClearNameBuffer());
                    TokenizerState = HtmlTokenizerState.s52_DocType;
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

                    // Note: we save the data in case we hit a parse error and have to emit a data token
                    data.Append(c);

                    if (c != CData[count])
                        break;


                    count++;
                }

                if (count == 7)
                {
                    TokenizerState = HtmlTokenizerState.s68_CDataSection;
                    return;
                }
            }

             
            ParseError(HtmlTokenizerState.s44_BogusComment);// parse error  
        }
        /// <summary>
        /// 8.2.4.46 Comment start state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-start-state"/> 
        /// </summary>
        void R46_CommentStart()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.s01_Data;

                EmitCommentToken(string.Empty);
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.s47_CommentStartDash;
                    break;
                case '>':
                    ParseError(HtmlTokenizerState.s01_Data);// parse error 
                    EmitCommentToken(string.Empty);
                    return;
                case '\0':
                    c = '\uFFFD';
                    goto default;
                default:
                    ParseError(HtmlTokenizerState.s48_Comment);// parse  
                    name.Append(c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.47 Comment start dash state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-start-dash-state"/> 
        /// </summary>
        void R47_CommentStartDash()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.s01_Data;
                EmitCommentTokenFromNameBuffer();
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.s50_CommentEnd;
                    break;
                case '>':
                    ParseError(HtmlTokenizerState.s01_Data);// parse error 
                    EmitCommentTokenFromNameBuffer();
                    return;
                case '\0':
                    c = '\uFFFD';
                    goto default;
                default:
                    ParseError(HtmlTokenizerState.s48_Comment);// parse error 
                    name.Append('-');
                    name.Append(c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.48 Comment state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-state"/> 
        /// </summary>
        void R48_Comment()
        {

            char c;
            while (ReadNext(out c))
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '-':
                        TokenizerState = HtmlTokenizerState.s49_CommentEndDash;
                        return;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        name.Append(c);
                        break;
                }
            }


            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitCommentTokenFromNameBuffer();
        }

        // FIXME: this is exactly the same as ReadCommentStartDash
        /// <summary>
		/// 8.2.4.49 Comment end dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-dash-state"/> 
		/// </summary>
        void R49_CommentEndDash()
        {

            char c;
            if (!ReadNext(out c))
            {
                TokenizerState = HtmlTokenizerState.s01_Data;
                EmitCommentTokenFromNameBuffer();
                return;
            }

            data.Append(c);

            switch (c)
            {
                case '-':
                    TokenizerState = HtmlTokenizerState.s50_CommentEnd;
                    break;
                case '>':
                    ParseError(HtmlTokenizerState.s01_Data);// parse error 
                    EmitCommentTokenFromNameBuffer();
                    return;
                case '\0':
                    c = '\uFFFD';
                    goto default;
                default:
                    ParseError(HtmlTokenizerState.s48_Comment);// parse error 
                    name.Append('-');
                    name.Append(c);
                    break;
            }
        }
        /// <summary>
        /// 8.2.4.50 Comment end state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-state"/> 
        /// </summary>
        void R50_CommentEnd()
        {

            char c;
            while (ReadNext(out c))
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                switch (c)
                {
                    case '>':
                        TokenizerState = HtmlTokenizerState.s01_Data;
                        EmitCommentTokenFromNameBuffer();
                        return;
                    case '!':
                        ParseError(HtmlTokenizerState.s51_CommentEndBang);// parse error 
                        return;
                    case '-':
                        name.Append('-');
                        break;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        TokenizerState = HtmlTokenizerState.s48_Comment;
                        name.Append(c);
                        return;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitCommentTokenFromNameBuffer();
        }
        /// <summary>
        /// 8.2.4.51 Comment end bang state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-bang-state"/> 
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
                    TokenizerState = HtmlTokenizerState.s49_CommentEndDash;
                    name.Append("--!");
                    break;
                case '>':
                    TokenizerState = HtmlTokenizerState.s01_Data;
                    EmitCommentTokenFromNameBuffer();
                    return;
                case '\0':
                    c = '\uFFFD';
                    goto default;
                default:
                    ParseError(HtmlTokenizerState.s48_Comment);// parse error 
                    name.Append("--!");
                    name.Append(c);
                    break;
            }
        }

    }
}