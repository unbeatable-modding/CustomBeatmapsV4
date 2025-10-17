using Rhythm;

namespace CustomBeatmaps.CustomData
{
    public readonly struct CCategory(int category)
    {
        public readonly int Index = category;
        public readonly BeatmapIndex.Category InternalCategory => BeatmapIndex.defaultIndex.Categories[Index];
    }
}
