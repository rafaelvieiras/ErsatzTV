﻿using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class TelevisionRepository : ITelevisionRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public TelevisionRepository(IDbContextFactory<TvContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<bool> AllShowsExist(List<int> showIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Show WHERE Id in @ShowIds",
                new { ShowIds = showIds })
            .Map(c => c == showIds.Count);
    }

    public async Task<bool> AllSeasonsExist(List<int> seasonIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Season WHERE Id in @SeasonIds",
                new { SeasonIds = seasonIds })
            .Map(c => c == seasonIds.Count);
    }

    public async Task<bool> AllEpisodesExist(List<int> episodeIds)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Episode WHERE Id in @EpisodeIds",
                new { EpisodeIds = episodeIds })
            .Map(c => c == episodeIds.Count);
    }

    public async Task<List<Show>> GetAllShows()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Shows
            .AsNoTracking()
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .ToListAsync();
    }

    public async Task<Option<Show>> GetShow(int showId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Shows
            .AsNoTracking()
            .Filter(s => s.Id == showId)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Genres)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Studios)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Actors)
            .ThenInclude(a => a.Artwork)
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync()
            .Map(Optional);
    }

    public async Task<List<ShowMetadata>> GetShowsForCards(List<int> ids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.ShowMetadata
            .AsNoTracking()
            .Filter(sm => ids.Contains(sm.ShowId))
            .Include(sm => sm.Artwork)
            .Include(sm => sm.Show)
            .OrderBy(sm => sm.SortTitle)
            .ToListAsync();
    }

    public async Task<List<SeasonMetadata>> GetSeasonsForCards(List<int> ids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.SeasonMetadata
            .AsNoTracking()
            .Filter(s => ids.Contains(s.SeasonId))
            .Include(s => s.Season.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Include(sm => sm.Artwork)
            .ToListAsync()
            .Map(
                list => list
                    .OrderBy(s => s.Season.Show.ShowMetadata.HeadOrNone().Match(sm => sm.SortTitle, () => string.Empty))
                    .ThenBy(s => s.Season.SeasonNumber)
                    .ToList());
    }

    public async Task<List<EpisodeMetadata>> GetEpisodesForCards(List<int> ids)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EpisodeMetadata
            .AsNoTracking()
            .Filter(em => ids.Contains(em.EpisodeId))
            .Include(em => em.Artwork)
            .Include(em => em.Directors)
            .Include(em => em.Writers)
            .Include(em => em.Episode)
            .ThenInclude(e => e.Season)
            .ThenInclude(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(em => em.Episode)
            .ThenInclude(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(em => em.Episode)
            .ThenInclude(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(em => em.SortTitle)
            .ToListAsync();
    }

    public async Task<List<Season>> GetAllSeasons()
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Seasons
            .AsNoTracking()
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .ToListAsync();
    }

    public async Task<Option<Season>> GetSeason(int seasonId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Seasons
            .AsNoTracking()
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .OrderBy(s => s.Id)
            .SingleOrDefaultAsync(s => s.Id == seasonId)
            .Map(Optional);
    }

    public async Task<int> GetSeasonCount(int showId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Seasons
            .AsNoTracking()
            .CountAsync(s => s.ShowId == showId);
    }

    public async Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        List<int> showIds = await dbContext.Connection.QueryAsync<int>(
                @"SELECT m1.ShowId
                FROM ShowMetadata m1
                LEFT OUTER JOIN ShowMetadata m2 ON m2.ShowId = @ShowId
                WHERE m1.Title = m2.Title AND m1.Year is m2.Year",
                new { ShowId = televisionShowId })
            .Map(results => results.ToList());

        return await dbContext.Seasons
            .AsNoTracking()
            .Where(s => showIds.Contains(s.ShowId))
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .OrderBy(s => s.SeasonNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetEpisodeCount(int seasonId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Episodes
            .AsNoTracking()
            .CountAsync(e => e.SeasonId == seasonId);
    }

    public async Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EpisodeMetadata
            .AsNoTracking()
            .Filter(em => em.Episode.SeasonId == seasonId)
            .Include(em => em.Artwork)
            .Include(em => em.Directors)
            .Include(em => em.Writers)
            .Include(em => em.Episode)
            .ThenInclude(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(em => em.Episode)
            .ThenInclude(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(em => em.EpisodeNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<int> maybeId = await dbContext.ShowMetadata
            .Where(s => s.Title == metadata.Title && s.Year == metadata.Year)
            .Where(s => s.Show.LibraryPathId == libraryPathId)
            .SingleOrDefaultAsync()
            .Map(Optional)
            .MapT(sm => sm.ShowId);

        return await maybeId.Match(
            id =>
            {
                return dbContext.Shows
                    .AsNoTracking()
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Artwork)
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Genres)
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Tags)
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Studios)
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Actors)
                    .ThenInclude(a => a.Artwork)
                    .Include(s => s.ShowMetadata)
                    .ThenInclude(sm => sm.Guids)
                    .Include(s => s.LibraryPath)
                    .ThenInclude(lp => lp.Library)
                    .Include(s => s.TraktListItems)
                    .ThenInclude(tli => tli.TraktList)
                    .OrderBy(s => s.Id)
                    .SingleOrDefaultAsync(s => s.Id == id)
                    .Map(Optional);
            },
            () => Option<Show>.None.AsTask());
    }

    public async Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(
        int libraryPathId,
        string showFolder,
        ShowMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        try
        {
            metadata.DateAdded = DateTime.UtcNow;
            metadata.Genres ??= new List<Genre>();
            metadata.Tags ??= new List<Tag>();
            metadata.Studios ??= new List<Studio>();
            metadata.Actors ??= new List<Actor>();
            metadata.Guids ??= new List<MetadataGuid>();
            var show = new Show
            {
                LibraryPathId = libraryPathId,
                ShowMetadata = new List<ShowMetadata> { metadata },
                Seasons = new List<Season>(),
                TraktListItems = new List<TraktListItem>()
            };

            await dbContext.Shows.AddAsync(show);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(show).Reference(s => s.LibraryPath).LoadAsync();
            await dbContext.Entry(show.LibraryPath).Reference(lp => lp.Library).LoadAsync();

            return new MediaItemScanResult<Show>(show) { IsAdded = true };
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    public async Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<Season> maybeExisting = await dbContext.Seasons
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Guids)
            .Include(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(s => s.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(s => s.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .OrderBy(s => s.ShowId)
            .ThenBy(s => s.SeasonNumber)
            .SingleOrDefaultAsync(s => s.ShowId == show.Id && s.SeasonNumber == seasonNumber);

        return await maybeExisting.Match(
            season => Right<BaseError, Season>(season).AsTask(),
            () => AddSeason(dbContext, show, libraryPathId, seasonNumber));
    }

    public async Task<Either<BaseError, Episode>> GetOrAddEpisode(
        Season season,
        LibraryPath libraryPath,
        string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        Option<Episode> maybeExisting = await dbContext.Episodes
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Artwork)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Genres)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Tags)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Studios)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(em => em.Guids)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Directors)
            .Include(i => i.EpisodeMetadata)
            .ThenInclude(mm => mm.Writers)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(i => i.MediaVersions)
            .ThenInclude(mv => mv.Streams)
            .Include(i => i.LibraryPath)
            .ThenInclude(lp => lp.Library)
            .Include(i => i.Season)
            .Include(i => i.TraktListItems)
            .ThenInclude(tli => tli.TraktList)
            .OrderBy(i => i.MediaVersions.First().MediaFiles.First().Path)
            .SingleOrDefaultAsync(i => i.MediaVersions.First().MediaFiles.First().Path == path);

        return await maybeExisting.Match<Task<Either<BaseError, Episode>>>(
            async episode =>
            {
                // move the file to the new season if needed
                // this can happen when adding NFO metadata to existing content
                if (episode.SeasonId != season.Id)
                {
                    episode.SeasonId = season.Id;
                    episode.Season = season;

                    await dbContext.Connection.ExecuteAsync(
                        @"UPDATE Episode SET SeasonId = @SeasonId WHERE Id = @EpisodeId",
                        new { SeasonId = season.Id, EpisodeId = episode.Id });
                }

                return episode;
            },
            async () => await AddEpisode(dbContext, season, libraryPath.Id, path));
    }

    public async Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.QueryAsync<string>(
            @"SELECT MF.Path
                FROM MediaFile MF
                INNER JOIN MediaVersion MV on MF.MediaVersionId = MV.Id
                INNER JOIN Episode E on MV.EpisodeId = E.Id
                INNER JOIN MediaItem MI on E.Id = MI.Id
                WHERE MI.LibraryPathId = @LibraryPathId",
            new { LibraryPathId = libraryPath.Id });
    }

    public async Task<Unit> DeleteByPath(LibraryPath libraryPath, string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT E.Id
                FROM Episode E
                INNER JOIN MediaItem MI on E.Id = MI.Id
                INNER JOIN MediaVersion MV on E.Id = MV.EpisodeId
                INNER JOIN MediaFile MF on MV.Id = MF.MediaVersionId
                WHERE MI.LibraryPathId = @LibraryPathId AND MF.Path = @Path",
            new { LibraryPathId = libraryPath.Id, Path = path });

        foreach (int episodeId in ids)
        {
            Episode episode = await dbContext.Episodes.FindAsync(episodeId);
            if (episode != null)
            {
                dbContext.Episodes.Remove(episode);
            }
        }

        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }

    public async Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<Season> seasons = await dbContext.Seasons
            .Filter(s => s.LibraryPathId == libraryPath.Id)
            .Filter(s => s.Episodes.Count == 0)
            .ToListAsync();
        dbContext.Seasons.RemoveRange(seasons);
        await dbContext.SaveChangesAsync();
        return Unit.Default;
    }

    public async Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<Show> shows = await dbContext.Shows
            .Filter(s => s.LibraryPathId == libraryPath.Id)
            .Filter(s => s.Seasons.Count == 0)
            .ToListAsync();
        var ids = shows.Map(s => s.Id).ToList();
        dbContext.Shows.RemoveRange(shows);
        await dbContext.SaveChangesAsync();
        return ids;
    }

    public async Task<Unit> RemoveMetadata(Episode episode, EpisodeMetadata metadata)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        episode.EpisodeMetadata.Remove(metadata);
        await dbContext.Connection.ExecuteAsync(
            @"DELETE FROM EpisodeMetadata WHERE Id = @MetadataId",
            new { MetadataId = metadata.Id });
        return Unit.Default;
    }

    public async Task<bool> AddDirector(EpisodeMetadata metadata, Director director)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Director (Name, EpisodeMetadataId) VALUES (@Name, @MetadataId)",
            new { director.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddWriter(EpisodeMetadata metadata, Writer writer)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Writer (Name, EpisodeMetadataId) VALUES (@Name, @MetadataId)",
            new { writer.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<Unit> UpdatePath(int mediaFileId, string path)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "UPDATE MediaFile SET Path = @Path WHERE Id = @MediaFileId",
            new { Path = path, MediaFileId = mediaFileId }).Map(_ => Unit.Default);
    }

    public async Task<List<Episode>> GetShowItems(int showId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        IEnumerable<int> ids = await dbContext.Connection.QueryAsync<int>(
            @"SELECT Episode.Id FROM Show
            INNER JOIN Season ON Season.ShowId = Show.Id
            INNER JOIN Episode ON Episode.SeasonId = Season.Id
            WHERE Show.Id = @ShowId",
            new { ShowId = showId });

        return await dbContext.Episodes
            .AsNoTracking()
            .Include(e => e.EpisodeMetadata)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Filter(e => ids.Contains(e.Id))
            .ToListAsync();
    }

    public async Task<List<Episode>> GetSeasonItems(int seasonId)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Episodes
            .AsNoTracking()
            .Include(e => e.EpisodeMetadata)
            .Include(e => e.MediaVersions)
            .ThenInclude(mv => mv.Chapters)
            .Include(m => m.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .Include(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .Filter(e => e.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task<bool> AddGenre(ShowMetadata metadata, Genre genre)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Genre (Name, ShowMetadataId) VALUES (@Name, @MetadataId)",
            new { genre.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddTag(Metadata metadata, Tag tag)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        switch (metadata)
        {
            case ShowMetadata:
                return await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO Tag (Name, ShowMetadataId, ExternalCollectionId) VALUES (@Name, @MetadataId, @ExternalCollectionId)",
                    new { tag.Name, MetadataId = metadata.Id, tag.ExternalCollectionId }).Map(result => result > 0);
            case SeasonMetadata:
                return await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO Tag (Name, SeasonMetadataId, ExternalCollectionId) VALUES (@Name, @MetadataId, @ExternalCollectionId)",
                    new { tag.Name, MetadataId = metadata.Id, tag.ExternalCollectionId }).Map(result => result > 0);
            case EpisodeMetadata:
                return await dbContext.Connection.ExecuteAsync(
                    "INSERT INTO Tag (Name, EpisodeMetadataId, ExternalCollectionId) VALUES (@Name, @MetadataId, @ExternalCollectionId)",
                    new { tag.Name, MetadataId = metadata.Id, tag.ExternalCollectionId }).Map(result => result > 0);
            default:
                return false;
        }
    }

    public async Task<bool> AddStudio(ShowMetadata metadata, Studio studio)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Connection.ExecuteAsync(
            "INSERT INTO Studio (Name, ShowMetadataId) VALUES (@Name, @MetadataId)",
            new { studio.Name, MetadataId = metadata.Id }).Map(result => result > 0);
    }

    public async Task<bool> AddActor(ShowMetadata metadata, Actor actor)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? artworkId = null;

        if (actor.Artwork != null)
        {
            artworkId = await dbContext.Connection.QuerySingleAsync<int>(
                @"INSERT INTO Artwork (ArtworkKind, DateAdded, DateUpdated, Path)
                      VALUES (@ArtworkKind, @DateAdded, @DateUpdated, @Path);
                      SELECT last_insert_rowid()",
                new
                {
                    ArtworkKind = (int)actor.Artwork.ArtworkKind,
                    actor.Artwork.DateAdded,
                    actor.Artwork.DateUpdated,
                    actor.Artwork.Path
                });
        }

        return await dbContext.Connection.ExecuteAsync(
                "INSERT INTO Actor (Name, Role, \"Order\", ShowMetadataId, ArtworkId) VALUES (@Name, @Role, @Order, @MetadataId, @ArtworkId)",
                new { actor.Name, actor.Role, actor.Order, MetadataId = metadata.Id, ArtworkId = artworkId })
            .Map(result => result > 0);
    }

    public async Task<bool> AddActor(EpisodeMetadata metadata, Actor actor)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? artworkId = null;

        if (actor.Artwork != null)
        {
            artworkId = await dbContext.Connection.QuerySingleAsync<int>(
                @"INSERT INTO Artwork (ArtworkKind, DateAdded, DateUpdated, Path)
                      VALUES (@ArtworkKind, @DateAdded, @DateUpdated, @Path);
                      SELECT last_insert_rowid()",
                new
                {
                    ArtworkKind = (int)actor.Artwork.ArtworkKind,
                    actor.Artwork.DateAdded,
                    actor.Artwork.DateUpdated,
                    actor.Artwork.Path
                });
        }

        return await dbContext.Connection.ExecuteAsync(
                "INSERT INTO Actor (Name, Role, \"Order\", EpisodeMetadataId, ArtworkId) VALUES (@Name, @Role, @Order, @MetadataId, @ArtworkId)",
                new { actor.Name, actor.Role, actor.Order, MetadataId = metadata.Id, ArtworkId = artworkId })
            .Map(result => result > 0);
    }

    private static async Task<Either<BaseError, Season>> AddSeason(
        TvContext dbContext,
        Show show,
        int libraryPathId,
        int seasonNumber)
    {
        try
        {
            var season = new Season
            {
                LibraryPathId = libraryPathId,
                ShowId = show.Id,
                SeasonNumber = seasonNumber,
                Episodes = new List<Episode>(),
                SeasonMetadata = new List<SeasonMetadata>
                {
                    new()
                    {
                        DateAdded = DateTime.UtcNow,
                        Guids = new List<MetadataGuid>(),
                        Tags = new List<Tag>()
                    }
                },
                TraktListItems = new List<TraktListItem>()
            };
            await dbContext.Seasons.AddAsync(season);
            await dbContext.SaveChangesAsync();

            await dbContext.Entry(season).Reference(s => s.LibraryPath).LoadAsync();
            await dbContext.Entry(season.LibraryPath).Reference(lp => lp.Library).LoadAsync();

            return season;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<Either<BaseError, Episode>> AddEpisode(
        TvContext dbContext,
        Season season,
        int libraryPathId,
        string path)
    {
        try
        {
            if (dbContext.MediaFiles.Any(mf => mf.Path == path))
            {
                return BaseError.New("Multi-episode files are not yet supported");
            }

            var episode = new Episode
            {
                LibraryPathId = libraryPathId,
                SeasonId = season.Id,
                EpisodeMetadata = new List<EpisodeMetadata>
                {
                    new()
                    {
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = SystemTime.MinValueUtc,
                        MetadataKind = MetadataKind.Fallback,
                        Actors = new List<Actor>(),
                        Guids = new List<MetadataGuid>(),
                        Writers = new List<Writer>(),
                        Directors = new List<Director>(),
                        Genres = new List<Genre>(),
                        Tags = new List<Tag>(),
                        Studios = new List<Studio>()
                    }
                },
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        },
                        Streams = new List<MediaStream>()
                    }
                },
                TraktListItems = new List<TraktListItem>()
            };
            await dbContext.Episodes.AddAsync(episode);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(episode).Reference(i => i.LibraryPath).LoadAsync();
            await dbContext.Entry(episode.LibraryPath).Reference(lp => lp.Library).LoadAsync();
            await dbContext.Entry(episode).Reference(e => e.Season).LoadAsync();
            return episode;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}
