﻿namespace ErsatzTV.Application.Search;

public record RebuildSearchIndex : IRequest<Unit>, IBackgroundServiceRequest;
