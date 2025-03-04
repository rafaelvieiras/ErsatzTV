﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    UpdateFFmpegProfileHandler : IRequestHandler<UpdateFFmpegProfile, Either<BaseError, UpdateFFmpegProfileResult>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, UpdateFFmpegProfileResult>> Handle(
        UpdateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, FFmpegProfile> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, p => ApplyUpdateRequest(dbContext, p, request));
    }

    private async Task<UpdateFFmpegProfileResult> ApplyUpdateRequest(
        TvContext dbContext,
        FFmpegProfile p,
        UpdateFFmpegProfile update)
    {
        p.Name = update.Name;
        p.ThreadCount = update.ThreadCount;
        p.HardwareAcceleration = update.HardwareAcceleration;
        p.VaapiDriver = update.VaapiDriver;
        p.VaapiDevice = update.VaapiDevice;
        p.ResolutionId = update.ResolutionId;
        p.VideoFormat = update.VideoFormat;
        p.VideoBitrate = update.VideoBitrate;
        p.VideoBufferSize = update.VideoBufferSize;
        p.AudioFormat = update.AudioFormat;
        p.AudioBitrate = update.AudioBitrate;
        p.AudioBufferSize = update.AudioBufferSize;
        p.NormalizeLoudness = update.NormalizeLoudness;
        p.AudioChannels = update.AudioChannels;
        p.AudioSampleRate = update.AudioSampleRate;
        p.NormalizeFramerate = update.NormalizeFramerate;
        p.DeinterlaceVideo = update.DeinterlaceVideo;
        await dbContext.SaveChangesAsync();
        return new UpdateFFmpegProfileResult(p.Id);
    }

    private static async Task<Validation<BaseError, FFmpegProfile>> Validate(
        TvContext dbContext,
        UpdateFFmpegProfile request) =>
        (await FFmpegProfileMustExist(dbContext, request), ValidateName(request), ValidateThreadCount(request),
            await ResolutionMustExist(dbContext, request))
        .Apply((ffmpegProfileToUpdate, _, _, _) => ffmpegProfileToUpdate);

    private static Task<Validation<BaseError, FFmpegProfile>> FFmpegProfileMustExist(
        TvContext dbContext,
        UpdateFFmpegProfile updateFFmpegProfile) =>
        dbContext.FFmpegProfiles
            .SelectOneAsync(p => p.Id, p => p.Id == updateFFmpegProfile.FFmpegProfileId)
            .Map(o => o.ToValidation<BaseError>("FFmpegProfile does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateFFmpegProfile updateFFmpegProfile) =>
        updateFFmpegProfile.NotEmpty(x => x.Name)
            .Bind(_ => updateFFmpegProfile.NotLongerThan(50)(x => x.Name));

    private static Validation<BaseError, int> ValidateThreadCount(UpdateFFmpegProfile updateFFmpegProfile) =>
        updateFFmpegProfile.AtLeast(0)(p => p.ThreadCount);

    private static Task<Validation<BaseError, int>> ResolutionMustExist(
        TvContext dbContext,
        UpdateFFmpegProfile updateFFmpegProfile) =>
        dbContext.Resolutions
            .SelectOneAsync(r => r.Id, r => r.Id == updateFFmpegProfile.ResolutionId)
            .MapT(r => r.Id)
            .Map(o => o.ToValidation<BaseError>($"[Resolution] {updateFFmpegProfile.ResolutionId} does not exist"));
}
