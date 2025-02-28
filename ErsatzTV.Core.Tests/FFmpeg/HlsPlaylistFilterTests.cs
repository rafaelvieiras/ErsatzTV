﻿using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ErsatzTV.Core.Tests.FFmpeg;

[TestFixture]
public class HlsPlaylistFilterTests
{
    [SetUp]
    public void SetUp() =>
        _hlsPlaylistFilter = new HlsPlaylistFilter(
            new Mock<ITempFilePool>().Object,
            new Mock<ILogger<HlsPlaylistFilter>>().Object
        );

    private HlsPlaylistFilter _hlsPlaylistFilter;

    [Test]
    public void _hlsPlaylistFilter_ShouldRewriteProgramDateTime()
    {
        var start = new DateTimeOffset(2021, 10, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        string[] input = NormalizeLineEndings(
            @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:49.320-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:53.320-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:57.320-0500
live001139.ts").Split(Environment.NewLine);

        TrimPlaylistResult result = _hlsPlaylistFilter.TrimPlaylist(start, start.AddSeconds(-30), input);

        result.PlaylistStart.Should().Be(start);
        result.Sequence.Should().Be(1137);
        result.Playlist.Should().Be(
            NormalizeLineEndings(
                @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-DISCONTINUITY-SEQUENCE:0
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:00.000-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:04.000-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:08.000-0500
live001139.ts
"));
    }

    [Test]
    public void _hlsPlaylistFilter_ShouldLimitSegments()
    {
        var start = new DateTimeOffset(2021, 10, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        string[] input = NormalizeLineEndings(
            @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:49.320-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:53.320-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:57.320-0500
live001139.ts").Split(Environment.NewLine);

        TrimPlaylistResult result = _hlsPlaylistFilter.TrimPlaylist(start, start.AddSeconds(-30), input, 2);

        result.PlaylistStart.Should().Be(start);
        result.Sequence.Should().Be(1137);
        result.Playlist.Should().Be(
            NormalizeLineEndings(
                @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-DISCONTINUITY-SEQUENCE:0
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:00.000-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:04.000-0500
live001138.ts
"));
    }

    [Test]
    public void _hlsPlaylistFilter_ShouldAddDiscontinuity()
    {
        var start = new DateTimeOffset(2021, 10, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        string[] input = NormalizeLineEndings(
            @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:49.320-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:53.320-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:57.320-0500
live001139.ts").Split(Environment.NewLine);

        TrimPlaylistResult result = _hlsPlaylistFilter.TrimPlaylist(
            start,
            start.AddSeconds(-30),
            input,
            int.MaxValue,
            true);

        result.PlaylistStart.Should().Be(start);
        result.Sequence.Should().Be(1137);
        result.Playlist.Should().Be(
            NormalizeLineEndings(
                @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-DISCONTINUITY-SEQUENCE:0
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:00.000-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:04.000-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:08.000-0500
live001139.ts
#EXT-X-DISCONTINUITY
"));
    }

    [Test]
    public void _hlsPlaylistFilter_ShouldFilterOldSegments()
    {
        var start = new DateTimeOffset(2021, 10, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        string[] input = NormalizeLineEndings(
            @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:49.320-0500
live001137.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:53.320-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:57.320-0500
live001139.ts").Split(Environment.NewLine);

        TrimPlaylistResult result = _hlsPlaylistFilter.TrimPlaylist(start, start.AddSeconds(6), input);

        result.PlaylistStart.Should().Be(start.AddSeconds(8));
        result.Sequence.Should().Be(1139);
        result.Playlist.Should().Be(
            NormalizeLineEndings(
                @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1139
#EXT-X-DISCONTINUITY-SEQUENCE:0
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:08.000-0500
live001139.ts
"));
    }

    [Test]
    public void _hlsPlaylistFilter_ShouldFilterOldDiscontinuity()
    {
        var start = new DateTimeOffset(2021, 10, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        string[] input = NormalizeLineEndings(
            @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1137
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:49.320-0500
live001137.ts
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:53.320-0500
live001138.ts
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-08T08:34:57.320-0500
live001139.ts").Split(Environment.NewLine);

        TrimPlaylistResult result = _hlsPlaylistFilter.TrimPlaylist(start, start.AddSeconds(6), input);

        result.PlaylistStart.Should().Be(start.AddSeconds(8));
        result.Sequence.Should().Be(1139);
        result.Playlist.Should().Be(
            NormalizeLineEndings(
                @"#EXTM3U
#EXT-X-VERSION:6
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:1139
#EXT-X-DISCONTINUITY-SEQUENCE:1
#EXT-X-INDEPENDENT-SEGMENTS
#EXT-X-DISCONTINUITY
#EXTINF:4.000000,
#EXT-X-PROGRAM-DATE-TIME:2021-10-09T08:00:08.000-0500
live001139.ts
"));
    }

    private static string NormalizeLineEndings(string str) =>
        str
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", Environment.NewLine);
}
