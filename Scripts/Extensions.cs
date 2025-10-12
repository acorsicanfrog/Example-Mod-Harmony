// ==================================================================================
// Extension Methods: Player Border Detection
//
// Demonstrates how to extend gameplay functionality with Harmony and C# extension 
// methods. Adds a system to compute and visualize borders between players on the map.
//
// Key Features:
// - Harmony Patch (MapGO.Start):
//      • When the map scene starts, gathers all borders for the current player.
//      • Sorts borders from longest to shortest (by shared hex edges).
//      • Highlights player borders visually in the scene using color overlays:
//          ◦ Cyan = Current player's tiles
//          ◦ Red  = Opponent's tiles
// - Extension Method: Player.GatherBorders(Map):
//      • Calculates all shared edges between the player’s territory and others.
//      • Builds one `BorderData` object per opponent.
//      • Efficiently deduplicates tiles and counts border edge lengths.
// - BorderData Class:
//      • Stores border metrics (edge count, tile sets) for each opponent.
//      • Provides quick access to `MyTilesCount`, `TheirTilesCount`, and both tile sets.
//      • Used to visualize and analyze territorial boundaries.
// - Performance Considerations:
//      • Uses hash sets to prevent duplicate tiles.
//      • Starts small dictionary capacities to reduce memory allocations.
//
// Usage:
// This system can be used for visual debugging, AI evaluation of front lines, or 
// strategic analysis mods.  
// Extend `DisplayBorders()` or `BorderData` to implement gameplay effects (e.g., 
// front-line bonuses, diplomacy scoring, or tactical overlays).
// ==================================================================================


using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

public static class Extensions
{
    [HarmonyPatch(typeof(MapGO), "Start")]
    public static class MapGO_Start_Void_Postfix
    {
        static void Postfix(MapGO __instance)
        {
            // Gathering all borders from current player
            List<BorderData> borders = TurnManager.currPlayer.GatherBorders(GameData.Instance.map);

            // Sorting from longer to shorter, optional, just to show how it's done
            borders.Sort((a, b) => b.EdgeCount.CompareTo(a.EdgeCount));

            DisplayBorders(borders);
        }

        static void DisplayBorders(List<BorderData> p_borders)
        {
            // Showing the borders
            foreach (BorderData b in p_borders)
            {
                // 'length' here = number of shared hex edges (fast, robust)
                Debug.Log($"{TurnManager.currPlayer.Country} vs {b.OtherPlayer.Country}: edges={b.EdgeCount}, myTiles={b.MyTilesCount}");

                foreach (var t in b.MyTiles) t.tileGO.GetComponent<SpriteRenderer>().color = Color.cyan;
                foreach (var t in b.TheirTiles) t.tileGO.GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
    }

    public sealed class BorderData
    {
        public Player OtherPlayer { get; private set; }

        /// <summary>Number of shared edges with OtherPlayer (hex edges).</summary>
        public int EdgeCount { get; internal set; }

        /// <summary>Your tiles that touch this opponent (deduped).</summary>
        public IReadOnlyCollection<Tile> MyTiles => _myTiles;

        /// <summary>Opponent tiles that touch you (deduped).</summary>
        public IReadOnlyCollection<Tile> TheirTiles => _theirTiles;

        public int MyTilesCount => _myTiles.Count;
        public int TheirTilesCount => _theirTiles.Count;

        private readonly HashSet<Tile> _myTiles = new HashSet<Tile>();
        private readonly HashSet<Tile> _theirTiles = new HashSet<Tile>();

        public BorderData(Player otherPlayer)
        {
            OtherPlayer = otherPlayer;
        }

        internal void AddMy(Tile t) { _myTiles.Add(t); }
        internal void AddTheir(Tile t) { _theirTiles.Add(t); }
    }

    /// <summary>
    /// Computes all borders between 'player' and every other player on the map.
    /// One BorderData per opponent (aggregated across the map).
    /// Border length = number of shared hex edges.
    /// MyTiles = set of player's tiles that touch that opponent.
    /// </summary>
    public static List<BorderData> GatherBorders(this Player player, Map p_map)
    {
        // Usually there are few neighbors; start small to reduce allocs.
        var byOpponent = new Dictionary<Player, BorderData>(capacity: 8);

        Tile[,] tiles = p_map.TilesTable;

        int sizeX = p_map.SizeX, sizeY = p_map.SizeY;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Tile t = tiles[x, y];

                if (t == null) continue;

                if (!ReferenceEquals(t.terrainOwner, player)) continue;

                var neighs = t.neighbours;

                if (neighs == null || neighs.Count == 0) continue;

                // Examine each neighbor once; we only count the edge from our side.
                for (int i = 0; i < neighs.Count; i++)
                {
                    Tile n = neighs[i];

                    if (n == null) continue;

                    Player other = n.terrainOwner;

                    if (other == null || ReferenceEquals(other, player))
                        continue; // not a border edge with another player

                    if (!byOpponent.TryGetValue(other, out var bd))
                    {
                        bd = new BorderData(other);
                        byOpponent.Add(other, bd);
                    }

                    // Count the shared edge (length += 1 edge).
                    bd.EdgeCount++;

                    // Record exact tiles (deduped by reference).
                    bd.AddMy(t);
                    bd.AddTheir(n);
                }
            }
        }

        // If desired, you can sort by longest border first:
        // var list = byOpponent.Values.ToList();
        // list.Sort((a,b) => b.EdgeCount.CompareTo(a.EdgeCount));
        // return list;

        return new List<BorderData>(byOpponent.Values);
    }
}