using LibDQB;
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

    private static MinimapTile MakeTile(int value) => MinimapTile.FromRawValue(value);

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
                sb.Append($",{val},{tile.BaseTileId},{tile.ApparentOverlayId}\n");
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
            Assert.IsTrue(tile.FormulaicOverlayId >= 0 && tile.FormulaicOverlayId < 11);
            Assert.IsTrue(tile.ApparentOverlayId >= 0 && tile.ApparentOverlayId < 11);

            for (int overlay = 0; overlay < 11; overlay++)
            {
                var other = tile.ReplaceOverlay(new OverlayId(overlay));
                if (tile.BaseTileId.IsLegal)
                {
                    Assert.AreEqual(tile.BaseTileId, other.BaseTileId);
                    Assert.AreEqual(overlay, other.FormulaicOverlayId);
                    Assert.AreEqual(overlay, other.ApparentOverlayId);
                }
                else
                {
                    // Overlay is always 0 when base tile is illegal
                    Assert.AreEqual(tile.BaseTileId, other.BaseTileId);
                    Assert.AreEqual(0, other.FormulaicOverlayId);
                }
            }
        }
    }

    [TestMethod]
    public void quirky_overlay()
    {
        var quirkyOverlays = Enumerable.Range(0x8000, 0x8000)
            .Select(MakeTile)
            .Where(tile => tile.ApparentOverlayId != tile.FormulaicOverlayId)
            .ToList();

        MinimapTile Quirky(int baseTileId)
        {
            var tile = MinimapTile.FromRawValue(0xC000 + 1 + baseTileId * 11);
            Assert.IsTrue(tile.IsQuirky);
            Assert.AreEqual(0, tile.FormulaicOverlayId);
            return tile;
        }

        // Sapphire found these 5 in original research.
        // Kramer has independently confirmed them.
        // So far there don't seem to be any more.
        Assert.HasCount(5, quirkyOverlays);
        int i = 0;
        Assert.AreEqual(Quirky(8), quirkyOverlays[i]);
        Assert.AreEqual(8, quirkyOverlays[i].ApparentOverlayId);
        i++;
        Assert.AreEqual(Quirky(9), quirkyOverlays[i]);
        Assert.AreEqual(10, quirkyOverlays[i].ApparentOverlayId);
        i++;
        Assert.AreEqual(Quirky(10), quirkyOverlays[i]);
        Assert.AreEqual(9, quirkyOverlays[i].ApparentOverlayId);
        i++;
        Assert.AreEqual(Quirky(11), quirkyOverlays[i]);
        Assert.AreEqual(7, quirkyOverlays[i].ApparentOverlayId);
        i++;
        Assert.AreEqual(Quirky(18), quirkyOverlays[i]);
        Assert.AreEqual(8, quirkyOverlays[i].ApparentOverlayId);
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
                // Normalizing down to non-quirky helps us ensure that Apparent Overlay is unchanged.
                // (Note that Formulaic Overlay can change here.)
                var other = orig.ReplaceBaseTile(new BaseTileId(tileId));
                // == changed ==
                Assert.AreEqual(tileId, other.BaseTileId);
                Assert.IsFalse(other.IsQuirky);
                // == unchanged ==
                Assert.AreEqual(orig.ApparentOverlayId, other.ApparentOverlayId);
                Assert.AreEqual(orig.IsVisible, other.IsVisible);
            }

            for (int overlay = 0; overlay < 11; overlay++)
            {
                if (orig.BaseTileId.IsLegal)
                {
                    // Normalizing down to non-quirky helps us ensure that the Apparent Overlay is
                    // changed to the requested value. This implies that Formulaic Overlay will
                    // match the requested value also.
                    var other = orig.ReplaceOverlay(new OverlayId(overlay));
                    // == changed ==
                    Assert.IsFalse(other.IsQuirky);
                    Assert.AreEqual(overlay, other.ApparentOverlayId);
                    Assert.AreEqual(overlay, other.FormulaicOverlayId);
                    // == unchanged ==
                    Assert.AreEqual(orig.IsVisible, other.IsVisible);
                    Assert.AreEqual(orig.BaseTileId, other.BaseTileId);
                }
                else
                {
                    // Illegal base tiles never have (apparent) overlays, so attempting
                    // to replace its overlay is a nonsensical request.
                    // I think the best thing we can do here is ignore the request.
                    Assert.AreEqual(orig, orig.ReplaceOverlay(new OverlayId(overlay)));
                }
            }

            foreach (var visible in new bool[] { true, false })
            {
                // It makes sense to preserve quirkiness, otherwise we might have to
                // do extra work to keep the Apparent Overlay unchanged.
                var other = orig.ReplaceVisibility(visible);
                // == changed ==
                Assert.AreEqual(visible, other.IsVisible);
                // == unchanged ==
                Assert.AreEqual(orig.IsQuirky, other.IsQuirky);
                Assert.AreEqual(orig.BaseTileId, other.BaseTileId);
                Assert.AreEqual(orig.FormulaicOverlayId, other.FormulaicOverlayId);
                Assert.AreEqual(orig.ApparentOverlayId, other.ApparentOverlayId);
            }

            {
                var other = orig.RemoveQuirkiness();
                // == changed ==
                Assert.IsFalse(other.IsQuirky);
                // == unchanged ==
                Assert.AreEqual(orig.IsVisible, other.IsVisible);
                Assert.AreEqual(orig.BaseTileId, other.BaseTileId);
                Assert.AreEqual(orig.ApparentOverlayId, other.ApparentOverlayId);
            }
        }
    }

    [TestMethod]
    public void basic_shoreline_test()
    {
        var grid = new Array2D<MinimapTile>(new Rect(XZ.Zero, new XZ(10, 10)), MinimapTile.FromRawValue(0x8001));
        var landTile = MinimapTile.FromRawValue(0x8001).ReplaceBaseTile(new BaseTileId(3));

        // Set it up so that 3,0 has land to the S.
        // NW, N, and NE are out-of-bounds and thus treated as sea.
        // All other neighbors are true sea.
        grid.Set(new XZ(3, 1), landTile);
        var key = MinimapShorelineKey.Compute(new XZ(3, 0), grid);
        Assert.AreEqual(28 + 15 * 0, key.DeepSeaBaseTileId);
        Assert.AreEqual(28 + 15 * 1, key.ShallowSeaBaseTileId);
        Assert.AreEqual(28 + 15 * 2, key.ClearWaterBaseTileId);
    }

    [TestMethod]
    public void illegal_tile_shoreline_regression()
    {
        // Illegal tiles should not count as land when computing shorelines,
        void Test(MinimapTile seaTile)
        {
            Assert.IsFalse(seaTile.BaseTileId.IsLegal);
            Assert.AreEqual(SeaType.IllegalTile, seaTile.SeaType);

            var grid = new Array2D<MinimapTile>(new Rect(XZ.Zero, new XZ(3, 3)), seaTile);
            var landTile = MinimapTile.FromRawValue(0x8001).ReplaceBaseTile(new BaseTileId(3));
            XZ landXZ = new XZ(1, 1);
            grid.Set(landXZ, landTile);

            var key = MinimapShorelineKey.Compute(landXZ, grid);
            Assert.AreEqual(0, key.Value);
            Assert.AreEqual(0, key.DeepSeaBaseTileId);
            Assert.AreEqual(1, key.ShallowSeaBaseTileId);
            Assert.AreEqual(2, key.ClearWaterBaseTileId);
        }

        Test(MinimapTile.FromRawValue(0));
        Test(MinimapTile.FromRawValue(0x4000));
        Test(MinimapTile.FromRawValue(0x8000));
        Test(MinimapTile.FromRawValue(0xC000));
        Test(MinimapTile.FromRawValue(0xC000 - 67)); // a "random" illegal tile
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
