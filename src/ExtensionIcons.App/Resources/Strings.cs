using System.Resources;

namespace ExtensionIcons.App.Resources;

/// <summary>
/// UI strings from Strings.resx (and, from v1.0, its per-culture siblings). Properties are
/// static so XAML can bind them with <c>{x:Static res:Strings.Name}</c>.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager = new("ExtensionIcons.App.Resources.Strings", typeof(Strings).Assembly);

    public static string Get(string name) => Manager.GetString(name) ?? $"!{name}!";

    public static string WindowTitle => Get(nameof(WindowTitle));
    public static string SearchLabel => Get(nameof(SearchLabel));
    public static string SearchHint => Get(nameof(SearchHint));
    public static string FilterCatalogOnly => Get(nameof(FilterCatalogOnly));
    public static string FilterUserChoiceOnly => Get(nameof(FilterUserChoiceOnly));
    public static string ColumnIcon => Get(nameof(ColumnIcon));
    public static string ColumnExtension => Get(nameof(ColumnExtension));
    public static string ColumnDescription => Get(nameof(ColumnDescription));
    public static string ColumnOwner => Get(nameof(ColumnOwner));
    public static string ColumnProgId => Get(nameof(ColumnProgId));
    public static string ColumnSource => Get(nameof(ColumnSource));
    public static string ColumnCatalogIcon => Get(nameof(ColumnCatalogIcon));
    public static string ColumnCatalogFormat => Get(nameof(ColumnCatalogFormat));
    public static string SourceUserChoice => Get(nameof(SourceUserChoice));
    public static string SourceClassDefault => Get(nameof(SourceClassDefault));
    public static string SourceNone => Get(nameof(SourceNone));
    public static string StatusLoading => Get(nameof(StatusLoading));
    public static string StatusShowing => Get(nameof(StatusShowing));
    public static string StatusLoadFailed => Get(nameof(StatusLoadFailed));
    public static string SourceOverridden => Get(nameof(SourceOverridden));
    public static string ButtonUseCatalogIcon => Get(nameof(ButtonUseCatalogIcon));
    public static string ButtonChooseIcon => Get(nameof(ButtonChooseIcon));
    public static string ButtonRestoreSelected => Get(nameof(ButtonRestoreSelected));
    public static string ButtonRestoreAll => Get(nameof(ButtonRestoreAll));
    public static string DialogTitle => Get(nameof(DialogTitle));
    public static string PickIconTitle => Get(nameof(PickIconTitle));
    public static string PickIconFilter => Get(nameof(PickIconFilter));
    public static string ConfirmSharedMessage => Get(nameof(ConfirmSharedMessage));
    public static string ConfirmRestoreAllMessage => Get(nameof(ConfirmRestoreAllMessage));
    public static string StatusIconChanged => Get(nameof(StatusIconChanged));
    public static string StatusRestored => Get(nameof(StatusRestored));
    public static string StatusNothingToRestore => Get(nameof(StatusNothingToRestore));
    public static string ErrorNotRegistered => Get(nameof(ErrorNotRegistered));
    public static string ErrorInvalidIcon => Get(nameof(ErrorInvalidIcon));
    public static string ErrorNotIco => Get(nameof(ErrorNotIco));
    public static string ErrorFileMissing => Get(nameof(ErrorFileMissing));
    public static string ErrorNoProgId => Get(nameof(ErrorNoProgId));
    public static string ErrorGeneric => Get(nameof(ErrorGeneric));
}
