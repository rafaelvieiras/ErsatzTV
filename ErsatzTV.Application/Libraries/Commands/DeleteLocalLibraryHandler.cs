﻿using Dapper;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Libraries;

public class DeleteLocalLibraryHandler : LocalLibraryHandlerBase,
    IRequestHandler<DeleteLocalLibrary, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchIndex _searchIndex;

    public DeleteLocalLibraryHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ISearchIndex searchIndex)
    {
        _dbContextFactory = dbContextFactory;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        DeleteLocalLibrary request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, LocalLibrary> validation = await LocalLibraryMustExist(dbContext, request);
        return await LanguageExtensions.Apply(validation, localLibrary => DoDeletion(dbContext, localLibrary));
    }

    private async Task<Unit> DoDeletion(TvContext dbContext, LocalLibrary localLibrary)
    {
        List<int> ids = await dbContext.Connection.QueryAsync<int>(
                @"SELECT MediaItem.Id FROM MediaItem
                      INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                      WHERE LP.LibraryId = @LibraryId",
                new { LibraryId = localLibrary.Id })
            .Map(result => result.ToList());

        await _searchIndex.RemoveItems(ids);
        _searchIndex.Commit();

        dbContext.LocalLibraries.Remove(localLibrary);
        await dbContext.SaveChangesAsync();

        return Unit.Default;
    }

    private static Task<Validation<BaseError, LocalLibrary>> LocalLibraryMustExist(
        TvContext dbContext,
        DeleteLocalLibrary request) =>
        dbContext.LocalLibraries
            .SelectOneAsync(ll => ll.Id, ll => ll.Id == request.LocalLibraryId)
            .Map(o => o.ToValidation<BaseError>($"Local library {request.LocalLibraryId} does not exist."));
}
