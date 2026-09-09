using System;
using SimpleFileBrowser;

namespace Warlander.Deedplanner.Persistence
{
    public class MapSavePicker : IMapSavePicker
    {
        public bool Show(Action<string[]> onSuccess, Action onCancel, string suggestedName)
        {
            FileBrowser.SetFilters(false, new FileBrowser.Filter("DeedPlanner 3 save", "MAP"));
            return FileBrowser.ShowSaveDialog(
                paths => onSuccess(paths),
                () => onCancel(),
                FileBrowser.PickMode.Files,
                initialFilename: suggestedName,
                title: "Save Map",
                saveButtonText: "Save");
        }
    }
}
