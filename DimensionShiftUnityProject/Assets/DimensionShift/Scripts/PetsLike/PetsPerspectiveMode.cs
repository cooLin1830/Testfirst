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
        HeadBreakBox,
        Star
    }

    public enum PetsPropKind
    {
        None,
        BreakableBrick,
        PushBox,
        HeadBreakBox,
        Star
    }

    public interface IPetsPerspectiveListener
    {
        void SetPerspectiveMode(PetsPerspectiveMode mode);
    }
}
