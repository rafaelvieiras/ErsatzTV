using System.Diagnostics;
using CliWrap;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Images;
using ErsatzTV.Application.Streaming;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Iptv;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class IptvController : ControllerBase
{
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly ILogger<IptvController> _logger;
    private readonly IMediator _mediator;

    public IptvController(
        IMediator mediator,
        ILogger<IptvController> logger,
        IFFmpegSegmenterService ffmpegSegmenterService)
    {
        _mediator = mediator;
        _logger = logger;
        _ffmpegSegmenterService = ffmpegSegmenterService;
    }

    [HttpGet("iptv/channels.m3u")]
    public Task<IActionResult> GetChannelPlaylist(
        [FromQuery]
        string mode = "mixed") =>
        _mediator.Send(new GetChannelPlaylist(Request.Scheme, Request.Host.ToString(), mode))
            .Map<ChannelPlaylist, IActionResult>(Ok);

    [HttpGet("iptv/xmltv.xml")]
    public Task<IActionResult> GetGuide() =>
        _mediator.Send(new GetChannelGuide(Request.Scheme, Request.Host.ToString()))
            .Map<ChannelGuide, IActionResult>(Ok);

    [HttpGet("iptv/hdhr/channel/{channelNumber}.ts")]
    public Task<IActionResult> GetHDHRVideo(string channelNumber, [FromQuery] string mode = "ts")
    {
        // don't redirect to the correct channel mode for HDHR clients; always use TS
        if (mode != "ts" && mode != "ts-legacy")
        {
            mode = "ts";
        }

        return GetTransportStreamVideo(channelNumber, mode);
    }

    [HttpGet("iptv/channel/{channelNumber}.ts")]
    public async Task<IActionResult> GetTransportStreamVideo(
        string channelNumber,
        [FromQuery]
        string mode = null)
    {
        // if mode is "unspecified" - find the configured mode and set it or redirect
        if (string.IsNullOrWhiteSpace(mode) || mode == "mixed")
        {
            Option<ChannelViewModel> maybeChannel = await _mediator.Send(new GetChannelByNumber(channelNumber));
            foreach (ChannelViewModel channel in maybeChannel)
            {
                switch (channel.StreamingMode)
                {
                    case StreamingMode.TransportStream:
                        mode = "ts-legacy";
                        break;
                    case StreamingMode.TransportStreamHybrid:
                        mode = "ts";
                        break;
                    default:
                        return Redirect($"/iptv/channel/{channelNumber}.m3u8");
                }
            }
        }

        FFmpegProcessRequest request = mode switch
        {
            "ts-legacy" => new GetConcatProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber),
            _ => new GetWrappedProcessByChannelNumber(Request.Scheme, Request.Host.ToString(), channelNumber)
        };

        return await _mediator.Send(request)
            .Map(
                result => result.Match<IActionResult>(
                    processModel =>
                    {
                        Command command = processModel.Process;

                        _logger.LogInformation("Starting ts stream for channel {ChannelNumber}", channelNumber);
                        _logger.LogInformation("ffmpeg arguments {FFmpegArguments}", command.Arguments);
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = command.TargetFilePath,
                                Arguments = command.Arguments,
                                RedirectStandardOutput = true,
                                RedirectStandardError = false,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        foreach ((string key, string value) in command.EnvironmentVariables)
                        {
                            process.StartInfo.Environment[key] = value;
                        }

                        process.Start();
                        return new FileStreamResult(process.StandardOutput.BaseStream, "video/mp2t");
                    },
                    error => BadRequest(error.Value)));
    }

    [HttpGet("iptv/session/{channelNumber}/hls.m3u8")]
    public async Task<IActionResult> GetLivePlaylist(string channelNumber, CancellationToken cancellationToken)
    {
        if (_ffmpegSegmenterService.SessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            DateTimeOffset now = DateTimeOffset.Now.AddSeconds(-30);
            Option<TrimPlaylistResult> maybePlaylist = await worker.TrimPlaylist(now, cancellationToken);
            foreach (TrimPlaylistResult result in maybePlaylist)
            {
                return Content(result.Playlist, "application/vnd.apple.mpegurl");
            }
        }

        return NotFound();
    }

    [HttpGet("iptv/channel/{channelNumber}.m3u8")]
    public async Task<IActionResult> GetHttpLiveStreamingVideo(
        string channelNumber,
        [FromQuery]
        string mode = "mixed")
    {
        // if mode is "unspecified" - find the configured mode and set it or redirect
        if (string.IsNullOrWhiteSpace(mode) || mode == "mixed")
        {
            Option<ChannelViewModel> maybeChannel = await _mediator.Send(new GetChannelByNumber(channelNumber));
            foreach (ChannelViewModel channel in maybeChannel)
            {
                switch (channel.StreamingMode)
                {
                    case StreamingMode.HttpLiveStreamingDirect:
                        mode = "hls-direct";
                        break;
                    case StreamingMode.HttpLiveStreamingSegmenter:
                        mode = "segmenter";
                        break;
                    default:
                        return Redirect($"/iptv/channel/{channelNumber}.ts");
                }
            }
        }

        switch (mode)
        {
            case "segmenter":
                Either<BaseError, Unit> result = await _mediator.Send(new StartFFmpegSession(channelNumber, false));
                return result.Match<IActionResult>(
                    _ => Redirect($"/iptv/session/{channelNumber}/hls.m3u8"),
                    error =>
                    {
                        switch (error)
                        {
                            case ChannelSessionAlreadyActive:
                                return RedirectPreserveMethod($"/iptv/session/{channelNumber}/hls.m3u8");
                            default:
                                _logger.LogWarning(
                                    "Failed to start segmenter for channel {ChannelNumber}: {Error}",
                                    channelNumber,
                                    error.ToString());
                                return NotFound();
                        }
                    });
            default:
                return await _mediator.Send(
                        new GetHlsPlaylistByChannelNumber(
                            Request.Scheme,
                            Request.Host.ToString(),
                            channelNumber,
                            mode))
                    .Map(
                        r => r.Match<IActionResult>(
                            playlist => Content(playlist, "application/x-mpegurl"),
                            error => BadRequest(error.Value)));
        }
    }

    [HttpGet("iptv/logos/{fileName}")]
    [HttpHead("iptv/logos/{fileName}.jpg")]
    [HttpGet("iptv/logos/{fileName}.jpg")]
    public async Task<IActionResult> GetImage(string fileName)
    {
        Either<BaseError, CachedImagePathViewModel> cachedImagePath =
            await _mediator.Send(new GetCachedImagePath(fileName, ArtworkKind.Logo));
        return cachedImagePath.Match<IActionResult>(
            Left: _ => new NotFoundResult(),
            Right: r => new PhysicalFileResult(r.FileName, r.MimeType));
    }
}
