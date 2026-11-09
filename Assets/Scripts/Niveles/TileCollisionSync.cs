using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PrimalEcho
{
    // Al darle Play, recalcula las cajas de colisión a partir de lo que esté pintado en "mapa"
    // y "Ramas_Un_Sentido". Así nunca quedan paredes invisibles aunque no hayas guardado.
    public static class TileCollisionSync
    {
        private const int TerrainLayer = 6;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Sync();
            SceneManager.sceneLoaded += (_, __) => Sync();
        }

        private static void Sync()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return;
            var solidBoxes = grid.transform.Find("Colision_Suelo");
            if (solidBoxes == null) return; // solo en los niveles generados
            var solid = grid.transform.Find("mapa")?.GetComponent<Tilemap>();
            if (solid != null) Rebuild(solidBoxes.gameObject, solid, false);
            var branchBoxes = grid.transform.Find("Colision_Ramas");
            var branches = grid.transform.Find("Ramas_Un_Sentido")?.GetComponent<Tilemap>();
            if (branchBoxes != null && branches != null) Rebuild(branchBoxes.gameObject, branches, true);
        }

        private static void Rebuild(GameObject owner, Tilemap map, bool effector)
        {
            foreach (var old in owner.GetComponents<BoxCollider2D>()) Object.Destroy(old);
            owner.layer = TerrainLayer;
            map.CompressBounds();
            BoundsInt b = map.cellBounds;
            var open = new Dictionary<(int, int), int>();
            for (int y = b.yMin; y <= b.yMax; y++)
            {
                var runs = new HashSet<(int, int)>();
                if (y < b.yMax)
                    for (int x = b.xMin; x < b.xMax;)
                    {
                        if (!map.HasTile(new Vector3Int(x, y, 0))) { x++; continue; }
                        int x0 = x;
                        while (x < b.xMax && map.HasTile(new Vector3Int(x, y, 0))) x++;
                        runs.Add((x0, x));
                    }
                foreach (var run in open.Keys.ToList())
                    if (!runs.Contains(run))
                    {
                        var box = owner.AddComponent<BoxCollider2D>();
                        box.offset = new Vector2((run.Item1 + run.Item2) / 2f, (open[run] + y) / 2f);
                        box.size = new Vector2(run.Item2 - run.Item1, y - open[run]);
                        box.usedByEffector = effector;
                        open.Remove(run);
                    }
                foreach (var run in runs)
                    if (!open.ContainsKey(run)) open[run] = y;
            }
        }
    }
}
