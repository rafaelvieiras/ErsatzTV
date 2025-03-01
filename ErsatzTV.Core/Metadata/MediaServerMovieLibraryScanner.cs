﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public abstract class MediaServerMovieLibraryScanner<TConnectionParameters, TLibrary, TMovie, TEtag>
    where TConnectionParameters : MediaServerConnectionParameters
    where TLibrary : Library
    where TMovie : Movie
    where TEtag : MediaServerItemEtag
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    protected MediaServerMovieLibraryScanner(
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalFileSystem localFileSystem,
        IMediator mediator,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        ILogger logger)
    {
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _localFileSystem = localFileSystem;
        _mediator = mediator;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _logger = logger;
    }

    protected async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TMovie, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            Either<BaseError, List<TMovie>> entries = await GetMovieLibraryItems(connectionParameters, library);

            foreach (BaseError error in entries.LeftToSeq())
            {
                return error;
            }

            return await ScanLibrary(
                movieRepository,
                connectionParameters,
                library,
                getLocalPath,
                ffmpegPath,
                ffprobePath,
                entries.RightToSeq().Flatten().ToList(),
                deepScan,
                cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
        finally
        {
            _searchIndex.Commit();
        }
    }

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TMovie, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        List<TMovie> movieEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<TEtag> existingMovies = await movieRepository.GetExistingMovies(library);

        var sortedMovies = movieEntries.OrderBy(m => m.MovieMetadata.Head().SortTitle).ToList();
        foreach (TMovie incoming in sortedMovies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)sortedMovies.IndexOf(incoming) / sortedMovies.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            string localPath = getLocalPath(incoming);

            if (await ShouldScanItem(movieRepository, library, existingMovies, incoming, localPath, deepScan) == false)
            {
                continue;
            }

            Either<BaseError, MediaItemScanResult<TMovie>> maybeMovie = await movieRepository
                .GetOrAdd(library, incoming)
                .MapT(
                    result =>
                    {
                        result.LocalPath = localPath;
                        return result;
                    })
                .BindT(existing => UpdateMetadata(connectionParameters, library, existing, incoming, deepScan))
                .BindT(existing => UpdateStatistics(existing, incoming, ffmpegPath, ffprobePath))
                .BindT(UpdateSubtitles);

            if (maybeMovie.IsLeft)
            {
                foreach (BaseError error in maybeMovie.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing movie {Title}: {Error}",
                        incoming.MovieMetadata.Head().Title,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TMovie> result in maybeMovie.RightToSeq())
            {
                await movieRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (_localFileSystem.FileExists(result.LocalPath))
                {
                    if (await movieRepository.FlagNormal(library, result.Item))
                    {
                        result.IsUpdated = true;
                    }
                }
                else
                {
                    Option<int> flagResult = await movieRepository.FlagUnavailable(library, result.Item);
                    if (flagResult.IsSome)
                    {
                        result.IsUpdated = true;
                    }
                }

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash movies that are no longer present on the media server
        var fileNotFoundItemIds = existingMovies.Map(m => m.MediaServerItemId)
            .Except(movieEntries.Map(MediaServerItemId)).ToList();
        List<int> ids = await movieRepository.FlagFileNotFound(library, fileNotFoundItemIds);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);

        return Unit.Default;
    }

    protected abstract string MediaServerItemId(TMovie movie);
    protected abstract string MediaServerEtag(TMovie movie);

    protected abstract Task<Either<BaseError, List<TMovie>>> GetMovieLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract Task<Option<MovieMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateMetadata(
        MediaItemScanResult<TMovie> result,
        MovieMetadata fullMetadata);

    private async Task<bool> ShouldScanItem(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TLibrary library,
        List<TEtag> existingMovies,
        TMovie incoming,
        string localPath,
        bool deepScan)
    {
        // deep scan will always pull every movie
        if (deepScan)
        {
            return true;
        }

        Option<TEtag> maybeExisting =
            existingMovies.Find(m => m.MediaServerItemId == MediaServerItemId(incoming));
        string existingItemId = await maybeExisting.Map(e => e.MediaServerItemId).IfNoneAsync(string.Empty);
        MediaItemState existingState = await maybeExisting.Map(e => e.State).IfNoneAsync(MediaItemState.Normal);

        if (existingState == MediaItemState.Unavailable)
        {
            // skip scanning unavailable items that still don't exist locally
            if (!_localFileSystem.FileExists(localPath))
            {
                return false;
            }
        }
        else if (existingItemId == MediaServerItemId(incoming))
        {
            // item is unchanged, but file does not exist
            // don't scan, but mark as unavailable
            if (!_localFileSystem.FileExists(localPath))
            {
                foreach (int id in await movieRepository.FlagUnavailable(library, incoming))
                {
                    await _searchIndex.RebuildItems(_searchRepository, new List<int> { id });
                }
            }

            return false;
        }

        if (maybeExisting.IsNone)
        {
            _logger.LogDebug("INSERT: new movie {Movie}", incoming.MovieMetadata.Head().Title);
        }
        else
        {
            _logger.LogDebug("UPDATE: Etag has changed for movie {Movie}", incoming.MovieMetadata.Head().Title);
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan)
    {
        foreach (MovieMetadata fullMetadata in await GetFullMetadata(
                     connectionParameters,
                     library,
                     result,
                     incoming,
                     deepScan))
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateStatistics(
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        string ffmpegPath,
        string ffprobePath)
    {
        TMovie existing = result.Item;

        if (result.IsAdded || MediaServerItemId(existing) != MediaServerItemId(incoming) ||
            existing.MediaVersions.Head().Streams.Count == 0)
        {
            if (_localFileSystem.FileExists(result.LocalPath))
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", result.LocalPath);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(
                        ffmpegPath,
                        ffprobePath,
                        existing,
                        result.LocalPath);

                foreach (BaseError error in refreshResult.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                        "Statistics",
                        result.LocalPath,
                        error.Value);
                }

                foreach (bool _ in refreshResult.RightToSeq())
                {
                    result.IsUpdated = true;
                }
            }
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateSubtitles(
        MediaItemScanResult<TMovie> existing)
    {
        try
        {
            // skip checking subtitles for files that don't exist locally
            if (!_localFileSystem.FileExists(existing.LocalPath))
            {
                return existing;
            }

            if (await _localSubtitlesProvider.UpdateSubtitles(existing.Item, existing.LocalPath, false))
            {
                return existing;
            }

            return BaseError.New("Failed to update local subtitles");
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
