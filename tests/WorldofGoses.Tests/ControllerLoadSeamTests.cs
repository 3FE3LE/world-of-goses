using System;
using System.IO;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class ControllerLoadSeamTests
{
    [Fact]
    public void LoadV11Save_UsesControllerMigrationPathAndPersistsV12()
    {
        string slotsDirectory = Path.Combine(
            Path.GetTempPath(),
            $"world-of-goses-load-{Guid.NewGuid():N}");
        Directory.CreateDirectory(slotsDirectory);

        try
        {
            var save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
            save.Version = 11;
            foreach (var citizen in save.Citizens)
            {
                citizen.AppearanceVariant = null;
            }
            string path = Path.Combine(slotsDirectory, "save_slot_0.json");
            File.WriteAllText(path, WorldPersistence.SerializeToJson(save));

            var loaded = WorldPersistence.DeserializeFromJson(File.ReadAllText(path));
            var migrated = WorldPersistence.MigrateToCurrent(loaded);
            WorldPersistence.Validate(migrated);
            WorldPersistence.WriteToFile(migrated, path);

            var persisted = WorldPersistence.LoadFromSlot(0, slotsDirectory);
            Assert.Equal(WorldSave.CurrentVersion, persisted.Version);
            Assert.All(
                persisted.Citizens,
                citizen => Assert.Equal("standard", citizen.AppearanceVariant));
        }
        finally
        {
            WorldPersistence.DeleteSlot(0, slotsDirectory);
            Directory.Delete(slotsDirectory, recursive: true);
        }
    }
}
