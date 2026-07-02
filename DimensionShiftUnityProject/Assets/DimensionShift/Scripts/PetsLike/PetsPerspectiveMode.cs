namespace DimensionShift.PetsLike
{
    public enum PetsPerspectiveMode
    {
        TwoD,
        TwoPointFiveD
    }

    public enum PetsCellKind
    {
        Empty,
        WhiteLine,
        WhiteInterior,
        BlackRegion,
        SwitchTo2D,
        SwitchToTwoPointFiveD,
        Exit
    }

    public interface IPetsPerspectiveListener
    {
        void SetPerspectiveMode(PetsPerspectiveMode mode);
    }
}
