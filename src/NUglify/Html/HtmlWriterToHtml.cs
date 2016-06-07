﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace NUglify.Html
{
    public class HtmlWriterToHtml : HtmlWriterBase
    {
        private readonly HtmlSettings settings;

        private bool lastNewLine = false;

        private bool allowIndent = true;

        private static readonly char[] AttributeCharsForcingQuote = new[]
        {' ', '\t', '\n', '\f', '\r', '"', '\'', '`', '=', '<', '>'};

        public HtmlWriterToHtml(TextWriter writer, HtmlSettings settings = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            Writer = writer;
            writer.NewLine = "\n";
            this.settings = settings;
        }


        public TextWriter Writer { get; }

        protected override void Write(string text)
        {
            if (settings.PrettyPrint && lastNewLine && allowIndent)
            {
                for (int i = 0; i < Depth; i++)
                {
                    Writer.Write("  ");
                }
                lastNewLine = false;
            }

            Writer.Write(text);
        }

        protected override void Write(char c)
        {
            if (settings.PrettyPrint && lastNewLine && allowIndent)
            {
                for (int i = 0; i < Depth; i++)
                {
                    Writer.Write("  ");
                }
                lastNewLine = false;
            }

            Writer.Write(c);
        }

        protected override void WriteStartTag(HtmlElement node)
        {
            var shouldPretty = ShouldPretty(node);
            allowIndent = (settings.PrettyPrint && node.Name != "pre");
            if (shouldPretty && !lastNewLine)
            {
                Writer.WriteLine();
                lastNewLine = true;
            }

            base.WriteStartTag(node);

            if (shouldPretty)
            {
                Writer.WriteLine();
                lastNewLine = true;
            }
        }

        protected override void WriteEndTag(HtmlElement node)
        {
            var shouldPretty = ShouldPretty(node);

            if (shouldPretty && !lastNewLine)
            {
                Writer.WriteLine();
                lastNewLine = true;
            }

            base.WriteEndTag(node);

            allowIndent = true;

            if (shouldPretty)
            {
                Writer.WriteLine();
                lastNewLine = true;
            }
        }

        private bool ShouldPretty(HtmlElement node)
        {
            return settings.PrettyPrint && node.Descriptor != null &&
                   !settings.InlineTagsPreservingSpacesAround.ContainsKey(node.Descriptor.Name) &&
                   node.Descriptor.Category != ContentKind.Phrasing && 
                   (node.Parent?.Descriptor == null || (node.Parent.Descriptor.Category & ContentKind.Phrasing) == 0);
        }


        protected override void WriteAttributeValue(HtmlElement element, HtmlAttribute attribute, bool isLast)
        {
            var attrValue = attribute.Value;

            var quoteChar = (char) 0;

            if (settings.AttributeQuoteChar == null)
            {
                var quoteCount = 0;
                var doubleQuoteCount = 0;

                for (int i = 0; i < attrValue.Length; i++)
                {
                    var c = attrValue[i];
                    if (c == '\'')
                    {
                        quoteCount++;
                    }
                    else if (c == '"')
                    {
                        doubleQuoteCount++;
                    }

                    // We also count escapes so that we have an exact count for both
                    if (c == '&')
                    {
                        if (attrValue.IndexOf("&#34;", i, StringComparison.OrdinalIgnoreCase) > 0
                            || attrValue.IndexOf("&quot;", i, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            doubleQuoteCount++;
                        }
                        else if (attrValue.IndexOf("&#39;", i, StringComparison.OrdinalIgnoreCase) > 0
                                 || attrValue.IndexOf("&apos;", i, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            quoteCount++;
                        }
                    }
                }

                quoteChar = quoteCount < doubleQuoteCount ? '\'' : '"';
            }
            else
            {
                quoteChar = settings.AttributeQuoteChar.Value == '"' ? '"' : '\'';
            }

            if (quoteChar == '"')
            {
                attrValue = attrValue.Replace("&#39;", "'");
                attrValue = attrValue.Replace("&apos;", "'");
                attrValue = attrValue.Replace("\"", "&#34;");
            }
            else
            {
                attrValue = attrValue.Replace("&#34;", "\"");
                attrValue = attrValue.Replace("&quot;", "\"");
                attrValue = attrValue.Replace("'", "&#39;");
            }

            var canRemoveQuotes = settings.RemoveQuotedAttributes && attrValue != string.Empty && attrValue.IndexOfAny(AttributeCharsForcingQuote) < 0;

            if (!canRemoveQuotes)
            {
                Write(quoteChar);
            }

            Write(attrValue);

            if (!canRemoveQuotes)
            {
                Write(quoteChar);
            }
            else
            {
                bool emitSpace = false;

                if (isLast)
                {
                    if (attrValue.EndsWith("/"))
                    {
                        emitSpace = true;
                    }
                }

                if (emitSpace)
                {
                    Write(" ");
                }
            }
        }
    }
}