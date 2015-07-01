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
        /// 8.2.4.34 Before attribute name state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#before-attribute-name-state"/> 
        /// </summary>
        void R34_BeforeAttributeName()
        {
            char c;
            while (ReadNext(out c))
            {

                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '/':
                        TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                        return;
                    case '>':
                        EmitTagToken();
                        return;
                    case '"':
                    case '\'':
                    case '<':
                    case '=':
                        ParseError();// parse error
                        goto default;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        TokenizerState = HtmlTokenizerState.s35_AttributeName;
                        name.Append(c);
                        return;
                }
            }
            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            tag = null;
            EmitDataToken();
        }
        /// <summary>
		/// 8.2.4.35 Attribute name state
		/// <seealso cref="http://www.w3.org/TR/html5/syntax.html#attribute-name-state"/> 
		/// </summary>
        void R35_AttributeName()
        {
            char c;
            while (ReadNext(out c))
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        TokenizerState = HtmlTokenizerState.s36_AfterAttributeName;
                        EmitTagAttribute();
                        return;
                    case '/':
                        TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                        EmitTagAttribute();
                        return;
                    case '=':
                        TokenizerState = HtmlTokenizerState.s37_BeforeAttributeValue;
                        EmitTagAttribute();
                        return;
                    case '>':
                        EmitTagAttribute();
                        EmitTagToken();
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
            name.Length = 0;
            tag = null;
            EmitDataToken();
        }
        /// <summary>
        /// 8.2.4.36 After attribute name state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#after-attribute-name-state"/> 
        /// </summary>
        void R36_AfterAttributeName()
        {
            char c;
            while (ReadNext(out c))
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '/':
                        TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                        return;
                    case '=':
                        TokenizerState = HtmlTokenizerState.s37_BeforeAttributeValue;
                        return;
                    case '>':
                        EmitTagToken();
                        return;
                    case '"':
                    case '\'':
                    case '<':
                        ParseError();// parse error
                        goto default;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        TokenizerState = HtmlTokenizerState.s35_AttributeName;
                        name.Append(c);
                        return;
                }
            }
            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            tag = null;
            EmitDataToken();
        }
        /// <summary>
        /// 8.2.4.37 Before attribute value state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#before-attribute-value-state"/> 
        /// </summary>
        void R37_BeforeAttributeValue()
        {
            char c;
            while (ReadNext(out c))
            {

                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '"':
                    case '\'':
                        TokenizerState = HtmlTokenizerState.s38_39_AttributeValueQuoted;
                        quote = c;
                        return;
                    case '&':
                        TokenizerState = HtmlTokenizerState.s40_AttributeValueUnquoted;
                        return;
                    case '/':
                        TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                        return;
                    case '>':
                        EmitTagToken();
                        return;
                    case '<':
                    case '=':
                    case '`':
                        ParseError();// parse error
                        goto default;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        TokenizerState = HtmlTokenizerState.s40_AttributeValueUnquoted;
                        name.Append(c);
                        return;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            tag = null;
            EmitDataToken();
        }
        /// <summary>
        /// 8.2.4.38 Attribute value (double-quoted) state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28double-quoted%29-state"/> 
        /// 8.2.4.39 Attribute value (single-quoted) state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28single-quoted%29-state"/> 
        /// </summary>
        void R38_39_AttributeValueQuoted()
        {
            char c;
            while (ReadNext(out c))
            {
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                switch (c)
                {
                    case '&':
                        TokenizerState = HtmlTokenizerState.s41_CharacterReferenceInAttributeValue;
                        return;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        if (c == quote)
                        {
                            TokenizerState = HtmlTokenizerState.s42_AfterAttributeValueQuoted;
                            attribute.Value = ClearNameBuffer();
                            return;
                        }
                        else
                        {
                            name.Append(c);
                        }
                        break;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            name.Length = 0;
            EmitDataToken();
        }
        /// <summary>
        /// 8.2.4.40 Attribute value (unquoted) state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28unquoted%29-state"/> 
        /// </summary>
        void R40_AttributeValueUnquoted()
        {
            char c;
            while (ReadNext(out c))
            {

                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);
                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        TokenizerState = HtmlTokenizerState.s34_BeforeAttributeName;
                        attribute.Value = ClearNameBuffer();
                        return;
                    case '&':
                        TokenizerState = HtmlTokenizerState.s41_CharacterReferenceInAttributeValue;
                        return;
                    case '>':
                        attribute.Value = ClearNameBuffer();
                        EmitTagToken();
                        return;
                    case '\'':
                    case '<':
                    case '=':
                    case '`':
                        ParseError();// parse error
                        goto default;
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
            name.Length = 0;
            EmitDataToken();
        }
        /// <summary>
        /// 8.2.4.41 Character reference in attribute value state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#character-reference-in-attribute-value-state"/> 
        /// </summary>
        void R41_CharacterReferenceInAttributeValue()
        {
            int additionalAllowedCharacter = (quote == -1) ? '>' : quote;
            char c;
            CharMode charMode;
            if (!Peek(out c, out charMode))
            {
                TokenizerState = HtmlTokenizerState.EndOfFile;
                data.Append('&');
                name.Length = 0;
                EmitDataToken();
                return;
            }

            bool consume;

            switch (c)
            {
                case '\t':
                case '\r':
                case '\n':
                case '\f':
                case ' ':
                case '<':
                case '&':
                    // no character is consumed, emit '&'
                    data.Append('&');
                    name.Append('&');
                    consume = false;
                    break;
                default:
                    if (c == additionalAllowedCharacter)
                    {
                        // this is not a character reference, nothing is consumed
                        data.Append('&');
                        name.Append('&');
                        consume = false;
                        break;
                    }

                    entity.Push('&');
                    while (entity.Push(c))
                    {
                        ReadNext();
                        if (!Peek(out c, out charMode))
                        {
                            TokenizerState = HtmlTokenizerState.EndOfFile;
                            data.Append(entity.GetPushedInput());
                            entity.Reset();
                            EmitDataToken();
                            return;
                        }
                    }
                    var pushed = entity.GetPushedInput();
                    string value;

                    switch (charMode)
                    {
                        default:
                            value = entity.GetValue();
                            break;
                        case CharMode.Assign:
                        case CharMode.LowerAsciiLetter:
                        case CharMode.UpperAsciiLetter:
                        case CharMode.Numeric:
                            value = pushed;
                            break;
                    }
                    data.Append(pushed);
                    name.Append(value);
                    consume = c == ';';
                    entity.Reset();
                    break;
            }

            if (quote == -1)
                TokenizerState = HtmlTokenizerState.s40_AttributeValueUnquoted;
            else
                TokenizerState = HtmlTokenizerState.s38_39_AttributeValueQuoted;

            if (consume)
                ReadNext();


        }
        /// <summary>
        /// 8.2.4.42 After attribute value (quoted) state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#after-attribute-value-%28quoted%29-state"/> 
        /// </summary>
        void R42_AfterAttributeValueQuoted()
        {
            char c;
            if (!Peek(out c))
            {
                TokenizerState = HtmlTokenizerState.EndOfFile;
                EmitDataToken();
                return;
            }

            switch (c)
            {
                case '\t':
                case '\r':
                case '\n':
                case '\f':
                case ' ':
                    TokenizerState = HtmlTokenizerState.s34_BeforeAttributeName;
                    data.Append(c);
                    ReadNext(); //consume
                    return;
                case '/':
                    TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                    data.Append(c);
                    ReadNext();//consume
                    return;
                case '>':
                    EmitTagToken();
                    ReadNext();//consume
                    return;
                default:
                    TokenizerState = HtmlTokenizerState.s34_BeforeAttributeName;
                    break;
            }
        }


    }
}