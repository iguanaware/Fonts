﻿using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_27
    {
        [Fact]
        public void ThrowsMeasureingWhitespace()
        {
            // wendy one returns wrong points for 'o'
            Font font = new FontCollection().Install(TestFonts.WendyOneFile).CreateFont(12);

            GlyphRenderer r = new GlyphRenderer();

            var size = TextMeasurer.MeasureBounds("          ", new RendererOptions(new Font(font, 30), 72)).Size;

            Assert.Equal(60, size.Width, 1);
            Assert.Equal(31.6, size.Height, 1);
        }
    }
}
