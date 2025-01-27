// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal sealed class Format4SubTable : CMapSubTable
    {
        public Format4SubTable(ushort language, PlatformIDs platform, ushort encoding, Segment[] segments, ushort[] glyphIds)
            : base(platform, encoding, 4)
        {
            this.Language = language;
            this.Segments = segments;
            this.GlyphIds = glyphIds;
        }

        public Segment[] Segments { get; }

        public ushort[] GlyphIds { get; }

        public ushort Language { get; }

        public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
        {
            int charAsInt = codePoint.Value;

            for (int i = 0; i < this.Segments.Length; i++)
            {
                ref Segment seg = ref this.Segments[i];

                if (seg.End >= charAsInt && seg.Start <= charAsInt)
                {
                    if (seg.Offset == 0)
                    {
                        glyphId = (ushort)((charAsInt + seg.Delta) & ushort.MaxValue);
                        return true;
                    }
                    else
                    {
                        long offset = (seg.Offset / 2) + (charAsInt - seg.Start);
                        glyphId = this.GlyphIds[offset - this.Segments.Length + seg.Index];
                        return true;
                    }
                }
            }

            glyphId = 0;
            return false;
        }

        public static IEnumerable<Format4SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
        {
            // 'cmap' Subtable Format 4:
            // Type   | Name                       | Description
            // -------|----------------------------|------------------------------------------------------------------------
            // uint16 | format                     | Format number is set to 4.
            // uint16 | length                     | This is the length in bytes of the subtable.
            // uint16 | language                   | Please see “Note on the language field in 'cmap' subtables“ in this document.
            // uint16 | segCountX2                 | 2 x segCount.
            // uint16 | searchRange                | 2 x (2**floor(log2(segCount)))
            // uint16 | entrySelector              | log2(searchRange/2)
            // uint16 | rangeShift                 | 2 x segCount - searchRange
            // uint16 | endCount[segCount]         | End characterCode for each segment, last=0xFFFF.
            // uint16 | reservedPad                | Set to 0.
            // uint16 | startCount[segCount]       | Start character code for each segment.
            // int16  | idDelta[segCount]           | Delta for all character codes in segment.
            // uint16 | idRangeOffset[segCount]    | Offsets into glyphIdArray or 0
            // uint16 | glyphIdArray[ ]            | Glyph index array (arbitrary length)
            // format has already been read by this point skip it
            ushort length = reader.ReadUInt16();
            ushort language = reader.ReadUInt16();
            ushort segCountX2 = reader.ReadUInt16();
            ushort searchRange = reader.ReadUInt16();
            ushort entrySelector = reader.ReadUInt16();
            ushort rangeShift = reader.ReadUInt16();
            int segCount = segCountX2 / 2;
            ushort[] endCounts = reader.ReadUInt16Array(segCount);
            ushort reserved = reader.ReadUInt16();

            ushort[] startCounts = reader.ReadUInt16Array(segCount);
            short[] idDelta = reader.ReadInt16Array(segCount);
            ushort[] idRangeOffset = reader.ReadUInt16Array(segCount);

            // table length thus far
            int headerLength = 16 + (segCount * 8);
            int glyphIdCount = (length - headerLength) / 2;

            ushort[] glyphIds = reader.ReadUInt16Array(glyphIdCount);

            Segment[] segments = Segment.Create(endCounts, startCounts, idDelta, idRangeOffset);
            foreach (EncodingRecord encoding in encodings)
            {
                yield return new Format4SubTable(language, encoding.PlatformID, encoding.EncodingID, segments, glyphIds);
            }
        }

        internal readonly struct Segment
        {
            public Segment(ushort index, ushort end, ushort start, short delta, ushort offset)
            {
                this.Index = index;
                this.End = end;
                this.Start = start;
                this.Delta = delta;
                this.Offset = offset;
            }

            public ushort Index { get; }

            public short Delta { get; }

            public ushort End { get; }

            public ushort Offset { get; }

            public ushort Start { get; }

            public static Segment[] Create(ushort[] endCounts, ushort[] startCode, short[] idDelta, ushort[] idRangeOffset)
            {
                int count = endCounts.Length;
                var segments = new Segment[count];
                for (ushort i = 0; i < count; i++)
                {
                    ushort start = startCode[i];
                    ushort end = endCounts[i];
                    short delta = idDelta[i];
                    ushort offset = idRangeOffset[i];
                    segments[i] = new Segment(i, end, start, delta, offset);
                }

                return segments;
            }
        }
    }
}
