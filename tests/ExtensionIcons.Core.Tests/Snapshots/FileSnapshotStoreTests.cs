using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.Core.Tests.Snapshots;

public sealed class FileSnapshotStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ExtensionIconsTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private static Snapshot Sample(long sequence, string description = "sample") => new(
        Guid.NewGuid(),
        sequence,
        new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc),
        description,
        [".htm", ".html"],
        [
            new RegistryValueChange(RegistryHive.CurrentUser, @"Software\Classes\MSEdgeHTM\DefaultIcon", "", null, @"Software\Classes\MSEdgeHTM"),
            new RegistryValueChange(RegistryHive.CurrentUser, @"Software\Classes\.htm", "", "htmlfile", null),
        ]);

    [Fact]
    public void A_snapshot_survives_a_round_trip_through_a_new_store_instance()
    {
        var snapshot = Sample(1);

        new FileSnapshotStore(_directory).Save(snapshot);
        var loaded = Assert.Single(new FileSnapshotStore(_directory).List());

        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Equal(snapshot.Sequence, loaded.Sequence);
        Assert.Equal(snapshot.CreatedUtc, loaded.CreatedUtc);
        Assert.Equal(snapshot.Description, loaded.Description);
        Assert.Equal(snapshot.Extensions, loaded.Extensions);
        Assert.Equal(snapshot.Changes, loaded.Changes);
        Assert.Null(loaded.RevertedUtc);
    }

    [Fact]
    public void Saving_again_with_the_same_id_replaces_the_snapshot()
    {
        var store = new FileSnapshotStore(_directory);
        var snapshot = Sample(1);
        store.Save(snapshot);

        var reverted = DateTime.UtcNow;
        store.Save(snapshot with { RevertedUtc = reverted });

        var loaded = Assert.Single(store.List());
        Assert.True(loaded.IsReverted);
    }

    [Fact]
    public void List_orders_by_sequence_not_by_file_name()
    {
        var store = new FileSnapshotStore(_directory);
        store.Save(Sample(3, "third"));
        store.Save(Sample(1, "first"));
        store.Save(Sample(2, "second"));

        Assert.Equal(["first", "second", "third"], store.List().Select(s => s.Description));
    }

    [Fact]
    public void List_of_a_missing_folder_is_empty()
    {
        Assert.Empty(new FileSnapshotStore(_directory).List());
    }

    [Fact]
    public void An_unreadable_snapshot_file_is_an_error_not_silently_skipped()
    {
        var store = new FileSnapshotStore(_directory);
        store.Save(Sample(1));
        File.WriteAllText(Path.Combine(_directory, "broken.json"), "{ not json");

        Assert.Throws<InvalidDataException>(() => store.List());
    }

    [Fact]
    public void Saving_leaves_no_temporary_files_behind()
    {
        new FileSnapshotStore(_directory).Save(Sample(1));

        Assert.Empty(Directory.EnumerateFiles(_directory, "*.tmp"));
    }
}
