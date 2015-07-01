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
        void ReadGenericRawTextLessThan(HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagOpen)
        {
            char c;
            if (!Peek(out c))
            {
                //eof
                throw new System.NotSupportedException();
                TokenizerState = HtmlTokenizerState.EndOfFile;
                return;
            }

            data.Append('<');
            switch (c)
            {
                case '/':
                    TokenizerState = rawTextEndTagOpen;
                    data.Append('/');
                    name.Length = 0;
                    ReadNext();
                    break;
                default:
                    TokenizerState = rawText;
                    break;
            }
        }

        void ReadGenericRawTextEndTagOpen(bool decoded, HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagName)
        {

            char c;
            CharMode charMode;
            if (!Peek(out c, out charMode))
            {
                TokenizerState = HtmlTokenizerState.EndOfFile;
                EmitDataToken(decoded);
                return;
            }
            switch (charMode)
            {
                //IsAsciiLetter(c)
                case CharMode.UpperAsciiLetter:
                case CharMode.LowerAsciiLetter:
                    TokenizerState = rawTextEndTagName;
                    name.Append(c);
                    data.Append(c);
                    ReadNext();
                    break;
                default:
                    TokenizerState = rawText;
                    break;
            }
        }

        void ReadGenericRawTextEndTagName(bool decoded, HtmlTokenizerState rawTextState)
        {
            var current = TokenizerState;

            do
            {

                char c;
                CharMode charMode;
                if (!ReadNext(out c, out charMode))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    name.Length = 0;
                    EmitDataToken(decoded);
                    return;
                }
                // Note: we save the data in case we hit a parse error and have to emit a data token
                data.Append(c);

                switch (c)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (NameIs(activeTagName))
                        {
                            TokenizerState = HtmlTokenizerState.s34_BeforeAttributeName;
                            break;
                        } 
                        goto default;
                    case '/':
                        if (NameIs(activeTagName))
                        {
                            TokenizerState = HtmlTokenizerState.s43_SelfClosingStartTag;
                            break;
                        }
                        goto default;
                    case '>':
                        if (NameIs(activeTagName))
                        {
                            SetEmitToken(CreateTagTokenFromNameBuffer(true));
                            TokenizerState = HtmlTokenizerState.s01_Data;
                            data.Length = 0;
                            return;
                        }
                        goto default;
                    default:
                        switch (charMode)
                        {
                            //TODO: review here
                            //when we found close tag that not match with activeTagName

                            default://if (!IsAsciiLetter(c))
                                TokenizerState = rawTextState;
                                return;
                            case CharMode.UpperAsciiLetter:
                            case CharMode.LowerAsciiLetter:
                                name.Append(c);
                                break;
                            case CharMode.NullChar:
                                name.Append('\uFFFD');
                                break;
                        }
                        break;
                }
            } while (TokenizerState == current);

            tag = CreateTagTokenFromNameBuffer(true);
        }

        /// <summary>
        /// 8.2.4.2 Character reference in data state
        /// <see cref=" http://www.w3.org/TR/html5/syntax.html#character-reference-in-data-state"/>
        /// </summary>
        void R02_CharacterReferenceInData()
        {
            ReadCharacterReference(HtmlTokenizerState.s01_Data);
        }
        /// <summary>
        /// 8.2.4.3 RCDATA state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-state"/> 
        /// </summary>
        void R03_RcData()
        {
            char c;
            while (ReadNext(out c))
            {
                switch (c)
                {
                    case '&':
                        if (DecodeCharacterReferences)
                        {
                            TokenizerState = HtmlTokenizerState.s04_CharacterReferenceInRcData;
                            return;
                        }

                        goto default;
                    case '<':
                        TokenizerState = HtmlTokenizerState.s11_RcDataLessThan;
                        EmitDataToken(DecodeCharacterReferences);
                        return;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        data.Append(c);
                        // Note: we emit at 1024 characters simply to avoid
                        // consuming too much memory.
                        if (data.Length >= 1024)
                        {
                            EmitDataToken(DecodeCharacterReferences);
                        }

                        break;
                }
            }


            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitDataToken(DecodeCharacterReferences);

        }

        /// <summary>
        /// 8.2.4.4 Character reference in RCDATA state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#character-reference-in-rcdata-state"/> 
        /// </summary>
        void R04_CharacterReferenceInRcData()
        {
            ReadCharacterReference(HtmlTokenizerState.s03_RcData);

        }
        /// <summary>
        /// 8.2.4.5 RAWTEXT state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-state"/> 
        /// </summary>
        void R05_RawText()
        {
            char c;
            while (ReadNext(out c))
            {
                switch (c)
                {
                    case '<':
                        TokenizerState = HtmlTokenizerState.s14_RawTextLessThan;
                        EmitDataToken();
                        return;
                    case '\0':
                        c = '\uFFFD';
                        goto default;
                    default:
                        data.Append(c);
                        // Note: we emit at 1024 characters simply to avoid
                        // consuming too much memory.
                        if (data.Length >= 1024)
                        {
                            EmitDataToken();
                            return;
                        }
                        break;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitDataToken();
        }


        /// <summary>
        /// 8.2.4.7 PLAINTEXT state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#plaintext-state"/> 
        /// </summary>
        void R07_PlainText()
        {

            char c;
            while (ReadNext(out c))
            {
                data.Append(c == '\0' ? '\uFFFD' : c);
                // Note: we emit at 1024 characters simply to avoid
                // consuming too much memory.
                if (data.Length >= 1024)
                {
                    EmitDataToken();
                    return;
                }
            }

            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            EmitDataToken();
        }

        void ReadCharacterReference(HtmlTokenizerState next)
        {

            char c;
            if (!Peek(out c))
            {
                TokenizerState = HtmlTokenizerState.EndOfFile;
                data.Append('&');
                EmitDataToken(true);
                return;
            }
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
                    TokenizerState = next;
                    data.Append('&');
                    return;
            }

            entity.Push('&');

            while (entity.Push(c))
            {
                ReadNext();

                if (!Peek(out c))
                {
                    TokenizerState = HtmlTokenizerState.EndOfFile;
                    data.Append(entity.GetPushedInput());
                    entity.Reset();

                    EmitDataToken(true);
                    return;
                }
            }

            TokenizerState = next;
            data.Append(entity.GetValue());
            entity.Reset();

            if (c == ';')
            {
                // consume the ';'
                ReadNext();
            }


        }
        /// <summary>
        /// 8.2.4.14 RAWTEXT less-than sign state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-less-than-sign-state"/> 
        /// </summary>
        void R14_RawTextLessThan()
        {
            ReadGenericRawTextLessThan(HtmlTokenizerState.s05_RawText, HtmlTokenizerState.s15_RawTextEndTagOpen);
        }
        /// <summary>
        /// 8.2.4.15 RAWTEXT end tag open state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-end-tag-open-state"/> 
        /// </summary>
        void R15_RawTextEndTagOpen()
        {
            ReadGenericRawTextEndTagOpen(false, HtmlTokenizerState.s05_RawText, HtmlTokenizerState.s16_RawTextEndTagName);
        }
        /// <summary>
        /// 8.2.4.16 RAWTEXT end tag name state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-end-tag-name-state"/> 
        /// </summary>
        void R16_RawTextEndTagName()
        {
            ReadGenericRawTextEndTagName(false, HtmlTokenizerState.s05_RawText);
        }

        /// <summary>
        /// 8.2.4.11 RCDATA less-than sign state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-less-than-sign-state"/> 
        /// </summary>
        void R11_RcDataLessThan()
        {
            ReadGenericRawTextLessThan(HtmlTokenizerState.s03_RcData, HtmlTokenizerState.s12_RcDataEndTagOpen);
        }
        /// <summary>
        /// 8.2.4.12 RCDATA end tag open state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-end-tag-open-state"/> 
        /// </summary>
        void R12_RcDataEndTagOpen()
        {
            ReadGenericRawTextEndTagOpen(DecodeCharacterReferences, HtmlTokenizerState.s03_RcData, HtmlTokenizerState.s13_RcDataEndTagName);
        }
        /// <summary>
        /// 8.2.4.13 RCDATA end tag name state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-end-tag-name-state"/> 
        /// </summary>
        void R13_RcDataEndTagName()
        {
            ReadGenericRawTextEndTagName(DecodeCharacterReferences, HtmlTokenizerState.s03_RcData);
        }


        /// <summary>
        /// 8.2.4.68 CDATA section state
        /// <see cref="http://www.w3.org/TR/html5/syntax.html#cdata-section-state"/> 
        /// </summary>
        void R68_CDataSection()
        {
            int cdataIndex = 0;

            char c;
            while (ReadNext(out c))
            {
                if (cdataIndex >= 3)
                {
                    data.Append(cdata[0]);
                    cdata[0] = cdata[1];
                    cdata[1] = cdata[2];
                    cdata[2] = c;

                    if (cdata[0] == ']' && cdata[1] == ']' && cdata[2] == '>')
                    {
                        TokenizerState = HtmlTokenizerState.s01_Data;
                        cdataIndex = 0;

                        EmitCDataToken();
                        return;
                    }
                }
                else
                {
                    cdata[cdataIndex++] = c;
                }
            }


            //eof
            TokenizerState = HtmlTokenizerState.EndOfFile;
            for (int i = 0; i < cdataIndex; i++)
                data.Append(cdata[i]);

            cdataIndex = 0;
            EmitCDataToken();
        }

    }
}