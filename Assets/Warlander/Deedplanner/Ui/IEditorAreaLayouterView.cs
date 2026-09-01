namespace Warlander.Deedplanner.Ui
{
    public interface IEditorAreaLayouterView
    {
        void SetScreenVisible(int index, bool visible);
        void SetBottomRowVisible(bool visible);
        void SetSplitVisible(int index, bool visible);
    }
}
