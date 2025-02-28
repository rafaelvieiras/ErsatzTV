﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class PlayoutModeSchedulerOne : PlayoutModeSchedulerBase<ProgramScheduleItemOne>
{
    public PlayoutModeSchedulerOne(ILogger logger) : base(logger)
    {
    }

    public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItemOne scheduleItem,
        ProgramScheduleItem nextScheduleItem,
        DateTimeOffset hardStop)
    {
        IMediaCollectionEnumerator contentEnumerator =
            collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
        foreach (MediaItem mediaItem in contentEnumerator.Current)
        {
            // find when we should start this item, based on the current time
            DateTimeOffset itemStartTime = GetStartTimeAfter(
                playoutBuilderState,
                scheduleItem);

            TimeSpan itemDuration = DurationForMediaItem(mediaItem);
            List<MediaChapter> itemChapters = ChaptersForMediaItem(mediaItem);

            var playoutItem = new PlayoutItem
            {
                MediaItemId = mediaItem.Id,
                Start = itemStartTime.UtcDateTime,
                Finish = itemStartTime.UtcDateTime + itemDuration,
                InPoint = TimeSpan.Zero,
                OutPoint = itemDuration,
                GuideGroup = playoutBuilderState.NextGuideGroup,
                FillerKind = scheduleItem.GuideMode == GuideMode.Filler
                    ? FillerKind.Tail
                    : FillerKind.None,
                WatermarkId = scheduleItem.WatermarkId,
                PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                SubtitleMode = scheduleItem.SubtitleMode
            };

            DateTimeOffset itemEndTimeWithFiller = CalculateEndTimeWithFiller(
                collectionEnumerators,
                scheduleItem,
                itemStartTime,
                itemDuration,
                itemChapters);

            List<PlayoutItem> playoutItems = AddFiller(
                playoutBuilderState,
                collectionEnumerators,
                scheduleItem,
                playoutItem,
                itemChapters);

            PlayoutBuilderState nextState = playoutBuilderState with
            {
                CurrentTime = itemEndTimeWithFiller
            };

            nextState.ScheduleItemsEnumerator.MoveNext();
            contentEnumerator.MoveNext();

            // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);

            // only play one item from collection, so always advance to the next item
            // _logger.LogDebug(
            //     "Advancing to next schedule item after playout mode {PlayoutMode}",
            //     "One");

            DateTimeOffset nextItemStart = GetStartTimeAfter(nextState, nextScheduleItem);

            if (scheduleItem.TailFiller != null)
            {
                (nextState, playoutItems) = AddTailFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    nextItemStart);
            }

            if (scheduleItem.FallbackFiller != null)
            {
                (nextState, playoutItems) = AddFallbackFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    nextItemStart);
            }

            nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

            return Tuple(nextState, playoutItems);
        }

        return Tuple(playoutBuilderState, new List<PlayoutItem>());
    }
}
