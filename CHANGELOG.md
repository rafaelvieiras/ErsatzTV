﻿# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]
### Fixed
- Cleanly stop all library scans when service termination is requested
- Fix health check crash when trash contains a show or a season
- Fix ability of health check crash to crash home page
- Remove and ignore Season 0/Specials from Plex shows that have no specials

### Changed
- Update Plex, Jellyfin and Emby movie and show library scanners to share a significant amount of code
  - This should help maintain feature parity going forward
- Jellyfin and Emby movie and show library scanners now support the `unavailable` media state
- Optimize search-index rebuilding to complete 100x faster

### Added
- Add `unavailable` state for Emby movie libraries
- Add `height` and `width` to search index for all videos
- Add `season_number` and `episode_number` to search index for all episodes

## [0.5.3-beta] - 2022-04-24
### Fixed
- Cleanly stop Plex library scan when service termination is requested
- Fix bug introduced with 0.5.2-beta that prevented some Plex content from being played
- Fix spammy subtitle error message
- Fix generating blur hashes for song backgrounds in Docker

### Changed
- No longer remove Plex movies and episodes from ErsatzTV when they do not exist on disk
  - Instead, a new `unavailable` media state will be used to indicate this condition
  - After updating mounts, path replacements, etc - a library scan can be used to resolve this state

## [0.5.2-beta] - 2022-04-22
### Fixed
- Fix unlocking libraries when scanning fails for any reason
- Fix software overlay of actual size watermark

### Added
- Add support for burning in embedded and external text subtitles
  - **This requires a one-time full library scan, which may take a long time with large libraries.**
- Sync Plex, Jellyfin and Emby collections as tags on movies, shows, seasons and episodes
  - This allows smart collections that use queries like `tag:"Collection Name"`
  - Note that Emby has an outstanding collections bug that prevents updates when removing items from a collection
- Sync Plex labels as tags on movies and shows
  - This allows smart collections that use queries like `tag:"Plex Label Name"`
- Add `Deep Scan` button for Plex libraries
  - This scanning mode is *slow* but is required to detect some changes like labels

### Changed
- Improve the speed and change detection of the Plex library scanners

## [0.5.1-beta] - 2022-04-17
### Fixed
- Fix subtitles edge case with NVENC
- Only select picture subtitles (text subtitles are not yet supported)
  - Supported picture subtitles are `hdmv_pgs_subtitle` and `dvd_subtitle`
- Fix subtitles using software encoders, videotoolbox, VAAPI
- Fix setting VAAPI driver name
- Fix ffmpeg troubleshooting reports
- Fix bug where filler would behave as if it were configured to pad even though a different mode was selected
- Fix bug where mid-roll count filler would skip scheduling the final chapter in an episode 

### Added
- Add `Empty Trash` button to `Trash` page

## [0.5.0-beta] - 2022-04-13
### Fixed
- Fix `HLS Segmenter` bug where it would drift off of the schedule if a playout was changed while the segmenter was running
- Ensure clients that use HDHomeRun emulation (like Plex) always get an `MPEG-TS` stream, regardless of the configured streaming mode
- Fix scheduling bug that caused some days to be skipped when fixed start times were used with fallback filler

### Added
- Add `Preferred Subtitle Language` and `Subtitle Mode` to channel settings
  - `Preferred Subtitle Language` will filter all subtitle streams based on language
  - `Subtitle Mode` will further filter subtitle streams based on attributes (forced, default)
  - If picture-based subtitles are found after filtering, they will be burned into the video stream
- Detect non-zero ffmpeg exit code from `HLS Segmenter` and `MPEG-TS`, log error output and display error output on stream
- Add `Watermark` setting to schedule items; this allows override the channel watermark. Watermark priority is:
  - Schedule Item
  - Channel
  - Global

### Changed
- Remove legacy transcoder logic option; all channels will use the new transcoder logic
- Renamed channel setting `Preferred Language` to `Preferred Audio Language`
- Reworked playout build logic to maintain collection progress in some scenarios. There are now three build modes:
  - `Continue` - add new items to the end of an existing playout
    - This mode is used when playouts are automatically extended in the background
  - `Refresh` - this mode will try to maintain collection progress while rebuilding the entire playout
    - This mode is used when a schedule is updated, or when collection modifications trigger a playout rebuild
  - `Reset` - this mode will rebuild the entire playout and will NOT maintain progress
    - This mode is only used when the `Reset Playout` button is clicked on the Playouts page
  - **This requires rebuilding all playouts, which will happen on startup after upgrading**
- Use ffmpeg to resize images; this should help reduce ErsatzTV's memory use
- Use ffprobe to check for animated logos and watermarks; this should help reduce ErsatzTV's memory use
- Allow two decimals in channel numbers (e.g. `5.73`)

## [0.4.5-alpha] - 2022-03-29
### Fixed
- Fix streaming mode inconsistencies when `mode` parameter is unspecified
- Fix startup on Windows 7

### Added
- Add option to automatically deinterlace video when transcoding
  - Previously, this was always enabled; the purpose of the option is to allow disabling any deinterlace filters
  - Note that there is no performance gain to disabling the option with progressive content; filters are only ever applied to interlaced content

### Changed
- Change FFmpeg Profile video codec and audio codec text fields to select fields
  - The appropriate video encoder will be determined based on the video format and hardware acceleration selections
- Remove FFmpeg Profile `Transcode`, `Normalize Video` and `Normalize Audio` settings
  - All content will be transcoded and have audio and video normalized
  - The only exception to this rule is `HLS Direct` streaming mode, which directly copies video and audio streams
- Always try to connect to Plex at `http://localhost:32400` even if that address isn't advertised by the Plex API
  - If Plex isn't on the localhost, all other addresses will be checked as with previous releases

## [0.4.4-alpha] - 2022-03-10
### Fixed
- Fix `HLS Direct` streaming mode
- Fix bug with `HLS Segmenter` (and `MPEG-TS`) on Windows that caused errors at program boundaries

### Added
- Perform additional duration analysis on files with missing duration metadata
- Add `nouveau` VAAPI driver option

## [0.4.3-alpha] - 2022-03-05
### Fixed
- Fix song sorting with `Chronological` and `Shuffle In Order` playback orders
- Fix watermark on scaled and/or padded video with NVIDIA acceleration
- Fix playback of interlaced mpeg2video content with NVIDIA acceleration
- Fix playback of all interlaced content with QSV acceleration
- Fix adding songs to collections from search results page
- Fix bug scheduling mid-roll filler with content that contains one chapter
  - No mid-roll filler will be inserted for content with zero or one chapters
- Fix thread sync bug with `HLS Segmenter` (and `MPEG-TS`) streaming modes
- Fix path replacement bug when media server path is left blank

### Added
- Add automated error reporting via Bugsnag
  - This can be disabled by editing the `appsettings.json` file or by setting the `Bugsnag:Enable` environment variable to `false`
- Add `album_artist` to song metadata and to search index
- Display `album_artist` on some song videos when it's different than the `artist`

### Changed
- Framerate normalization will never normalize framerate below 24fps
  - Instead, content with a lower framerate will be normalized up to 24fps
- `Shuffle In Order` will group songs by album artist instead of by track artist

