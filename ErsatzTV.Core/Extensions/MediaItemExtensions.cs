﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;

namespace ErsatzTV.Core.Extensions;

public static class MediaItemExtensions
{
    public static MediaVersion GetHeadVersion(this MediaItem mediaItem) =>
        mediaItem switch
        {
            Movie m => m.MediaVersions.Head(),
            Episode e => e.MediaVersions.Head(),
            MusicVideo mv => mv.MediaVersions.Head(),
            OtherVideo ov => ov.MediaVersions.Head(),
            Song s => s.MediaVersions.Head(),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
        };

    public static async Task<string> GetLocalPath(
        this MediaItem mediaItem,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        bool log = true)
    {
        MediaVersion version = mediaItem.GetHeadVersion();

        MediaFile file = version.MediaFiles.Head();
        string path = file.Path;
        return mediaItem switch
        {
            PlexMovie plexMovie => await plexPathReplacementService.GetReplacementPlexPath(
                plexMovie.LibraryPathId,
                path,
                log),
            PlexEpisode plexEpisode => await plexPathReplacementService.GetReplacementPlexPath(
                plexEpisode.LibraryPathId,
                path,
                log),
            JellyfinMovie jellyfinMovie => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinMovie.LibraryPathId,
                path,
                log),
            JellyfinEpisode jellyfinEpisode => await jellyfinPathReplacementService.GetReplacementJellyfinPath(
                jellyfinEpisode.LibraryPathId,
                path,
                log),
            EmbyMovie embyMovie => await embyPathReplacementService.GetReplacementEmbyPath(
                embyMovie.LibraryPathId,
                path,
                log),
            EmbyEpisode embyEpisode => await embyPathReplacementService.GetReplacementEmbyPath(
                embyEpisode.LibraryPathId,
                path,
                log),
            _ => path
        };
    }
}
