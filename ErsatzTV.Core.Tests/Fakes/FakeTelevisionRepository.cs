﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Tests.Fakes;

public class FakeTelevisionRepository : ITelevisionRepository
{
    public Task<bool> AllShowsExist(List<int> showIds) => throw new NotSupportedException();
    public Task<bool> AllSeasonsExist(List<int> seasonIds) => throw new NotSupportedException();

    public Task<bool> AllEpisodesExist(List<int> episodeIds) => throw new NotSupportedException();

    public Task<List<Show>> GetAllShows() => throw new NotSupportedException();

    public Task<Option<Show>> GetShow(int showId) => throw new NotSupportedException();

    public Task<List<ShowMetadata>> GetShowsForCards(List<int> ids) => throw new NotSupportedException();
    public Task<List<SeasonMetadata>> GetSeasonsForCards(List<int> ids) => throw new NotSupportedException();

    public Task<List<EpisodeMetadata>> GetEpisodesForCards(List<int> ids) => throw new NotSupportedException();

    public Task<List<Episode>> GetShowItems(int showId) => throw new NotSupportedException();

    public Task<List<Season>> GetAllSeasons() => throw new NotSupportedException();

    public Task<Option<Season>> GetSeason(int seasonId) => throw new NotSupportedException();

    public Task<int> GetSeasonCount(int showId) => throw new NotSupportedException();

    public Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
        throw new NotSupportedException();

    public Task<List<Episode>> GetSeasonItems(int seasonId) => throw new NotSupportedException();

    public Task<int> GetEpisodeCount(int seasonId) => throw new NotSupportedException();

    public Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize) =>
        throw new NotSupportedException();

    public Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, MediaItemScanResult<Show>>>
        AddShow(int libraryPathId, string showFolder, ShowMetadata metadata) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path) =>
        throw new NotSupportedException();

    public Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<Unit> DeleteByPath(LibraryPath libraryPath, string path) => throw new NotSupportedException();

    public Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath) => throw new NotSupportedException();

    public Task<Either<BaseError, MediaItemScanResult<PlexShow>>> GetOrAddPlexShow(
        PlexLibrary library,
        PlexShow item) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, PlexSeason>> GetOrAddPlexSeason(PlexLibrary library, PlexSeason item) =>
        throw new NotSupportedException();

    public Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> GetOrAddPlexEpisode(
        PlexLibrary library,
        PlexEpisode item) =>
        throw new NotSupportedException();

    public Task<bool> AddGenre(ShowMetadata metadata, Genre genre) => throw new NotSupportedException();
    public Task<bool> AddTag(Domain.Metadata metadata, Tag tag) => throw new NotSupportedException();

    public Task<bool> AddStudio(ShowMetadata metadata, Studio studio) => throw new NotSupportedException();
    public Task<bool> AddActor(ShowMetadata metadata, Actor actor) => throw new NotSupportedException();

    public Task<bool> AddActor(EpisodeMetadata metadata, Actor actor) => throw new NotSupportedException();

    public Task<List<int>> RemoveMissingPlexShows(PlexLibrary library, List<string> showKeys) =>
        throw new NotSupportedException();

    public Task<Unit> RemoveMissingPlexSeasons(string showKey, List<string> seasonKeys) =>
        throw new NotSupportedException();

    public Task<List<int>> RemoveMissingPlexEpisodes(string seasonKey, List<string> episodeKeys) =>
        throw new NotSupportedException();

    public Task<Unit> RemoveMetadata(Episode episode, EpisodeMetadata metadata) =>
        throw new NotSupportedException();

    public Task<bool> AddDirector(EpisodeMetadata metadata, Director director) => throw new NotSupportedException();

    public Task<bool> AddWriter(EpisodeMetadata metadata, Writer writer) => throw new NotSupportedException();
    public Task<Unit> UpdatePath(int mediaFileId, string path) => throw new NotSupportedException();
    public Task<Unit> SetPlexEtag(PlexShow show, string etag) => throw new NotSupportedException();

    public Task<Unit> SetPlexEtag(PlexSeason season, string etag) => throw new NotSupportedException();

    public Task<Unit> SetPlexEtag(PlexEpisode episode, string etag) => throw new NotSupportedException();

    public Task<List<PlexItemEtag>> GetExistingPlexEpisodes(PlexLibrary library, PlexSeason season) =>
        throw new NotSupportedException();
}