## [0.4.2-alpha] - 2022-02-26
### Fixed
- Add improved but experimental transcoder logic, which can be toggled on and off in `Settings`
- Fix `HLS Segmenter` bug when source video packet contains no duration (`N/A`)
- Fix green line at the bottom of some content scaled using QSV acceleration

### Added
- Add configurable channel group (M3U) and categories (XMLTV)
- Add `Shuffle Schedule Items` option to schedule configuration
  - When this is enabled, schedule items will be shuffled rather than looped in order
  - **To support this, all playouts will be rebuilt (one time) after upgrading to this version**

### Changed
- Disable framerate normalization by default and on all ffmpeg profiles
  - If framerate normalization is desired (not typically needed), it can be re-enabled manually
- Show watermarks over songs
- Hide unused local libraries

## [0.4.1-alpha] - 2022-02-10
### Fixed
- Normalize smart quotes in search queries as they are unsupported by the search library
- Fix incorrect watermark time calculations caused by working ahead in `HLS Segmenter`
- Fix ui crash adding empty path to local library
- Fix ui crash loading collection editor
- Properly flag items as `File Not Found` when local library path (folder) is missing from disk
- Fix playback bug with unknown pixel format
- Fix playback of interlaced mpeg2video on NVIDIA, VAAPI

### Added
- Include `Series` category tag for all episodes in XMLTV
- Include movie, episode (show), music video (artist) genres as `category` tags in XMLTV
- Add framerate normalization to `HLS Segmenter` and `MPEG-TS` streaming modes
- Add `HLS Segmenter Initial Segment Count` setting to allow segmenter to work ahead before allowing client playback

### Changed
- Intermittent watermarks will now fade in and out
- Show collection name in some playout build error messages
- Use hardware-accelerated filter for watermarks on NVIDIA
- Use hardware-accelerated deinterlace for some content on NVIDIA

## [0.4.0-alpha] - 2022-01-29
### Fixed
- Fix m3u `mode` query param to properly override streaming mode for all channels
  - `segmenter` for `HLS Segmenter`
  - `hls-direct` for `HLS Direct`
  - `ts` for `MPEG-TS`
  - `ts-legacy` for `MPEG-TS (Legacy)`
  - omitting the `mode` parameter returns each channel as configured
- Link `File Not Found` health check to `Trash` page to allow deletion
- Fix `HLS Segmenter` streaming mode with multiple ffmpeg-based clients
  - Jellyfin (web) and TiviMate (Android) were specifically tested

### Added
- Hide console window on macOS and Windows; tray menu can be used to access UI, logs and to stop the app
- Also write logs to text files in the `logs` config subfolder
- Add `added_date` to search index
    - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete
- Add `added_inthelast`, `added_notinthelast` search field for relative added date queries
    - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`

## [0.3.8-alpha] - 2022-01-23
### Fixed
- Fix issue preventing some versions of ffmpeg (usually 4.4.x) from streaming MPEG-TS (Legacy) channels at all
  - The issue appears to be caused by using a thread count other than `1`
  - Thread count is now forced to `1` for all streaming modes other than HLS Segmenter
- Fix bug with HLS Segmenter in cultures where `.` is a group/thousands separator
- Fix search results page crashing with some media kinds
- Always use MPEG-TS or MPEG-TS (Legacy) streaming mode with HDHR (Plex)
  - Other configured modes will fall back to MPEG-TS when accessed by Plex

### Changed
- Upgrade ffmpeg from 4.4 to 5.0 in all docker images
    - Upgrading from 4.4 to 5.0 is recommended for all installations

## [0.3.7-alpha] - 2022-01-17
### Fixed
- Fix local folder scanners to properly detect removed/re-added folders with unchanged contents
- Fix double-click startup on mac
- Fix trakt list sync when show does not contain a year
- Properly unlock libraries when a scan is unable to be performed because ffmpeg or ffprobe have not been found

### Added
- Add trash system for local libraries to maintain collection and schedule integrity through media share outages
  - When items are missing from disk, they will be flagged and present in the `Media` > `Trash` page
  - The trash page can be used to permanently remove missing items from the database
  - When items reappear at the expected location on disk, they will be unflagged and removed from the trash
- Add basic Mac hardware acceleration using VideoToolbox

### Changed
- Local libraries only: when items are missing from disk, they will be added to the trash and no longer removed from collections, etc.
- Show song thumbnail in song list

## [0.3.6-alpha] - 2022-01-10
### Fixed
- Properly index `minutes` field when adding new items during scan (vs when rebuilding index)
- Fix some nvenc edge cases where only padding is needed for normalization
- Properly overwrite environment variables for ffmpeg processes (`LIBVA_DRIVER_NAME`, `FFREPORT`)

### Added
- Add music video `artist` to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Remove `HLS Hybrid` streaming mode; all channels have been reconfigured to use the superior `HLS Segmenter` streaming mode
- Update `MPEG-TS` streaming mode to internally use the HLS segmenter
  - This improves compatibility with many clients and also improves performance at program boundaries
- Renamed existing `MPEG-TS` mode as `MPEG-TS (Legacy)`
  - This mode will be removed in a future release

## [0.3.5-alpha] - 2022-01-05
### Fixed
- Fix bundled ffmpeg version in base docker image (NOT nvidia or vaapi) which prevented playback since v0.3.0-alpha
- Use software decoding for mpeg4 content when VAAPI acceleration is enabled
- Fix hardware acceleration health check to recognize QSV on non-Windows platforms

### Changed
- Treat `setsar` as a hardware filter, avoiding unneeded `hwdownload` and `hwupload` steps when padding isn't required

## [0.3.4-alpha] - 2021-12-21
### Fixed
- Fix other video and song scanners to include videos contained directly in top-level folders that are added to a library
- Allow saving ffmpeg troubleshooting reports on Windows

## [0.3.3-alpha] - 2021-12-12
### Fixed
- Fix bug with saving multiple blurhash versions for cover art; all cover art will be automatically rescanned
- Fix song detail margin when no cover art exists and no watermark exists
- Fix synchronizing virtual shows and seasons from Jellyfin
- Properly sort channels in M3U

### Changed
- Use blurhash of ErsatzTV colors instead of solid colors for default song backgrounds
- Use select control instead of autocomplete control in many places
    - The autocomplete control is not intuitive to use and has focus bugs

## [0.3.2-alpha] - 2021-12-03
### Fixed
- Fix artwork upload on Windows
- Fix unicode song metadata on Windows
- Fix unicode console output on Windows
- Fix TV Show NFO metadata processing when `year` is missing
- Fix song detail outline to help legibility on white backgrounds
- Optimize song artwork scanning to prevent re-processing album artwork for each song

### Changed
- Use custom log database backend which should be more portable (i.e. work in osx-arm64)
- Use cover art blurhashes for song backgrounds instead of solid colors or box blur

## [0.3.1-alpha] - 2021-11-30
### Fixed
- Fix song page links in UI
- Show song artist in playout detail
- Include song artist and cover art in channel guide (xmltv)
- Use subtitles to display errors, which fixes many edge cases of unescaped characters
- Properly split song genre tags
- Properly display all songs that have an identical album and title
- Fix channel logo and watermark uploads
- Fix regression introduced with `v0.2.4-alpha` that caused some filler edge cases to crash the playout builder

### Added
- Add song genres to search index
- Use embedded song cover art when sidecar cover art is unavailable

### Changed
- Randomly place song cover art on left or right side of screen
- Randomly use a solid color from the cover art instead of blurred cover art for song background
- Randomly select song detail layout (large title/small artist or small artist/title/album)

## [0.3.0-alpha] - 2021-11-25
### Fixed
- Properly fix database incompatibility introduced with `v0.2.4-alpha` and partially fixed with `v0.2.5-alpha`
  - The proper fix requires rebuilding all playouts, which will happen on startup after upgrading
- Fix local library locking/progress display when adding paths
- Fix grouping duration items in EPG when custom title is configured

### Added
- Add *experimental* `Songs` local libraries
    - Like `Other Videos`, `Songs` require no metadata or particular folder layout, and will have tags added for each containing folder
    - For Example, a song at `rock/band/1990 - Album/01 whatever.flac` will have the tags `rock`, `band` and `1990 - Album`, and the title `01 whatever`
    - Songs will also have basic metadata read from embedded tags (album, artist, title)
    - Video will be automatically generated for songs using metadata and cover art or watermarks if available
- Add support for `.webm` video files

## [0.2.5-alpha] - 2021-11-21
### Fixed
- Include other video title in channel guide (xmltv)
- Fix bug introduced with 0.2.4-alpha that caused some playouts to build from year 0
- Use less memory matching Trakt list items

### Added
- Build osx-arm64 packages on release

### Changed
- No longer warn about local Plex guids; they aren't used for Trakt matching and can be ignored

## [0.2.4-alpha] - 2021-11-13
### Changed
- Upgrade to dotnet 6
- Use `scale_cuda` instead of `scale_npp` for NVIDIA scaling in all cases

## [0.2.3-alpha] - 2021-11-03
### Fixed
- Fix bug with audio filter in cultures where `.` is a group/thousands separator
- Fix bug where flood playout mode would only schedule one item
  - This would happen if the flood was followed by another flood with a fixed start time

### Added
- Support empty `.etvignore` file to instruct local movie scanner to ignore the containing folder

## [0.2.2-alpha] - 2021-10-30
### Fixed
- Fix EPG entries for Duration schedule items that play multiple items
- Fix EPG entries for Multiple schedule items that play more than one item

### Added
- Add fallback filler settings to Channel and global FFmpeg Settings
  - When streaming is attempted during an unscheduled gap, the resulting video will be determined using the following priority:
    - Channel fallback filler
    - Global fallback filler
    - Generated `Channel Is Offline` error message video 

### Changed
- Allow per-episode folders for local show libraries
  - e.g. `Show Name\Season #\Episode #\Show Name - s#e#.mkv`

