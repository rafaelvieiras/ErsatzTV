﻿using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Emby;

public interface IEmbyTelevisionLibraryScanner
{
    Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken);
}
