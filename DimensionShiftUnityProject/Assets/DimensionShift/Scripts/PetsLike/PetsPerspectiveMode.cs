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
        Exit,
        BreakableBrick,
        PushBox,
        BouncePad,
        HeadBreakBox
    }

    public enum PetsPropKind
    {
        None,
        BreakableBrick,
        PushBox,
        HeadBreakBox
    }

    public interface IPetsPerspectiveListener
    {
        void SetPerspectiveMode(PetsPerspectiveMode mode);
    }
}
