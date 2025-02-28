﻿using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Option;

namespace ErsatzTV.FFmpeg;

public static class CommandGenerator
{
    public static IList<EnvironmentVariable> GenerateEnvironmentVariables(IEnumerable<IPipelineStep> pipelineSteps) =>
        pipelineSteps.SelectMany(ps => ps.EnvironmentVariables).ToList();

    public static IList<string> GenerateArguments(
        Option<VideoInputFile> maybeVideoInputFile,
        Option<AudioInputFile> maybeAudioInputFile,
        Option<WatermarkInputFile> maybeWatermarkInputFile,
        Option<ConcatInputFile> maybeConcatInputFile,
        IList<IPipelineStep> pipelineSteps)
    {
        var arguments = new List<string>();

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.GlobalOptions);
        }

        var includedPaths = new System.Collections.Generic.HashSet<string>();
        foreach (VideoInputFile videoInputFile in maybeVideoInputFile)
        {
            includedPaths.Add(videoInputFile.Path);

            foreach (IInputOption step in videoInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(videoInputFile));
            }

            arguments.AddRange(new[] { "-i", videoInputFile.Path });
        }

        foreach (AudioInputFile audioInputFile in maybeAudioInputFile)
        {
            if (!includedPaths.Contains(audioInputFile.Path))
            {
                includedPaths.Add(audioInputFile.Path);

                foreach (IInputOption step in audioInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(audioInputFile));
                }

                arguments.AddRange(new[] { "-i", audioInputFile.Path });
            }
        }

        foreach (WatermarkInputFile watermarkInputFile in maybeWatermarkInputFile)
        {
            if (!includedPaths.Contains(watermarkInputFile.Path))
            {
                includedPaths.Add(watermarkInputFile.Path);

                foreach (IInputOption step in watermarkInputFile.InputOptions)
                {
                    arguments.AddRange(step.InputOptions(watermarkInputFile));
                }

                arguments.AddRange(new[] { "-i", watermarkInputFile.Path });
            }
        }

        foreach (ConcatInputFile concatInputFile in maybeConcatInputFile)
        {
            foreach (IInputOption step in concatInputFile.InputOptions)
            {
                arguments.AddRange(step.InputOptions(concatInputFile));
            }

            arguments.AddRange(new[] { "-i", concatInputFile.Path });
        }

        foreach (IPipelineStep step in pipelineSteps)
        {
            arguments.AddRange(step.FilterOptions);
        }

        foreach (IPipelineStep step in pipelineSteps.Filter(s => s is not StreamSeekFilterOption))
        {
            arguments.AddRange(step.OutputOptions);
        }

        return arguments;
    }
}
