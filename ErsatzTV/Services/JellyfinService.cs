﻿using System.Threading.Channels;
using Bugsnag;
using ErsatzTV.Application;
using ErsatzTV.Application.Jellyfin;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using MediatR;

namespace ErsatzTV.Services;

public class JellyfinService : BackgroundService
{
    private readonly ChannelReader<IJellyfinBackgroundServiceRequest> _channel;
    private readonly ILogger<JellyfinService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public JellyfinService(
        ChannelReader<IJellyfinBackgroundServiceRequest> channel,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<JellyfinService> logger)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FileSystemLayout.JellyfinSecretsPath))
            {
                await File.WriteAllTextAsync(FileSystemLayout.JellyfinSecretsPath, "{}", cancellationToken);
            }

            _logger.LogInformation(
                "Jellyfin service started; secrets are at {JellyfinSecretsPath}",
                FileSystemLayout.JellyfinSecretsPath);

            // synchronize sources on startup
            await SynchronizeSources(new SynchronizeJellyfinMediaSources(), cancellationToken);

            await foreach (IJellyfinBackgroundServiceRequest request in _channel.ReadAllAsync(cancellationToken))
            {
                try
                {
                    Task requestTask;
                    switch (request)
                    {
                        case SynchronizeJellyfinMediaSources synchronizeJellyfinMediaSources:
                            requestTask = SynchronizeSources(synchronizeJellyfinMediaSources, cancellationToken);
                            break;
                        case SynchronizeJellyfinAdminUserId synchronizeJellyfinAdminUserId:
                            requestTask = SynchronizeAdminUserId(synchronizeJellyfinAdminUserId, cancellationToken);
                            break;
                        case SynchronizeJellyfinLibraries synchronizeJellyfinLibraries:
                            requestTask = SynchronizeLibraries(synchronizeJellyfinLibraries, cancellationToken);
                            break;
                        case ISynchronizeJellyfinLibraryById synchronizeJellyfinLibraryById:
                            requestTask = SynchronizeJellyfinLibrary(synchronizeJellyfinLibraryById, cancellationToken);
                            break;
                        case SynchronizeJellyfinCollections synchronizeJellyfinCollections:
                            requestTask = SynchronizeJellyfinCollections(
                                synchronizeJellyfinCollections,
                                cancellationToken);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
                    }

                    await requestTask;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process Jellyfin background service request");

                    try
                    {
                        using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                        {
                            IClient client = scope.ServiceProvider.GetRequiredService<IClient>();
                            client.Notify(ex);
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            _logger.LogInformation("Jellyfin service shutting down");
        }
    }

    private async Task SynchronizeSources(
        SynchronizeJellyfinMediaSources request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, List<JellyfinMediaSource>> result = await mediator.Send(request, cancellationToken);
        result.Match(
            sources =>
            {
                if (sources.Any())
                {
                    _logger.LogInformation("Successfully synchronized jellyfin media sources");
                }
            },
            error =>
            {
                _logger.LogWarning(
                    "Unable to synchronize jellyfin media sources: {Error}",
                    error.Value);
            });
    }

    private async Task SynchronizeLibraries(
        SynchronizeJellyfinLibraries request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogInformation(
                "Successfully synchronized Jellyfin libraries for source {MediaSourceId}",
                request.JellyfinMediaSourceId),
            error => _logger.LogWarning(
                "Unable to synchronize Jellyfin libraries for source {MediaSourceId}: {Error}",
                request.JellyfinMediaSourceId,
                error.Value));
    }

    private async Task SynchronizeAdminUserId(
        SynchronizeJellyfinAdminUserId request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogInformation(
                "Successfully synchronized Jellyfin admin user id for source {MediaSourceId}",
                request.JellyfinMediaSourceId),
            error => _logger.LogWarning(
                "Unable to synchronize Jellyfin admin user id for source {MediaSourceId}: {Error}",
                request.JellyfinMediaSourceId,
                error.Value));
    }

    private async Task SynchronizeJellyfinLibrary(
        ISynchronizeJellyfinLibraryById request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, string> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            name => _logger.LogDebug("Done synchronizing jellyfin library {Name}", name),
            error => _logger.LogWarning(
                "Unable to synchronize jellyfin library {LibraryId}: {Error}",
                request.JellyfinLibraryId,
                error.Value));
    }

    private async Task SynchronizeJellyfinCollections(
        SynchronizeJellyfinCollections request,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Either<BaseError, Unit> result = await mediator.Send(request, cancellationToken);
        result.BiIter(
            _ => _logger.LogDebug("Done synchronizing jellyfin collections"),
            error => _logger.LogWarning(
                "Unable to synchronize jellyfin collections for source {MediaSourceId}: {Error}",
                request.JellyfinMediaSourceId,
                error.Value));
    }
}
