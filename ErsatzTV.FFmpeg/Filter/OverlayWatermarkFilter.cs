﻿using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.FFmpeg.Filter;

public class OverlayWatermarkFilter : BaseFilter
{
    private readonly FrameState _currentState;
    private readonly FrameSize _resolution;
    private readonly WatermarkState _watermarkState;

    public OverlayWatermarkFilter(FrameState currentState, WatermarkState watermarkState, FrameSize resolution)
    {
        _currentState = currentState;
        _watermarkState = watermarkState;
        _resolution = resolution;
    }

    public override string Filter => $"overlay={Position}";

    protected string Position
    {
        get
        {
            double horizontalMargin = Math.Round(_watermarkState.HorizontalMarginPercent / 100.0 * _resolution.Width);
            double verticalMargin = Math.Round(_watermarkState.VerticalMarginPercent / 100.0 * _resolution.Height);

            return _watermarkState.Location switch
            {
                WatermarkLocation.BottomLeft => $"x={horizontalMargin}:y=H-h-{verticalMargin}",
                WatermarkLocation.TopLeft => $"x={horizontalMargin}:y={verticalMargin}",
                WatermarkLocation.TopRight => $"x=W-w-{horizontalMargin}:y={verticalMargin}",
                WatermarkLocation.TopMiddle => $"x=(W-w)/2:y={verticalMargin}",
                WatermarkLocation.RightMiddle => $"x=W-w-{horizontalMargin}:y=(H-h)/2",
                WatermarkLocation.BottomMiddle => $"x=(W-w)/2:y=H-h-{verticalMargin}",
                WatermarkLocation.LeftMiddle => $"x={horizontalMargin}:y=(H-h)/2",
                _ => $"x=W-w-{horizontalMargin}:y=H-h-{verticalMargin}"
            };
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
