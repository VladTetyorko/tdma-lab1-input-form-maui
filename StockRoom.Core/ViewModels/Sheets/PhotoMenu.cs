namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// Take / Pick / Remove, offered the same way wherever a product photo can be changed
public static class PhotoMenu
{
    public const string Take = "TakePhotoButton";
    public const string Pick = "PickPhotoButton";
    public const string Remove = "RemovePhotoButton";

    public static ActionSheetViewModel For(ProductPhotoEditor photo)
    {
        List<SheetAction> actions = [];
        if (photo.IsCaptureSupported)
        {
            actions.Add(new SheetAction(Take, "Take photo"));
        }

        actions.Add(new SheetAction(Pick, "Pick photo"));
        if (photo.HasPhoto)
        {
            actions.Add(new SheetAction(Remove, "Remove photo", true));
        }

        return new ActionSheetViewModel("Photo", [.. actions]);
    }
}
