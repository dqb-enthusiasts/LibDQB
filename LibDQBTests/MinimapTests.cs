using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQBTests;

[TestClass]
public class MinimapTests
{
    private static IReadOnlyList<MinimapShorelineKey> AllKeys = Enumerable.Range(0, 256)
        .Select(val => new MinimapShorelineKey((byte)val))
        .ToList();

    [TestMethod]
    public void verify_canonical_values()
    {
        Assert.HasCount(256, AllKeys);
        Assert.AreEqual(256, AllKeys.Distinct().Count());

        // Every key should have the same base tile ID as its canonical form.
        foreach (var key in AllKeys)
        {
            var canon = key.MakeCanonical();
            Assert.AreEqual(key.DeepSeaBaseTileId, canon.DeepSeaBaseTileId);
        }

        // There are 47 unique tile types.
        // This means we expect 47 unique base tiles...
        Assert.AreEqual(47, AllKeys.Select(key => key.DeepSeaBaseTileId).Distinct().Count());
        Assert.AreEqual(47, AllKeys.Select(key => key.ShallowSeaBaseTileId).Distinct().Count());
        // ...and 47 canonical forms.
        Assert.AreEqual(47, AllKeys.Select(key => key.MakeCanonical()).Distinct().Count());
    }

    [TestMethod]
    public void sanity_check_spacing()
    {
        // This test simply demonstrates the pattern between the deep sea and shallow sea tile values.
        // Unfortunately it seems pretty arbitrary to me.
        foreach (var key in AllKeys)
        {
            int difference = key.ShallowSeaBaseTileId - key.DeepSeaBaseTileId;

            // The spacing from Deep -> Shallow Sea always matches
            // the spacing from Shallow Sea -> Clear Water.
            Assert.AreEqual(difference, key.ClearWaterBaseTileId - key.ShallowSeaBaseTileId);

            if (key.DeepSeaBaseTileId == 0)
            {
                Assert.AreEqual(1, difference);
            }
            else if (key.DeepSeaBaseTileId < 32 * 3)
            {
                // bank 1 or 2 (within the first 3 lines of SheetRetro.png)
                Assert.AreEqual(15, difference);
            }
            else
            {
                // bank 3
                Assert.AreEqual(225, difference);
            }
        }
    }

    private static MinimapTile MakeTile(int value) => new MinimapTile { TileValue = value };

    private void do_tile_snapshot_test(string filename, Func<MinimapTile, bool> includeTile)
    {
        var snapshotFile = Path.Combine(FindProjectRoot().FullName, "LibDQBTests", "snapshots", filename);

        // Test wraparound
        const int from = 0x8000 * -2;
        const int to = 0x8000 * 2;

        var sb = new StringBuilder();

        for (int val = from; val <= to; val++)
        {
            var tile = MakeTile(val);
            if (includeTile(tile))
            {
                sb.Append("0x").Append(val.ToString("x8"));
                sb.Append($",{val},{tile.BaseTileId},{tile.QuirkyOverlay ?? tile.OverlayId}\n");
            }
        }

        var expected = File.ReadAllText(snapshotFile);
        var actual = sb.ToString();
        if (expected != actual)
        {
            var outPath = snapshotFile.Replace(filename, filename.Replace(".expected.", ".actual."));
            File.WriteAllText(outPath, actual);
            Assert.Fail($"Snapshot has changed. Compare {outPath} vs {filename}");
        }
    }

    [TestMethod]
    public void all_minimap_tiles_snapshot_test()
    {
        do_tile_snapshot_test("all-minimap-tiles.expected.csv", _ => true);
    }

    [TestMethod]
    public void legal_minimap_tiles_snapshot_test()
    {
        do_tile_snapshot_test("legal-minimap-tiles.expected.csv", tile => tile.BaseTileId.IsLegal);
    }

    [TestMethod]
    public void smoke_test_all_tiles()
    {
        // Test wraparound
        const int from = 0x8000 * -2;
        const int to = 0x8000 * 2;

        for (int val = from; val <= to; val++)
        {
            var tile = MakeTile(val);
            Assert.IsLessThan((int)SeaTypeIndex.END, (int)tile.SeaTypeIndex);
            Assert.IsTrue(tile.OverlayId >= 0 && tile.OverlayId < 11);

            for (int overlay = 0; overlay < 11; overlay++)
            {
                var other = tile.ReplaceOverlay(new OverlayId(overlay));
                if (tile.BaseTileId.IsLegal)
                {
                    Assert.AreEqual(tile.BaseTileId, other.BaseTileId);
                    Assert.AreEqual(overlay, other.OverlayId);
                }
                else
                {
                    // Overlay is always 0 when base tile is illegal
                    Assert.AreEqual(tile.BaseTileId, other.BaseTileId);
                    Assert.AreEqual(0, other.OverlayId);
                }
            }
        }
    }

