// UNBEATABLE — Tradução PT-BR
// Plugin BepInEx 5 (Mono). Escrito em C# 5 para compilar com o csc do .NET Framework.
//
// Como funciona:
//  * Diálogos (Yarn Spinner 2.0): pós-patch em Yarn.Unity.Localization.GetLocalizedString(id).
//    Se existir tradução para o id da linha, troca o texto e mantém o prefixo "Personagem: ".
//  * Interface (Unity Localization): pós-patch em DetailedLocalizationTable<T>.GetEntry(...).
//    Quando a tabela inglesa entrega uma entrada, o valor é trocado pela tradução (tabela|chave).
// O jogo deve ficar em ENGLISH no menu de idioma: o mod substitui o inglês.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.Localization.Tables;

namespace UnbeatablePTBR
{
    [BepInPlugin(Guid, "UNBEATABLE PT-BR", "0.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "br.traducao.unbeatable";
        internal static ManualLogSource Log;
        internal static readonly Dictionary<string, string> Dialogue = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> UiByKey = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> UiByText = new Dictionary<string, string>();
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> LogMissing;
        internal static readonly HashSet<string> Reported = new HashSet<string>();

        void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("Geral", "Ativado", true, "Liga/desliga a tradução.");
            LogMissing = Config.Bind("Debug", "RegistrarFaltantes", false,
                "Registra no log (BepInEx/LogOutput.log) textos exibidos que ainda não têm tradução.");

            string dir = Path.Combine(Path.GetDirectoryName(Info.Location), "traducao");
            int d = LoadCsv(Path.Combine(dir, "dialogos.csv"), true);
            int u = LoadCsv(Path.Combine(dir, "interface.csv"), false);
            int f = LoadFixed(Path.Combine(dir, "fixos.csv"));
            Log.LogInfo(string.Format("Carregado: {0} falas, {1} textos de interface, {2} textos fixos.", d, u, f));

            var h = new Harmony(Guid);
            TryPatch(h, "Yarn.Unity.Localization, YarnSpinner.Unity", "GetLocalizedString",
                new[] { typeof(string) }, "YarnPostfix");
            var tableType = typeof(DetailedLocalizationTable<StringTableEntry>);
            TryPatch(h, tableType, "GetEntry", new[] { typeof(long) }, "EntryPostfix");
            TryPatch(h, tableType, "GetEntry", new[] { typeof(string) }, "EntryPostfix");
            // Textos fixos no código (avaliações, nomes de cor...): troca exata no TextMeshPro.
            if (Fixed.Count > 0)
            {
                TryPatch(h, typeof(TMPro.TMP_Text), "set_text", new[] { typeof(string) }, "TmpPrefix");
                TryPatch(h, typeof(TMPro.TMP_Text), "SetText", new[] { typeof(string), typeof(bool) }, "TmpPrefix");
            }
        }

        internal static readonly Dictionary<string, string> Fixed = new Dictionary<string, string>();

        static int LoadFixed(string path)
        {
            if (!File.Exists(path)) return 0;
            List<List<string>> rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
            if (rows.Count == 0) return 0;
            int iEn = rows[0].IndexOf("en"), iPt = rows[0].IndexOf("pt");
            for (int r = 1; r < rows.Count; r++)
                if (rows[r].Count > iPt && rows[r][iPt].Length > 0) Fixed[rows[r][iEn]] = rows[r][iPt];
            return Fixed.Count;
        }

        void TryPatch(Harmony h, string typeName, string method, Type[] args, string post)
        {
            Type t = Type.GetType(typeName);
            if (t == null) { Log.LogError("Tipo não encontrado: " + typeName); return; }
            TryPatch(h, t, method, args, post);
        }

        void TryPatch(Harmony h, Type t, string method, Type[] args, string post)
        {
            try
            {
                MethodInfo target = AccessTools.Method(t, method, args);
                MethodInfo pf = typeof(Patches).GetMethod(post, BindingFlags.Static | BindingFlags.Public);
                h.Patch(target, null, new HarmonyMethod(pf));
                Log.LogInfo("Patch ok: " + t.Name + "." + method);
            }
            catch (Exception e) { Log.LogError("Falha no patch " + t.Name + "." + method + ": " + e); }
        }

        // CSV: separador vírgula, aspas duplas, quebras de linha dentro de aspas, BOM UTF-8.
        static int LoadCsv(string path, bool dialogue)
        {
            if (!File.Exists(path)) { Log.LogWarning("Arquivo ausente: " + path); return 0; }
            List<List<string>> rows = ParseCsv(File.ReadAllText(path, Encoding.UTF8));
            if (rows.Count == 0) return 0;
            List<string> head = rows[0];
            int iPt = head.IndexOf("pt"), iEn = head.IndexOf("en"), iHash = head.IndexOf("en_md5");
            int n = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                if (row.Count <= iPt) continue;
                string pt = row[iPt];
                if (string.IsNullOrEmpty(pt)) continue;
                if (dialogue) Dialogue[row[head.IndexOf("id")]] = pt;
                else
                {
                    UiByKey[row[head.IndexOf("tabela")] + "|" + row[head.IndexOf("chave")]] = pt;
                    // Busca reserva pelo texto inglês, guardada como hash (md5, 10 hex)
                    // para o pacote não precisar distribuir o texto original.
                    string h = iHash >= 0 && row.Count > iHash ? row[iHash]
                             : (iEn >= 0 && row.Count > iEn && row[iEn].Length > 0 ? Hash(row[iEn]) : "");
                    if (h.Length > 0 && !UiByText.ContainsKey(h)) UiByText[h] = pt;
                }
                n++;
            }
            return n;
        }