## [0.2.1-alpha] - 2021-10-24
### Fixed
- Fix saving dynamic start time on schedule items

## [0.2.0-alpha] - 2021-10-23
### Fixed
- Fix generated streams with mpeg2video
- Fix incorrect row count in playout detail table
- Fix deleting movies that have been removed from Jellyfin and Emby
- Fix bug that caused large unscheduled gaps in playouts
  - This was caused by schedule items with a fixed start of midnight

### Added
- Add new filler system
    - `Pre-Roll Filler` plays before each media item
    - `Mid-Roll Filler` plays between media item chapters
    - `Post-Roll Filler` plays after each media item
    - `Tail Filler` plays after all media items, until the next media item
    - `Fallback Filler` loops instead of default offline image to fill any remaining gaps
- Store chapter details with media statistics; this is needed to support mid-roll filler
    - This requires re-ingesting statistics for all media items the first time this version is launched
- Add switch to show/hide filler in playout detail table
- Add `minutes` field to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Change some debug log messages to info so they show by default again
- Remove tail collection options from `Duration` playout mode
- Show localized start time in schedule items tables

## [0.1.5-alpha] - 2021-10-18
### Fixed
- Fix double scheduling; this could happen if the app was shutdown during a playout build
- Fix updating Jellyfin and Emby TV seasons
- Fix updating Jellyfin and Emby artwork
- Fix Plex, Jellyfin, Emby worker crash attempting to sync library that no longer exists
- Fix bug with `Duration` mode scheduling when media items are too long to fit in the requested duration

### Added
- Include music video thumbnails in channel guide (xmltv)

### Changed
- Automatically find working Plex address on startup
- Automatically select schedule item in schedules that contain only one item
- Change default log level from `Debug` to `Information`
  - The `Debug` log level can be enabled in the `appsettings.json` file for non-docker installs
  - The `Debug` log level can be enabled by setting the environment variable `Serilog:MinimumLevel=Debug` for docker installs

## [0.1.4-alpha] - 2021-10-14
### Fixed
- Fix error message/offline stream continuity with channels that use HLS Segmenter
- Fix removing items from search index when folders are removed from local libraries

### Added
- Add `Other Video` local libraries
  - Other video items require no metadata or particular folder layout, and will have tags added for each containing folder
  - For Example, a video at `commercials/sd/1990/whatever.mkv` will have the tags `commercials`, `sd` and `1990`, and the title `whatever`
- Add filler `Tail Mode` option to `Duration` playout mode (in addition to existing `Offline` option)
  - Filler collection will always be randomized (to fill as much time as possible)
  - Filler will be hidden from channel guide, but visible in playout details in ErsatzTV
  - Unfilled time will show offline image
- Add `Guide Mode` option to all schedule items
  - `Normal` guide mode will show all scheduled items in the channel guide (xmltv)
  - `Filler` guide mode will hide all scheduled items from the channel guide, and extend the end time for the previous item in the guide

## [0.1.3-alpha] - 2021-10-13
### Fixed
- Fix startup bug for some docker installations

## [0.1.2-alpha] - 2021-10-12
### Added
- Include more cuda (nvidia) filters in docker image
- Enable deinterlacing with nvidia using new `yadif_cuda` filter
- Add two HLS Segmenter settings: idle timeout and work-ahead limit
  - `HLS Segmenter Idle Timeout` - the number of seconds to keep transcoding a channel while no requests have been received from any client
    - This setting must be greater than or equal to 30 (seconds)
  - `Work-Ahead HLS Segmenter Limit` - the number of segmenters (channels) that will work-ahead simultaneously (if multiple channels are being watched)
    - "working ahead" means transcoding at full speed, which can take a lot of resources
    - This setting must be greater than or equal to 0
- Add more watermark locations ("middle" of each side)
- Add `VAAPI Device` setting to ffmpeg profile to support installations with multiple video cards
- Add *experimental* `RadeonSI` option for `VAAPI Driver` and include mesa drivers in vaapi docker image

### Changed
- Upgrade ffmpeg from 4.3 to 4.4 in all docker images
  - Upgrading from 4.3 to 4.4 is recommended for all installations
- Move `VAAPI Driver` from settings page to ffmpeg profile to support installations with multiple video cards

### Fixed
- Fix some transcoding edge cases with nvidia and pixel format `yuv420p10le`

## [0.1.1-alpha] - 2021-10-10
### Added
- Add music video album to search index
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Changed
- Remove forced initial delay from `HLS Segmenter` streaming mode
- Upgrade nvidia docker image from 18.04 to 20.04

