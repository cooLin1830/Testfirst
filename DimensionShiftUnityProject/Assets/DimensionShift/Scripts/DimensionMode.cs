namespace DimensionShift
{
    public enum DimensionMode
    {
        TwoD,
        ThreeD
    }

    public interface IDimensionModeListener
    {
        void SetDimensionMode(DimensionMode mode);
    }
}