        static readonly System.Security.Cryptography.MD5 Md5 = System.Security.Cryptography.MD5.Create();
        static readonly Dictionary<string, string> HashCache = new Dictionary<string, string>();

        internal static string Hash(string s)
        {
            string h;
            if (HashCache.TryGetValue(s, out h)) return h;
            byte[] b = Md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(10);
            for (int i = 0; i < 5; i++) sb.Append(b[i].ToString("x2"));
            h = sb.ToString();
            if (HashCache.Count < 20000) HashCache[s] = h;
            return h;
        }

        static List<List<string>> ParseCsv(string s)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool q = false;
            int i = (s.Length > 0 && s[0] == '﻿') ? 1 : 0;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (q)
                {
                    if (c == '"') { if (i + 1 < s.Length && s[i + 1] == '"') { cell.Append('"'); i++; } else q = false; }
                    else cell.Append(c);
                }
                else if (c == '"') q = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Length = 0; }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(cell.ToString()); cell.Length = 0; rows.Add(row); row = new List<string>(); }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }

    public static class Patches
    {
        static bool IsEnglish(string code)
        {
            return string.IsNullOrEmpty(code) || code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }

        // Prefixo "Personagem: " das linhas Yarn (id interno do falante, não é exibido assim).
        static int SpeakerPrefixLength(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0;
            int max = Math.Min(line.Length - 1, 42);
            for (int i = 0; i < max; i++)
            {
                char c = line[i];
                if (c == '<' || c == '[' || c == '\n') return 0;
                if (c == ':' && line[i + 1] == ' ') return i + 2;
            }
            return 0;
        }

        public static void YarnPostfix(object __instance, string key, ref string __result)
        {
            try
            {
                if (!Plugin.Enabled.Value || key == null) return;
                var loc = __instance as Yarn.Unity.Localization;
                if (loc == null || !IsEnglish(loc.LocaleCode)) return;
                string pt;
                if (Plugin.Dialogue.TryGetValue(key, out pt))
                {
                    int p = SpeakerPrefixLength(__result);
                    __result = (p > 0 ? __result.Substring(0, p) : "") + pt;
                }
                else if (Plugin.LogMissing.Value && Plugin.Reported.Add(key))
                    Plugin.Log.LogInfo("[sem tradução] " + key + " = " + __result);
            }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        public static void TmpPrefix(ref string __0)
        {
            string pt;
            if (__0 != null && __0.Length < 40 && Plugin.Enabled.Value && Plugin.Fixed.TryGetValue(__0, out pt))
                __0 = pt;
        }

        public static void EntryPostfix(object __instance, object __result)
        {
            try
            {
                if (!Plugin.Enabled.Value) return;
                var entry = __result as StringTableEntry;
                var table = __instance as StringTable;
                if (entry == null || table == null) return;
                if (!IsEnglish(table.LocaleIdentifier.Code)) return;
                string id = table.TableCollectionName + "|" + entry.Key;
                string pt;
                if (!Plugin.UiByKey.TryGetValue(id, out pt))
                {
                    string cur = entry.Value;
                    if (cur == null || !Plugin.UiByText.TryGetValue(Plugin.Hash(cur), out pt))
                    {
                        if (Plugin.LogMissing.Value && !string.IsNullOrEmpty(cur) && Plugin.Reported.Add(id))
                            Plugin.Log.LogInfo("[sem tradução] " + id + " = " + cur);
                        return;
                    }
                }
                if (entry.Value != pt) entry.Value = pt;
            }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }
}