    [TestMethod]
    public void quirky_overlay()
    {
        var quirkyOverlays = Enumerable.Range(0x8000, 0x8000)
            .Select(MakeTile)
            .Where(tile => tile.QuirkyOverlay.HasValue)
            .ToList();

        MinimapTile Quirky(int baseTileId)
        {
            return new MinimapTile { TileValue = 0xC000 + 1 + baseTileId * 11 };
        }

        // Sapphire found these 5 in original research.
        // Kramer has independently confirmed them.
        // So far there don't seem to be any more.
        Assert.HasCount(5, quirkyOverlays);
        int i = 0;
        Assert.AreEqual(Quirky(8), quirkyOverlays[i]);
        Assert.AreEqual(8, quirkyOverlays[i].QuirkyOverlay.GetValueOrDefault());
        i++;
        Assert.AreEqual(Quirky(9), quirkyOverlays[i]);
        Assert.AreEqual(10, quirkyOverlays[i].QuirkyOverlay.GetValueOrDefault());
        i++;
        Assert.AreEqual(Quirky(10), quirkyOverlays[i]);
        Assert.AreEqual(9, quirkyOverlays[i].QuirkyOverlay.GetValueOrDefault());
        i++;
        Assert.AreEqual(Quirky(11), quirkyOverlays[i]);
        Assert.AreEqual(7, quirkyOverlays[i].QuirkyOverlay.GetValueOrDefault());
        i++;
        Assert.AreEqual(Quirky(18), quirkyOverlays[i]);
        Assert.AreEqual(8, quirkyOverlays[i].QuirkyOverlay.GetValueOrDefault());
        i++;
        Assert.AreEqual(quirkyOverlays.Count, i);
    }

    [TestMethod]
    public void tile_swap_method_invariants()
    {
        for (int i = 0; i <= 0xFFFF; i++)
        {
            var orig = MakeTile(i);

            for (int tileId = 0; tileId <= BaseTileId.MaxLegalValue; tileId++)
            {
                var other = orig.ReplaceBaseTile(new BaseTileId(tileId));
                Assert.AreEqual(tileId, other.BaseTileId);
                Assert.IsFalse(other.IsQuirky); // never quirky
                Assert.AreEqual(orig.OverlayId, other.OverlayId); // overlay unchanged
                Assert.AreEqual(orig.IsVisible, other.IsVisible); // visibility unchanged
            }

            for (int overlay = 0; overlay < 11; overlay++)
            {
                var overlayId = new OverlayId(overlay);
                if (orig.BaseTileId.IsLegal)
                {
                    var other = orig.ReplaceOverlay(overlayId);
                    Assert.AreEqual(overlay, other.OverlayId);
                    Assert.IsFalse(other.IsQuirky); // never quirky
                    Assert.AreEqual(orig.BaseTileId, other.BaseTileId); // base tile unchanged
                    Assert.AreEqual(orig.IsVisible, other.IsVisible); // visibility unchanged
                }
                else
                {
                    // Illegal base tiles never have overlays, so attempting
                    // to replace its overlay is a nonsensical request.
                    // I think the best thing we can do here is ignore the request.
                    Assert.AreEqual(orig, orig.ReplaceOverlay(overlayId));
                }
            }

            foreach (var visible in new bool[] { true, false })
            {
                var other = orig.ReplaceVisibility(visible);
                Assert.AreEqual(visible, other.IsVisible);
                Assert.IsFalse(other.IsQuirky); // never quirky
                Assert.AreEqual(orig.BaseTileId, other.BaseTileId); // base tile unchanged
                Assert.AreEqual(orig.OverlayId, other.OverlayId); // overlay unchanged
            }
        }
    }

    private static DirectoryInfo FindProjectRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        do
        {
            if (dir.GetDirectories("LibDQBTests").Any())
            {
                return dir;
            }
            dir = dir.Parent ?? dir;
        } while (dir.Parent != null);

        throw new Exception("Failed to find project root");
    }
}