## [0.1.0-alpha] - 2021-10-08
### Added
- Add *experimental* streaming mode `HLS Segmenter` (most similar to `HLS Hybrid`) 
  - This mode is intended to increase client compatibility and reduce issues at program boundaries
  - If you want the temporary transcode files to be located on a particular drive, the docker path is `/root/.local/share/etv-transcode`
- Store frame rate with media statistics; this is needed to support HLS Segmenter
  - This requires re-ingesting statistics for all media items the first time this version is launched

### Changed
- Use latest iHD driver (21.2.3 vs 20.1.1) in vaapi docker images

### Fixed
- Add downsampling to support transcoding 10-bit HEVC content with the h264_vaapi encoder
- Fix updating statistics when media items are replaced
- Fix XMLTV generation when scheduled episode is missing metadata

## [0.0.62-alpha] - 2021-10-05
### Added
- Support IMDB ids from Plex libraries, which may improve Trakt matching for some items

### Fixed
- Include Specials/Season 0 `episode-num` entry in XMLTV
- Fix some transcoding edge cases with VAAPI and pixel formats `yuv420p10le`, `yuv444p10le` and `yuv444p`
- Update Plex movie and episode paths when they are changed within Plex
- Always use `libx264` software encoder for error messages

## [0.0.61-alpha] - 2021-09-30
### Fixed
- Revert nvenc/cuda filter change from v60

## [0.0.60-alpha] - 2021-09-25
### Added
- Add Trakt list support under `Lists` > `Trakt Lists`
  - Trakt lists can be added by url or by `user/list`
  - To re-download a Trakt list, simply add it again (no need to delete)
  - See `Logs` for unmatched item details
  - Trakt lists can only be scheduled by using Smart Collections
- Add seasons to search index
  - This is needed because Trakt lists can contain seasons 
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Fixed
- Fix local television scanner to properly update episode metadata when NFO files have been added/changed
- Properly detect ffmpeg nvenc (cuda) support in Hardware Acceleration health check
- Fix nvenc/cuda filter for some yuv420p content

## [0.0.59-alpha] - 2021-09-18
### Added
- Add `Health Checks` table to home page to identify and surface common misconfigurations
  - `FFmpeg Version` checks `ffmpeg` and `ffprobe` versions
  - `FFmpeg Reports` checks whether ffmpeg troubleshooting reports are enabled since they can use a lot of disk space over time
  - `Hardware Acceleration` checks whether channels that transcode are using acceleration methods that ffmpeg claims to support
  - `Movie Metadata` checks whether all movies have metadata (fallback metadata counts as metadata)
  - `Episode Metadata` checks whether all episodes have metadata (fallback metadata counts as metadata)
  - `Zero Duration` checks whether all movies and episodes have a valid (non-zero) duration
  - `VAAPI Driver` checks whether a vaapi driver preference is configured when using the vaapi docker image
- Add setting to each playout to schedule an automatic daily rebuild
  - This is useful if the playout uses a smart collection with `released_onthisday`

### Fixed
- Fix docker vaapi support for newer Intel platforms (Gen 8+)
  - This includes a new setting to force a particular vaapi driver (`iHD` or `i965`), as some Gen 8 or 9 hardware that is supported by both drivers will perform better with one or the other
- Fix scanning and indexing local movies and episodes without NFO metadata
- Fix displaying seasons for shows with no year (in metadata or in folder name)
- Fix "direct play" in MPEG-TS mode (copy audio and video stream when `Transcode` is unchecked)

