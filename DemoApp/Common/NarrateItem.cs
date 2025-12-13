using System;

namespace DemoApp.Common
{
    public class NarrateItem : HistoryItem
    {
        public string Model { get; init; }
        public string Voice { get; init; }
        public int Seed { get; init; }
        public float Speed { get; init; }
        public int Steps { get; init; }
        public int Channels { get; init; }
        public int SampleRate { get; init; }
        public TimeSpan Duration { get; init; }
        public string InputText { get; init; }
    }
}
