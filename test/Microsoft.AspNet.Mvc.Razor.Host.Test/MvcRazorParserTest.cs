// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorCodeParserTest
    {
        public static TheoryData ViewImportsData
        {
            get
            {
                // codeTrees, expectedDirectiveDescriptors
                return new TheoryData<CodeTree[], TagHelperDirectiveDescriptor[]>
                {
                    {
                        new[] { CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP" }) },
                        new[] { CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix) }
                    },
                    {
                        new[] { CreateCodeTree(new AddTagHelperChunk { LookupText = "ATH" }) },
                        new[] { CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper) }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(
                                new AddTagHelperChunk { LookupText = "ATH1" },
                                new AddTagHelperChunk { LookupText = "ATH2" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("ATH1", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("ATH2", TagHelperDirectiveType.AddTagHelper)
                        }
                    },
                    {
                        new[] { CreateCodeTree(new RemoveTagHelperChunk { LookupText = "RTH" }) },
                        new[] { CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper) }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(
                                new RemoveTagHelperChunk { LookupText = "RTH1" },
                                new RemoveTagHelperChunk { LookupText = "RTH2" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH1", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("RTH2", TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP1" }),
                            CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP2" }),
                        },
                        new[] { CreateDirectiveDescriptor("THP1", TagHelperDirectiveType.TagHelperPrefix) }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(
                                new TagHelperPrefixDirectiveChunk { Prefix = "THP" },
                                new RemoveTagHelperChunk { LookupText = "RTH" },
                                new AddTagHelperChunk { LookupText = "ATH" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(
                                new LiteralChunk { Text = "Hello world" },
                                new AddTagHelperChunk { LookupText = "ATH" }),
                            CreateCodeTree(new RemoveTagHelperChunk { LookupText = "RTH" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                        }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP" }),
                            CreateCodeTree(
                                new LiteralChunk { Text = "Hello world" },
                                new AddTagHelperChunk { LookupText = "ATH" }),
                            CreateCodeTree(new RemoveTagHelperChunk { LookupText = "RTH" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                    {
                        new[]
                        {
                            CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP1" }),
                            CreateCodeTree(new AddTagHelperChunk { LookupText = "ATH" }),
                            CreateCodeTree(new RemoveTagHelperChunk { LookupText = "RTH" }),
                            CreateCodeTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP2" }),
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP1", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewImportsData))]
        public void GetTagHelperDescriptors_ReturnsExpectedDirectiveDescriptors(
            CodeTree[] codeTrees,
            TagHelperDirectiveDescriptor[] expectedDirectiveDescriptors)
        {
            // Arrange
            var builder = new BlockBuilder { Type = BlockType.Comment };
            var block = new Block(builder);

            IList<TagHelperDirectiveDescriptor> descriptors = null;
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(r => r.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                    .Callback((TagHelperDescriptorResolutionContext context) =>
                    {
                        descriptors = context.DirectiveDescriptors;
                    })
                    .Returns(Enumerable.Empty<TagHelperDescriptor>())
                    .Verifiable();

            var baseParser = new RazorParser(
                new CSharpCodeParser(),
                new HtmlMarkupParser(),
                tagHelperDescriptorResolver: resolver.Object);
            var parser = new TestableMvcRazorParser(baseParser, codeTrees, defaultInheritedChunks: new Chunk[0]);

            // Act
            parser.GetTagHelperDescriptorsPublic(block, errorSink: new ErrorSink()).ToArray();

            // Assert
            Assert.NotNull(descriptors);
            Assert.Equal(expectedDirectiveDescriptors.Length, descriptors.Count);

            for (var i = 0; i < expectedDirectiveDescriptors.Length; i++)
            {
                var expected = expectedDirectiveDescriptors[i];
                var actual = descriptors[i];

                Assert.Equal(expected.DirectiveText, actual.DirectiveText, StringComparer.Ordinal);
                Assert.Equal(SourceLocation.Zero, actual.Location);
                Assert.Equal(expected.DirectiveType, actual.DirectiveType);
            }
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("   ;  ", "")]
        [InlineData("    ", "")]
        [InlineData(";;", "")]
        [InlineData("a", "a")]
        [InlineData("a;", "a")]
        [InlineData("abcd", "abcd")]
        [InlineData("abc;d", "abc;d")]
        [InlineData("a bc d", "a bc d")]
        [InlineData("a\t\tbc\td\t", "a\t\tbc\td")]
        [InlineData("abc;", "abc")]
        [InlineData("  abc;", "abc")]
        [InlineData("\tabc;", "abc")]
        [InlineData(";; abc;", ";; abc")]
        [InlineData(";;\tabc;", ";;\tabc")]
        [InlineData("\t;;abc;", ";;abc")]
        [InlineData("abc;; ;", "abc")]
        [InlineData("abc;;\t;", "abc")]
        [InlineData("\tabc  \t;", "abc")]
        [InlineData("abc;;\r\n;", "abc")]
        [InlineData("abcd \n", "abcd")]
        [InlineData("\r\n\r  \n\t  abcd \t \t \n  \r\n", "abcd")]
        [InlineData("pqrs\r", "pqrs")]
        public void RemoveWhitespaceAndTrailingSemicolons_ReturnsExpectedValues(string input, string expectedOutput)
        {
            // Arrange and Act
            var output = MvcRazorCodeParser.RemoveWhitespaceAndTrailingSemicolons(input);

            // Assert
            Assert.Equal(expectedOutput, output, StringComparer.Ordinal);
        }

        private static CodeTree CreateCodeTree(params Chunk[] chunks)
        {
            return new CodeTree
            {
                Chunks = chunks
            };
        }

        private static TagHelperDirectiveDescriptor CreateDirectiveDescriptor(
            string directiveText,
            TagHelperDirectiveType directiveType)
        {
            return new TagHelperDirectiveDescriptor(directiveText, SourceLocation.Undefined, directiveType);
        }

        private class TestableMvcRazorParser : MvcRazorParser
        {
            public TestableMvcRazorParser(RazorParser parser,
                                          IReadOnlyList<CodeTree> codeTrees,
                                          IReadOnlyList<Chunk> defaultInheritedChunks)
                : base(parser, codeTrees, defaultInheritedChunks, typeof(ModelExpression).FullName)
            {
            }

            public IEnumerable<TagHelperDescriptor> GetTagHelperDescriptorsPublic(
                Block documentRoot,
                ErrorSink errorSink)
            {
                return GetTagHelperDescriptors(documentRoot, errorSink);
            }
        }
    }
}