## [0.0.58-alpha] - 2021-09-15
### Added
- Add `released_notinthelast` search field for relative release date queries
  - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`
- Add `released_onthisday` search field for historical queries
  - Syntax is `released_onthisday:1` and will search for items released on this month number and day number in prior years
- Add tooltip explaining `Keep Multi-Part Episodes Together`

### Fixed
- Properly display watermark when no other video filters (like scaling or padding) are required
- Fix building some playouts in timezones with positive offsets (like UTC+2)
- Fix `Shuffle In Order` so all collections/shows start from the earliest episode
  - You may need to rebuild playouts to see this fixed behavior more quickly

## [0.0.57-alpha] - 2021-09-10
### Added
- Add `released_inthelast` search field for relative release date queries
  - Syntax is a number and a unit (days, weeks, months, years) like `1 week` or `2 years`
- Allow adding smart collections to multi collections

### Fixed
- Fix loading artwork in Kodi
  - Use fake image extension (`.jpg`) for artwork in M3U and XMLTV since Kodi detects MIME type from URL
  - Enable HEAD requests for IPTV image paths since Kodi requires those

## [0.0.56-alpha] - 2021-09-10
### Added
- Add Smart Collections
  - Smart Collections use search queries and can be created from the search result page
  - Smart Collections are re-evaluated every time playouts are extended or rebuilt to automatically include newly-matching items
  - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete
- Allow `Shuffle In Order` with Collections and Smart Collections
  - Episodes will be grouped by show, and music videos will be grouped by artist
  - All movies will be a single group (multi-collections are probably better if `Shuffle In Order` is desired for movies)
  - All groups will be be ordered chronologically (custom ordering is only supported in multi-collections)

### Fixed
- Generate XMLTV that validates successfully
  - Properly order elements
  - Omit channels with no programmes
  - Properly identify channels using the format number.etv like `15.etv`
- Fix building playouts when multi-part episode grouping is enabled and episodes are missing metadata
- Fix incorrect total items count in `Multi Collections` table

## [0.0.55-alpha] - 2021-09-03
### Fixed
- Fix all local library scanners to ignore dot underscore files (`._`)

## [0.0.54-alpha] - 2021-08-21
### Added
- Add `Shuffle In Order` playback order for multi-collections.
  - This is useful for randomizing multiple collections/shows on a single channel, while each collection maintains proper ordering (custom or chronological)

### Fixed
- Fix bug parsing ffprobe output in cultures where `.` is a group/thousands separator
  - This bug likely prevented ETV from scheduling correctly or working at all in those cultures
  - After installing a version with this fix, affected content will need to be removed from ETV and re-added

## [0.0.53-alpha] - 2021-08-01
### Fixed
- Fix error message displayed after building empty playout
- Fix Emby and Jellyfin links

### Changed
- Always proxy Jellyfin and Emby artwork; this fixes artwork in some networking scenarios

## [0.0.52-alpha] - 2021-07-22
### Added
- Add multiple local libraries to better organize your media
- Add `Move Library Path` function to support reorganizing existing local libraries

### Fixed
- Fix bug preventing playouts from rebuilding after an empty collection is encountered within a multi-collection

## [0.0.51-alpha] - 2021-07-18
### Added
- Add `Multi Collection` to support shuffling multiple collections within a single schedule item
  - Collections within a multi collection are optionally grouped together and ordered when scheduling; this can be useful for franchises
- Add `Playout Days To Build` setting to control how many days of playout data/program guide data should be built into the future

### Changed
- Move `Playback Order` from schedule to schedule items
  - This allows different schedule items to have different playback orders within a single schedule

### Fixed
- Fix release notes on home page with `-alpha` suffix
- Fix linux-arm release by including SQLite interop artifacts
- Fix issue where cached Plex credentials may become invalid when multiple servers are used

## [0.0.50-alpha] - 2021-07-13
### Added
- Add Linux ARM release artifacts which can be used on Raspberry Pi devices

### Fixed
- Fix bug preventing ingestion of local movies with fallback metadata (without NFO files)
- Fix extra spaces in titles of local movies with fallback metadata (without NFO files)

## [0.0.49-prealpha] - 2021-07-11
### Added
- Include audio language metadata in all streaming modes
- Add special zero-count case to `Multiple` playout mode
  - This configuration will automatically maintain the multiple count so that it is equal to the number of items in the collection
  - This configuration should be used if you want to play every item in a collection exactly once before advancing

### Changed
- Use case-insensitive sorting for collections page and `Add to Collection` dialog
- Use case-insensitive sorting for all collection lists in schedule items editor
- Use natural sorting for schedules page and `Add to Schedule` dialog

### Fixed
- Fix flooding schedule items that have a fixed start time

## [0.0.48-prealpha] - 2021-06-22
### Added
- Store pixel format with media statistics; this is needed to support normalization of 10-bit media items
  - This requires re-ingesting statistics for all media items the first time this version is launched

### Changed
- Use ffprobe to retrieve statistics for Plex media items (Local, Emby and Jellyfin libraries already use ffprobe)

### Fixed
- Fix playback of transcoded 10-bit media items (pixel format `yuv420p10le`) on Nvidia hardware
- Emby and Jellyfin scanners now respect library refresh interval setting
- Fix adding new seasons to existing Emby and Jellyfin shows
- Fix adding new episodes to existing Emby and Jellyfin seasons

## [0.0.47-prealpha] - 2021-06-15
### Added
- Add warning during playout rebuild when schedule has been emptied
- Save Logs, Playout Detail, Schedule Detail table page sizes

### Changed
- Show all log entries in log viewer, not just most recent 100 entries
- Use server-side paging and sorting for Logs table
- Use server-side paging for Playout Detail table
- Remove pager from Schedule Items editor (all schedule items will always be displayed)

### Fixed
- Fix ui crash adding a channel without a watermark
- Clear playout detail table when playout is deleted
- Fix blazor error font color
- Fix some audio stream languages missing from UI and search index
- Fix audio stream selection for languages with multiple codes
- Fix searching when queries contain non-ascii characters

## [0.0.46-prealpha] - 2021-06-14
### Added
- Add watermark opacity setting to allow blending with content
- Add global watermark setting; channel-specific watermarks have precedence over global watermarks
- Save Schedules, Playouts table page sizes

### Changed
- Remove unused API and SDK project; may reintroduce in the future but for now they have fallen out of date
- Rework watermarks to be separate from channels (similar to ffmpeg profiles)
  - **All existing watermarks have been removed and will need to be recreated using the new page**
  - This allows easy watermark reuse across channels

### Fixed
- Fix ui crash adding or editing schedule items due to Artist with no name
- Fix many potential sources of inconsistent data in UI

## [0.0.45-prealpha] - 2021-06-12
### Added
- Add experimental `HLS Hybrid` channel mode
  - Media items are transcoded using the channel's ffmpeg profile and served using HLS
- Add optional channel watermark

### Changed
- Remove framerate normalization; it caused more problems than it solved
- Include non-US (and unknown) content ratings in XMLTV

### Fixed
- Fix serving channels.m3u with missing content ratings
- Fix percent progress indicator for Jellyfin and Emby show library scans

## [0.0.44-prealpha] - 2021-06-09
### Added
- Add artists directly to schedules
- Include MPAA and VCHIP content ratings in XMLTV guide data
- Quickly skip missing files during Plex library scan

### Fixed
- Ignore unsupported plex guids (this prevented some libraries from scanning correctly)
- Ignore unsupported STRM files from Jellyfin

## [0.0.43-prealpha] - 2021-06-05
### Added
- Support `(Part #)` name suffixes for multi-part episode grouping
- Support multi-episode files in local and Plex libraries
- Save Channels table page size
- Add optional query string parameter to M3U channel playlist to allow some customization per client
    - `?mode=ts` will force `MPEG-TS` mode for all channels
    - `?mode=hls-direct` will force `HLS Direct` mode for all channels
    - `?mode=mixed` or no parameter will maintain existing behavior

### Changed
- Rename channel mode `TransportStream` to `MPEG-TS` and `HttpLiveStreaming` to `HLS Direct`
- Improve `HLS Direct` mode compatibility with Channels DVR Server

### Fixed
- Fix search result crashes due to missing season metadata

## [0.0.42-prealpha] - 2021-05-31
### Added
- Support roman numerals and english integer names for multi-part episode grouping
- Add option to treat entire collection as a single show with multi-part episode grouping
    - This is useful for multi-part episodes that span multiple shows (crossovers)

### Changed
- Skip zero duration items when building a playout, rather than aborting the playout build

### Fixed
- Fix edge case where a playout rebuild would get stuck and block all other playouts and local library scans

## [0.0.41-prealpha] - 2021-05-30
### Added
- Add button to refresh list of Plex, Jellyfin, Emby libraries without restarting app
- Add episodes to search index
- Add director and writer metadata to episodes
- Add unique id/provider id metadata, which will support future features
- Allow grouping multi-part episodes with titles ending in `Part X`, `Part Y`, etc.

### Changed
- Change home page link from release notes to full changelog

### Fixed
- Fix missing channel logos after restart
- Fix multi-part episode grouping with missing episodes/parts
- Fix multi-part episode grouping in collections containing multiple shows
- Fix updating modified seasons and episodes from Jellyfin and Emby

## [0.0.40-prealpha] - 2021-05-28
### Added
- Add content rating metadata to movies and shows
- Add director and writer metadata to movies
- Sync tv show thumbnail artwork in Local, Jellyfin and Emby libraries (*not* Plex)
- Prioritize tv show thumbnail artwork over tv show posters in XMLTV
- Include tv show artwork in XMLTV when grouped items with custom title are all from the same show
- Cache resized local artwork on disk

### Fixed
- Recursively retrieve Jellyfin and Emby items
- Fix incorrect search item counts
- Fix stack trace information in non-docker releases
- Fix crash opening `Add to Schedule` dialog
- Disable FFmpeg troubleshooting reports on Windows as they do not work properly

## [0.0.39-prealpha] - 2021-05-25
### Added
- Show Jellyfin and Emby artwork in XMLTV

### Fixed
- Fix path replacements for Jellyfin and Emby, including UNC paths
- Use Emby path replacements for playback
- Fix playback when `fps` is the only required filter
- Fix resources (images, fonts) required to display offline channel message

## [0.0.38-prealpha] - 2021-05-23
### Added
- Add support for Emby
- Use "single-file" deployments for releases
    - Non-docker releases will have significantly fewer files
    - It is recommended to empty your installation folder before copying in the latest release.

### Fixed
- Fix some cases where Jellyfin artwork would not display
- Fix saving schedule items with duration less than one hour
- Use ffmpeg 4.3 in docker images; there was a performance regression with 4.4 (only in docker)

## [0.0.37-prealpha] - 2021-05-21
### Added
- Add option to keep multi-part episodes together when shuffling (i.e. two-part season finales)
- Optimize Plex TV Scanner to quickly process shows that have not been updated since the last scan
- Optimize local Movie, Show, Music Video scanners to quickly skip unchanged folders, and to notice any mtime change
- Add server binding configuration to `appsettings.json` which lets non-docker installations bind to localhost or change the port number

### Fixed
- Properly ignore `Other` Jellyfin libraries
- Fix bug where search index would try to re-initialize unnecessarily
- Fix one cause of green line at bottom of some transcoded videos by forcing even scaling targets

## [0.0.36-prealpha] - 2021-05-16
### Added
- Add support for Jellyfin
- Add support for ffmpeg 4.4, and use ffmpeg 4.4 in all docker images
- Add configurable library refresh interval
- Add button to copy/clone ffmpeg profile

## [0.0.35-prealpha] - 2021-04-27
### Added
- Add search button for each library in `Libraries` page to quickly filter content by library
    - This requires rebuilding the search index and search results may be empty or incomplete until the rebuild is complete

### Fixed
- Fix ingesting actors and actor artwork from newly-added Plex media items
- Only show `movie` and `show` libraries from Plex. Other library types are not supported at this time.
- Fix local movie scanner missing replaced/updated files

## [0.0.34-prealpha] - 2021-04-17
### Added
- Allow `enter` key to submit all dialogs
- Add actors to movies and shows (Plex or NFO metadata is required)
    - Note that this requires a one-time full library scan to ingest actor metadata, which may take a long time with large libraries.
- Rework metadata list links in UI (languages, studios, genres, etc)

### Fixed
- Fix EPG generation with music video channels that do not use a custom title
- Fix lag when typing in search bar, `Add To Collection` dialog
- Fix collections paging
- Fix padding odd resolutions (this bug caused some items to always fail playback)
- Only update Plex episode artwork as needed

## [0.0.33-prealpha] - 2021-04-11
### Added
- Add language buttons to movies, shows, artists
- Show release notes on home page

### Fixed
- Re-import missing television metadata that was incorrectly removed with `v0.0.32`
- Fix language indexing; language searches now use full english name
- Fix synchronizing television studios, genres from Plex
- Limit channels to one playout per channel
    - Though more than one playout was previously possible it was unsupported and unlikely to work as expected, if at all
    - A future release may make this possible again
    
## [0.0.32-prealpha] - 2021-04-09
### Added
- `Add All To Collection` button to quickly add all search results to a collection
- Add Artists scanned from Music Video libraries
    - Artist folders are now required, but music videos now have no naming requirements
    - `artist.nfo` metadata is supported along with thumbnail and poster artwork
- Save Collections table page size in local storage

### Fixed
- Fix audio stream language indexing for movies and music videos
- Fix synchronizing list of Plex servers and connection addresses for each server
- Fix `See All` link for music video search results

## [0.0.31-prealpha] - 2021-04-06
### Added
- Add documentation link to UI
- Add `language` search field
- Minor log viewer improvements
- Use fragment navigation with letter bar (clicking a letter will page and scroll until that letter is in view)
- Send all audio streams with HLS when channel has no preferred language
- Move FFmpeg settings to new `Settings` page
- Add HDHR tuner count setting to new `Settings` page

### Fixed
- Fix poster width
- Fix bug that would occasionally prevent items from being added to the search index
- Automatically refresh the Plex Media Sources page after sign in or sign out

## 0.0.30-prealpha [YANKED]

## [0.0.29-prealpha] - 2021-04-04
- No longer require NFO metadata for music videos
    - Instead, the only requirement is that music video files be named `[artist] - [track].[extension]` where the three characters (space dash space) between artist and track are required
- Add library scan progress detail
- Optimize library scans after adding library path to only scan new library path
- Fix bug replacing music videos
- Scan Plex libraries and local libraries on different threads
- Use English names for preferred languages in UI instead of ISO language code

## [0.0.28-prealpha] - 2021-04-03
- Apply audio normalization more consistently; this should further reduce program boundary errors
- Replace unused audio volume setting with audio loudness normalization option
    - This can be particularly helpful with music video channels if media items have inconsistent loudness
    - This setting may be less desirable on movie channels where loudness is intended to be dynamic
- Fix XMLTV containing music videos that do not use a custom title
- Fix channels table sorting, add paging to channels table
- Add sorting and paging to schedules table
- Add paging to playouts table
- Use table instead of cards for collections view

## [0.0.27-prealpha] - 2021-04-02
- Add ***basic*** music video library support
    - **NFO metadata is required for music videos** - see [tags](https://kodi.wiki/view/NFO_files/Music_videos#Music_Video_Tags), [template](https://kodi.wiki/view/NFO_files/Music_videos#Template_nfo) and [sample](https://kodi.wiki/view/NFO_files/Music_videos#Sample_nfo)
    - Artists can be searched using the `artist` field, like `artist:daft`
- Clear search query when clicking `Movies` or `TV Shows` from paged search results
- Add show title to playout details
- Let ffmpeg determine thread count by default (signified by `0` threads in ffmpeg profile)
- Save troubleshooting reports for ffmpeg concat process in addition to transcode process
- Simplify ffmpeg normalization options
- Add frame rate setting to ffmpeg profile
    - When video normalization is enabled, all media items will have their frame rate converted to the same value
- Fix some scenarios where streaming would freeze at program boundaries
- Fix bug preventing some Plex libraries from scanning
- Fix bug preventing some local libraries from scanning folders that were recently added

## [0.0.26-prealpha] - 2021-03-30
- Add `Custom Title` option to schedule items
    - When a custom title is set, the schedule item will be grouped in the EPG with the custom title
- Navigate to schedule items after creating new schedule
- Fix channel editor so preferred language is no longer required on every channel
- Fix bug with audio track selection during non-normalized playback
- Fix bug with playout builds where `Multiple` or `Duration` items wouldn't respect the settings over time
- Fix bug that prevented some television folders from scanning

## [0.0.25-prealpha] - 2021-03-29
- Add preferred language feature
    - Global preference can be set in FFmpeg settings; channels can override global preference
    - Preferences require [ISO 639-2](https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes) language codes
    - Audio stream selection will attempt to respect these preferences and prioritize streams with the most channels
    - English (`eng`) will be used as a fallback when no preferences have been configured
    - ***This feature requires a one-time reanalysis of every media item which may take a long time for large libraries and playback may fail until this scan has completed***
- Fix channel sorting in EPG
- Fix mixed-platform path replacements (Plex on Windows with ErsatzTV on Linux, or Plex on Linux with ErsatzTV on Windows)
- Fix local television library scanning; this was broken with `v0.0.23`
- Optimize local library scanning; regular scans should be significantly faster
- Add log warning when a zero-duration media item is encountered
- Fix indexing local shows without NFO metadata.
    - If you have this issue the best way to fix is to:
        - Shutdown ErsatzTV
        - Delete the `search-index` subfolder inside the ErsatzTV config folder
        - Start ErsatzTV; the full search index will be rebuilt on startup
- Fix updating search index when genres, tags, studios are updated in local libraries
- Adjust artwork routes so all IPTV traffic can be proxied with a single path prefix of `/iptv`

## [0.0.24-prealpha] - 2021-03-22
- Fix a critical bug preventing library synchronization with Plex sign ins performed with `v0.0.22` or `v0.0.23`
    - **If you are unable to sync libraries from Plex, please sign out and back in to apply this fix**
- Fallback to `folder.jpg` when `poster.jpg` is not present
- Attach episodes to correct show when adding NFO metadata to existing libraries

## [0.0.23-prealpha] - 2021-03-21
- Remove all Plex items from search index after sign out
- Fix fallback metadata for local episodes (episode number was missing)
- Improve television show year detection where year is missing from nfo metadata
- Fix sorting for titles that start with `A` or `An` in addition to `The`
- Properly escape search links containing special characters (genre, tag)
- Add and index `Studio` metadata

## [0.0.22-prealpha] - 2021-03-20
- Log errors encountered during search index build; attempt to continue with partial index when errors are encountered
- Only search `title` field by default; `genre` and `tag` can be searched with `field:query` syntax
- Allow leading wildcards in searches
- Keep search query in search field to allow easy modification
- Fix default ffmpeg profile when creating new channels
- Fix multiple bugs with updating Plex servers, libraries, path replacements
- Add `release_date` to search index

## [0.0.21-prealpha] - 2021-03-20
- Optimize local library scanning to use less memory
- Duplicate some documentation near the schedule item editor
- Fix bug with updating `Normalize Video Codec` setting
- Rework search functionality
    - Search landing page will show up to 50 items of each type
    - `See All` links can be used to page through all search results
    - Complex search queries supported (`christmas OR santa`)
    - Fields that are searched by default:
        - `title`
        - `genre`
        - `tag`
    - Fields that aren't searched by default, but can be included in queries with syntax like (`plot:whatever`):
        - `plot`
        - `library_name`
        - `type` (`movie` or `show`)
    - Add letter bar to all paged search results to quickly navigate to a particular letter

## [0.0.20-prealpha] - 2021-03-17
- Fix NVIDIA hardware acceleration in `develop-nvidia` and `latest-nvidia` Docker tags
    - This may never have worked correctly in Docker with older releases
- Fix occasional crash rebuilding playout from ui
- Fix crash adding a channel when no channels exist
- Fix playback for media containing attached pictures

## [0.0.19-prealpha] - 2021-03-16
- Regularly scan Plex libraries (same as local libraries)
- Add ability to create new collection from `Add to Collection` dialog
- Fix channel logos in XMLTV
- Add episode posters (show posters) to XMLTV
- Fix shuffled schedules from occasionally having repeated items when reshuffling
    - This was more likely to happen with low-cardinality collections like A B C C A B B C A
- Add optional FFmpeg troubleshooting reports
- Allow synchronizing hidden Plex libraries

## [0.0.18-prealpha] - 2021-03-14
- Plex is now a supported media source
    - Plex is **not** used for transcoding at this point, files are played directly from the filesystem using ErsatzTV transcoding
    - Path replacements will be needed if your shared media folders are mounted differently in Plex and ErsatzTV

## [0.0.17-prealpha] - 2021-03-13
- Fix bug introduced with 0.0.16 that prevented some playouts from building
- Properly set sort title on added tv shows
- Fix loading season pages containing episodes that have incomplete metadata
- Improve XMLTV guide data

## [0.0.16-prealpha] - 2021-03-12
- Fix infinite loop caused by incorrectly configured ffprobe path
- Add more strict ffmpeg and ffprobe settings validation
- Add custom playback order option to collections that contain only movies
    - This custom playback order will override the schedule's configured playback order for the collection

## [0.0.15-prealpha] - 2021-03-11
- Update UI for tv shows
- Fix tv show sorting
- Fix editing channel numbers
- Fix playout timezone bugs
- Add searchable genres and tags from local NFO metadata
- Add multi-select feature to movies, shows, search results and collection items pages

## [0.0.14-prealpha] - 2021-03-09
- New movie layout utilizing fan art (if available)
- New dark UI
- Fix offline stream (displayed when no media is scheduled for playback)
- Add M3U codec hints for Channels DVR
- Allow sub-channel numbers
- Fix bug where ffmpeg wouldn't terminate after a media item completed playback
- Fix time zone in new docker base images
- Fix vaapi pipeline with mpeg4 content by using software decoder with hardware encoder
- Enforce unique schedule name
- Enforce unique channel number
- Fix sorting of collection items in UI

## [0.0.13-prealpha] - 2021-03-07
- Remember selected Collection in `Add To Collection` dialog
- Automatically rebuild Playouts referencing any Collection that has items added or removed from the UI
- Remove Media Items from database when files are removed from disk
- Add hardware-accelerated transcoding support (`qsv`, `nvenc`/`nvidia`, `vaapi`)
    - All flavors support resolution normalization (scaling and padding)
    - This requires support within ffmpeg; see README for new docker image tags

## [0.0.12-prealpha] - 2021-03-02
- Fix a database migration issue introduced with version 0.0.11
- Shutdown app when database migration failures are encountered at startup

## [0.0.11-prealpha] - 2021-03-01
- Add Libraries and Library Paths under Media Sources
    - Two local libraries exist: `Movies` and `Shows`
    - Local Media Sources from prior versions are now found under Library Paths
- Add `Rebuild Playout` buttons to quickly regenerate playouts after modifying collections
- Add `Add to Collection` buttons to most media cards (movies, shows, seasons, episodes)
- Add Search page for searching movies and shows

## [0.0.10-prealpha] - 2021-02-21
- Rework how television media is stored in the database
- Rework how media is linked to a collection
- Add season, episode and movie detail views to UI
- Add media to collections and schedules from detail views
- Easily add and remove media from a collection
- Easily add and reorder schedule items

## [0.0.9-prealpha] - 2021-02-15
- Local media scanner has been rewritten and is much more performant
- Ignore extras in the same folder as movies (`-behindthescenes`, `-trailer`, etc)
- Support `movie.nfo` metadata in addition to matching filename nfo metadata
- Changes to video files, metadata and posters are automatically detected and used

## [0.0.8-prealpha] - 2021-02-14
- Optimize scanning so playouts are only rebuilt when necessary (duration changes, or collection membership changes)
- Automatically add new posters during scanning
- Support more poster file types (jpg, jpeg, png, gif, tbn)
- Add "Refresh All Metadata" button to media sources page; this should only be needed if NFO metadata or posters are modified
- Add progress indicator for media sources that are being actively scanned
- Prevent deleting media source during scan
- Prevent creating playout with empty schedule

## [0.0.7-prealpha] - 2021-02-13
- Rework media items layout - table has been replaced with cards/posters
- Fix bug preventing long folder names from being used as media sources
- Use 24h time pickers in schedule editor

## [0.0.6-prealpha] - 2021-02-12
- Add version information to UI
- Add basic log viewer to UI

## [0.0.5-prealpha] - 2021-02-12
- Fix bug where media scanner could stop prematurely and miss media items
- Add database migrations

## [0.0.4-prealpha] - 2021-02-11
- **Fix HDHomeRun routes** - this version is required to use as a DVR with Plex, older versions will not work
- Improve metadata parsing for tv, add fallback (filename) parsing for movies

## [0.0.3-prealpha] - 2021-02-11
- Fix incomplete XML issue introduced with v0.0.2-prealpha
- Add `.ts` files to local media scanner
- Change M3U, XMLTV, API icons to text links

## 0.0.2-prealpha - 2021-02-11 [YANKED]
- Relax some searches to be case-insensitive
- Improve categorization of tv episodes without sidecar metadata
- Properly escape XML content in XMLTV

## [0.0.1-prealpha] - 2021-02-10
- Initial release to facilitate testing outside of Docker.


[Unreleased]: https://github.com/jasongdove/ErsatzTV/compare/v0.5.3-beta...HEAD
[0.5.3-beta]: https://github.com/jasongdove/ErsatzTV/compare/v0.5.2-beta...v0.5.3-beta
[0.5.2-beta]: https://github.com/jasongdove/ErsatzTV/compare/v0.5.1-beta...v0.5.2-beta
[0.5.1-beta]: https://github.com/jasongdove/ErsatzTV/compare/v0.5.0-beta...v0.5.1-beta
[0.5.0-beta]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.5-alpha...v0.5.0-beta
[0.4.5-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.4-alpha...v0.4.5-alpha
[0.4.4-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.3-alpha...v0.4.4-alpha
[0.4.3-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.2-alpha...v0.4.3-alpha
[0.4.2-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.1-alpha...v0.4.2-alpha
[0.4.1-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.4.0-alpha...v0.4.1-alpha
[0.4.0-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.8-alpha...v0.4.0-alpha
[0.3.8-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.7-alpha...v0.3.8-alpha
[0.3.7-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.6-alpha...v0.3.7-alpha
[0.3.6-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.5-alpha...v0.3.6-alpha
[0.3.5-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.4-alpha...v0.3.5-alpha
[0.3.4-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.3-alpha...v0.3.4-alpha
[0.3.3-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.2-alpha...v0.3.3-alpha
[0.3.2-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.1-alpha...v0.3.2-alpha
[0.3.1-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.3.0-alpha...v0.3.1-alpha
[0.3.0-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.5-alpha...v0.3.0-alpha
[0.2.5-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.4-alpha...v0.2.5-alpha
[0.2.4-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.3-alpha...v0.2.4-alpha
[0.2.3-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.2-alpha...v0.2.3-alpha
[0.2.2-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.1-alpha...v0.2.2-alpha
[0.2.1-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.2.0-alpha...v0.2.1-alpha
[0.2.0-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.5-alpha...v0.2.0-alpha
[0.1.5-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.4-alpha...v0.1.5-alpha
[0.1.4-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.3-alpha...v0.1.4-alpha
[0.1.3-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.2-alpha...v0.1.3-alpha
[0.1.2-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.1-alpha...v0.1.2-alpha
[0.1.1-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.1.0-alpha...v0.1.1-alpha
[0.1.0-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.62-alpha...v0.1.0-alpha
[0.0.62-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.61-alpha...v0.0.62-alpha
[0.0.61-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.60-alpha...v0.0.61-alpha
[0.0.60-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.59-alpha...v0.0.60-alpha
[0.0.59-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.58-alpha...v0.0.59-alpha
[0.0.58-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.57-alpha...v0.0.58-alpha
[0.0.57-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.56-alpha...v0.0.57-alpha
[0.0.56-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.55-alpha...v0.0.56-alpha
[0.0.55-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.54-alpha...v0.0.55-alpha
[0.0.54-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.53-alpha...v0.0.54-alpha
[0.0.53-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.52-alpha...v0.0.53-alpha
[0.0.52-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.51-alpha...v0.0.52-alpha
[0.0.51-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.50-alpha...v0.0.51-alpha
[0.0.50-alpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.49-prealpha...v0.0.50-alpha
[0.0.49-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.48-prealpha...v0.0.49-prealpha
[0.0.48-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.47-prealpha...v0.0.48-prealpha
[0.0.47-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.46-prealpha...v0.0.47-prealpha
[0.0.46-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.45-prealpha...v0.0.46-prealpha
[0.0.45-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.44-prealpha...v0.0.45-prealpha
[0.0.44-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.43-prealpha...v0.0.44-prealpha
[0.0.43-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.42-prealpha...v0.0.43-prealpha
[0.0.42-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.41-prealpha...v0.0.42-prealpha
[0.0.41-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.40-prealpha...v0.0.41-prealpha
[0.0.40-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.39-prealpha...v0.0.40-prealpha
[0.0.39-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.38-prealpha...v0.0.39-prealpha
[0.0.38-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.37-prealpha...v0.0.38-prealpha
[0.0.37-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.36-prealpha...v0.0.37-prealpha
[0.0.36-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.35-prealpha...v0.0.36-prealpha
[0.0.35-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.34-prealpha...v0.0.35-prealpha
[0.0.34-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.33-prealpha...v0.0.34-prealpha
[0.0.33-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.32-prealpha...v0.0.33-prealpha
[0.0.32-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.31-prealpha...v0.0.32-prealpha
[0.0.31-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.29-prealpha...v0.0.31-prealpha
[0.0.29-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.28-prealpha...v0.0.29-prealpha
[0.0.28-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.27-prealpha...v0.0.28-prealpha
[0.0.27-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.26-prealpha...v0.0.27-prealpha
[0.0.26-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.25-prealpha...v0.0.26-prealpha
[0.0.25-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.24-prealpha...v0.0.25-prealpha
[0.0.24-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.23-prealpha...v0.0.24-prealpha
[0.0.23-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.22-prealpha...v0.0.23-prealpha
[0.0.22-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.21-prealpha...v0.0.22-prealpha
[0.0.21-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.20-prealpha...v0.0.21-prealpha
[0.0.20-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.19-prealpha...v0.0.20-prealpha
[0.0.19-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.18-prealpha...v0.0.19-prealpha
[0.0.18-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.17-prealpha...v0.0.18-prealpha
[0.0.17-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.16-prealpha...v0.0.17-prealpha
[0.0.16-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.15-prealpha...v0.0.16-prealpha
[0.0.15-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.14-prealpha...v0.0.15-prealpha
[0.0.14-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.13-prealpha...v0.0.14-prealpha
[0.0.13-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.12-prealpha...v0.0.13-prealpha
[0.0.12-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.11-prealpha...v0.0.12-prealpha
[0.0.11-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.10-prealpha...v0.0.11-prealpha
[0.0.10-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.9-prealpha...v0.0.10-prealpha
[0.0.9-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.8-prealpha...v0.0.9-prealpha
[0.0.8-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.7-prealpha...v0.0.8-prealpha
[0.0.7-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.6-prealpha...v0.0.7-prealpha
[0.0.6-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.5-prealpha...v0.0.6-prealpha
[0.0.5-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.4-prealpha...v0.0.5-prealpha
[0.0.4-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.3-prealpha...v0.0.4-prealpha
[0.0.3-prealpha]: https://github.com/jasongdove/ErsatzTV/compare/v0.0.1-prealpha...v0.0.3-prealpha
[0.0.1-prealpha]: https://github.com/jasongdove/ErsatzTV/releases/tag/v0.0.1-prealpha