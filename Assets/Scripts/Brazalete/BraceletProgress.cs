using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrimalEcho
{
    // Un único registro guarda el saldo y las compras en la misma transacción.
    public sealed class BraceletProgress : MonoBehaviour
    {
        private const string SaveKey = "PrimalEcho_Brazalete_v1";
        [Serializable] private sealed class SaveData
        {
            public int fragments;
            public List<string> purchased = new List<string>();
            public int fruits, cores;
            public List<string> chipIds = new List<string>();
            public List<string> chipTitles = new List<string>();
            public List<string> chipTexts = new List<string>();
        }

        public const int MaxFruits = 3;
        public int Fruits => data.fruits;
        public int Cores => data.cores;
        public int ChipCount => data.chipIds.Count;
        public string ChipTitle(int index) => data.chipTitles[index];
        public string ChipText(int index) => data.chipTexts[index];
        public bool HasChip(string id) => data.chipIds.Contains(id);

        public bool AddFruit()
        {
            if (data.fruits >= MaxFruits) return false;
            data.fruits++;
            Save();
            return true;
        }

        /// <summary>Come una fruta: cura un corazón. No se gasta si la vida ya está llena.</summary>
        public bool UseFruit()
        {
            if (data.fruits <= 0 || !TryGetComponent(out KaelHealth health) || !health.Heal(1)) return false;
            data.fruits--;
            Save();
            return true;
        }

        public void AddCore() { data.cores++; Save(); }

        public void AddChip(string id, string title, string text)
        {
            if (HasChip(id)) return;
            data.chipIds.Add(id); data.chipTitles.Add(title ?? ""); data.chipTexts.Add(text ?? "");
            Save();
        }

        private SaveData data;
        private BraceletSettings settings;
        private readonly HashSet<string> applied = new HashSet<string>();
        public int Fragments => data.fragments;
        public BraceletSettings Settings => settings;
        public bool Owns(string id) => data.purchased.Contains(id);

        private void Awake()
        {
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "{}")); }
            catch (ArgumentException) { data = new SaveData(); }
            if (data == null) data = new SaveData();
            if (data.purchased == null) data.purchased = new List<string>();
            data.fragments = Mathf.Max(0, data.fragments);
            data.fruits = Mathf.Clamp(data.fruits, 0, MaxFruits);
            data.cores = Mathf.Max(0, data.cores);
            if (data.chipIds == null) data.chipIds = new List<string>();
            if (data.chipTitles == null) data.chipTitles = new List<string>();
            if (data.chipTexts == null) data.chipTexts = new List<string>();
            while (data.chipTitles.Count < data.chipIds.Count) data.chipTitles.Add("");
            while (data.chipTexts.Count < data.chipIds.Count) data.chipTexts.Add("");
        }

        private void OnEnable() => EchoShard.Collected += Collect;
        private void OnDisable() => EchoShard.Collected -= Collect;

        public void Configure(BraceletSettings value)
        {
            settings = value;
            ApplyPurchased();
        }

        private void Start() => ApplyPurchased();

        private void Collect()
        {
            if (data.fragments < int.MaxValue) data.fragments++;
            Save();
        }

        public bool Available(int index)
        {
            var upgrade = settings.upgrades[index];
            int parent = upgrade.prerequisite;
            return parent < 0 || (parent < settings.upgrades.Length && Owns(settings.upgrades[parent].id));
        }

        public bool TryPurchase(int index)
        {
            if (settings == null || index < 0 || index >= settings.upgrades.Length) return false;
            var upgrade = settings.upgrades[index];
            if (string.IsNullOrEmpty(upgrade.id) || Owns(upgrade.id) || !Available(index) || data.fragments < upgrade.cost || data.cores < upgrade.coreCost) return false;
            data.fragments -= Mathf.Max(0, upgrade.cost);
            data.cores -= Mathf.Max(0, upgrade.coreCost);
            data.purchased.Add(upgrade.id);
            Save();
            ApplyPurchased();
            SincronizacionPartida.Guardar();
            return true;
        }

        /// <summary>Aplica en este componente un DatosPartida descargado de la nube y lo guarda en PlayerPrefs.</summary>
        public void AplicarDatosPartida(DatosPartida partida)
        {
            if (partida == null) return;
            if (data == null) data = LeerDatosPrefs();
            FusionarConDatosPartida(data, partida);
            Save();
            ApplyPurchased();
        }

        /// <summary>Guarda un DatosPartida en PlayerPrefs y actualiza el BraceletProgress activo si existe en la escena.</summary>
        public static void AplicarDesdeNube(DatosPartida partida)
        {
            if (partida == null) return;
            var activo = FindFirstObjectByType<BraceletProgress>();
            if (activo != null)
            {
                activo.AplicarDatosPartida(partida);
                return;
            }
            SaveData local = LeerDatosPrefs();
            FusionarConDatosPartida(local, partida);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(local));
            PlayerPrefs.Save();
        }

        /// <summary>Copia fragmentos, frutas, núcleos, mejoras y chips actuales hacia un DatosPartida.</summary>
        public static void RellenarDatosPartida(DatosPartida destino)
        {
            if (destino == null) return;
            var activo = FindFirstObjectByType<BraceletProgress>();
            SaveData origen = activo != null && activo.data != null ? activo.data : LeerDatosPrefs();
            destino.fragmentos = Mathf.Max(0, origen.fragments);
            destino.frutas = Mathf.Clamp(origen.fruits, 0, MaxFruits);
            destino.nucleos = Mathf.Max(0, origen.cores);
            destino.mejoras = origen.purchased != null ? new List<string>(origen.purchased) : new List<string>();
            destino.chips = origen.chipIds != null ? new List<string>(origen.chipIds) : new List<string>();
        }

        private static SaveData LeerDatosPrefs()
        {
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "{}")); }
            catch (ArgumentException) { d = new SaveData(); }
            if (d == null) d = new SaveData();
            if (d.purchased == null) d.purchased = new List<string>();
            if (d.chipIds == null) d.chipIds = new List<string>();
            if (d.chipTitles == null) d.chipTitles = new List<string>();
            if (d.chipTexts == null) d.chipTexts = new List<string>();
            return d;
        }

        private static void FusionarConDatosPartida(SaveData destino, DatosPartida partida)
        {
            destino.fragments = Mathf.Max(0, partida.fragmentos);
            destino.fruits = Mathf.Clamp(partida.frutas, 0, MaxFruits);
            destino.cores = Mathf.Max(0, partida.nucleos);

            destino.purchased.Clear();
            if (partida.mejoras != null)
            {
                foreach (string m in partida.mejoras)
                    if (!string.IsNullOrEmpty(m) && !destino.purchased.Contains(m))
                        destino.purchased.Add(m);
            }

            // Conserva título y texto si el chip ya existía localmente.
            var titulosPrevios = new Dictionary<string, string>();
            var textosPrevios = new Dictionary<string, string>();
            for (int i = 0; i < destino.chipIds.Count; i++)
            {
                string id = destino.chipIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (i < destino.chipTitles.Count) titulosPrevios[id] = destino.chipTitles[i];
                if (i < destino.chipTexts.Count) textosPrevios[id] = destino.chipTexts[i];
            }

            destino.chipIds.Clear();
            destino.chipTitles.Clear();
            destino.chipTexts.Clear();
            if (partida.chips != null)
            {
                foreach (string id in partida.chips)
                {
                    if (string.IsNullOrEmpty(id) || destino.chipIds.Contains(id)) continue;
                    destino.chipIds.Add(id);
                    destino.chipTitles.Add(titulosPrevios.TryGetValue(id, out string t) && !string.IsNullOrEmpty(t) ? t : id);
                    destino.chipTexts.Add(textosPrevios.TryGetValue(id, out string tx) ? tx : "");
                }
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void ApplyPurchased()
        {
            if (settings == null || settings.upgrades == null) return;
            foreach (var upgrade in settings.upgrades)
            {
                if (!Owns(upgrade.id) || applied.Contains(upgrade.id)) continue;
                int amount = Mathf.Max(1, upgrade.amount);
                switch (upgrade.effect)
                {
                    case BraceletUpgradeEffect.Health:
                        if (!TryGetComponent(out KaelHealth health)) continue;
                        health.IncreaseMaxHealth(amount);
                        break;
                    case BraceletUpgradeEffect.SpearDamage:
                        if (!TryGetComponent(out KaelSpear spear)) continue;
                        spear.AddDamage(amount);
                        break;
                    case BraceletUpgradeEffect.ArrowDamage:
                        if (!TryGetComponent(out KaelShooter shooter)) continue;
                        shooter.AddDamage(amount);
                        break;
                }
                applied.Add(upgrade.id);
            }
        }
    }
}
