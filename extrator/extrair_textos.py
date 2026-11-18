"""
Extrator dos textos originais (inglês) do UNBEATABLE.

O repositório distribui só o português. Este script tira o texto original
da SUA cópia do jogo, para você traduzir, revisar ou atualizar a tradução
quando o jogo receber patch.

Requisitos: Python 3.9+ e UnityPy
    pip install UnityPy

Uso (na pasta do repositório):
    python extrator/extrair_textos.py
    python extrator/extrair_textos.py --jogo "D:\\SteamLibrary\\steamapps\\common\\UNBEATABLE"

Gera em textos_originais/ (essa pasta fica fora do git):
    interface_en.csv   tabela, chave, id, en          (menus, opções, desafios...)
    dialogos_en.csv    id, personagem, en             (falas da história)
    interface.csv      tabela, chave, id, en, pt, situacao   } inglês lado a lado com a
    dialogos.csv       id, personagem, en, pt, situacao       } tradução de traducao/

"situacao" mostra: ok | falta (sem tradução) | mudou (o inglês mudou depois que
a linha foi traduzida; só para interface) | sobrando (tradução de algo que não
existe mais no jogo).

Os interface.csv/dialogos.csv completos também funcionam direto no plugin:
dá para copiá-los para BepInEx/plugins/UnbeatablePTBR/traducao/ (o plugin
aceita o formato com a coluna "en").
"""
import argparse, csv, hashlib, os, re, struct, sys

try:
    import UnityPy
except ImportError:
    sys.exit("Falta o UnityPy. Instale com:  pip install UnityPy")

JOGO_PADRAO = r"C:\Program Files (x86)\Steam\steamapps\common\UNBEATABLE"
md5 = lambda t: hashlib.md5(t.encode("utf-8")).hexdigest()[:10]
norm = lambda s: s.replace("\r\n", "\n")


# ---------------------------------------------------------------- interface
def extrair_interface(jogo):
    """Tabelas do Unity Localization (inglês) + chaves compartilhadas."""
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
    aa = os.path.join(jogo, "UNBEATABLE_Data", "StreamingAssets", "aa", "StandaloneWindows64")
    tabelas = os.path.join(aa, "localization-string-tables-english_assets_all.bundle")
    compart = os.path.join(aa, "localization-assets-shared_assets_all.bundle")
    for p in (tabelas, compart):
        if not os.path.exists(p):
            sys.exit("Não achei %s\nConfira o caminho do jogo (--jogo)." % p)

    env = UnityPy.load(tabelas)
    ver = env.objects[0].assets_file.unity_version
    gen = TypeTreeGenerator(ver)
    gen.load_local_game(jogo)          # lê os DLLs de UNBEATABLE_Data/Managed
    UnityPy.config.FALLBACK_UNITY_VERSION = ver

    chaves, entradas = {}, {}
    for caminho in (compart, tabelas):
        e = UnityPy.load(caminho)
        e.typetree_generator = gen
        for o in e.objects:
            if o.type.name != "MonoBehaviour":
                continue
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if "m_TableData" in t:                       # StringTable (_en)
                col = t.get("m_Name", "").rsplit("_", 1)[0]
                entradas[col] = {str(r["m_Id"]): r.get("m_Localized", "") for r in t["m_TableData"]}
            elif "m_Entries" in t and "m_TableCollectionName" in t:   # SharedTableData
                chaves[t["m_TableCollectionName"]] = {str(x["m_Id"]): x["m_Key"] for x in t["m_Entries"]}
    linhas = []
    for col, ents in entradas.items():
        for i, txt in ents.items():
            linhas.append((col, chaves.get(col, {}).get(i, ""), i, norm(txt or "")))
    linhas.sort(key=lambda r: (r[0], r[1]))
    return linhas


# ---------------------------------------------------------------- diálogos
class _Leitor:
    def __init__(s, b): s.b, s.p = b, 0
    def i32(s):
        v = struct.unpack_from("<i", s.b, s.p)[0]; s.p += 4; return v
    def txt(s):
        n = s.i32()
        if n < 0 or n > len(s.b) - s.p: raise ValueError
        v = s.b[s.p:s.p + n].decode("utf-8"); s.p += n; s.p = (s.p + 3) & ~3; return v
    def lista(s):
        n = s.i32()
        if n < 0 or n > 500000: raise ValueError
        return [s.txt() for _ in range(n)]


def extrair_dialogos(jogo):
    """Yarn Spinner 2: objetos Yarn.Unity.Localization em resources.assets.
    Layout: cabeçalho (28 bytes), nome, código do idioma, chaves[], valores[]."""
    arq = os.path.join(jogo, "UNBEATABLE_Data", "resources.assets")
    if not os.path.exists(arq):
        sys.exit("Não achei %s" % arq)
    env = UnityPy.load(arq)
    vistos, linhas = set(), []
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        raw = o.get_raw_data()
        if b"line:" not in raw:
            continue
        r = _Leitor(raw)
        try:
            r.p = 28
            r.txt()                     # nome do objeto
            idioma = r.txt()
            ks, vs = r.lista(), r.lista()
        except Exception:
            continue
        if len(ks) != len(vs) or not ks or not ks[0].startswith("line:"):
            continue
        if idioma and not idioma.startswith("en"):
            continue
        for k, v in zip(ks, vs):
            if k in vistos:
                continue
            vistos.add(k)
            m = re.match(r"^([^:\n<]{1,40}):\s(.*)$", v, re.S)
            fala, texto = (m.group(1), m.group(2)) if m else ("", v)
            linhas.append((k, fala, norm(texto)))
    return linhas


# ---------------------------------------------------------------- saída
def ler_csv(p):
    if not os.path.exists(p):
        return []
    with open(p, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def escrever(p, cab, linhas):
    with open(p, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f); w.writerow(cab); w.writerows(linhas)


def main():
    ap = argparse.ArgumentParser(description="Extrai os textos originais do UNBEATABLE.")
    ap.add_argument("--jogo", default=JOGO_PADRAO, help="pasta do jogo (onde fica UNBEATABLE.exe)")
    raiz = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    ap.add_argument("--traducao", default=os.path.join(raiz, "traducao"), help="pasta com os CSVs em português")
    ap.add_argument("--saida", default=os.path.join(raiz, "textos_originais"), help="pasta de saída")
    a = ap.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    os.makedirs(a.saida, exist_ok=True)

    print("Lendo a interface (pode levar alguns minutos)...")
    ui = extrair_interface(a.jogo)
    print("  %d textos" % len(ui))
    print("Lendo os diálogos...")
    dl = extrair_dialogos(a.jogo)
    print("  %d falas" % len(dl))

    escrever(os.path.join(a.saida, "interface_en.csv"), ["tabela", "chave", "id", "en"], ui)
    escrever(os.path.join(a.saida, "dialogos_en.csv"), ["id", "personagem", "en"], dl)

    # lado a lado com a tradução
    hash_de = lambda r: r.get("en_md5") or (md5(r["en"]) if r.get("en") else "")
    pt_ui = {}   # (tabela, chave) -> [linhas]; o jogo tem algumas chaves repetidas
    for r in ler_csv(os.path.join(a.traducao, "interface.csv")):
        pt_ui.setdefault((r["tabela"], r["chave"]), []).append(r)
    cont, saida = {}, []
    for tab, ch, i, en in ui:
        cand = pt_ui.get((tab, ch), [])
        r = next((c for c in cand if hash_de(c) == md5(en)), cand[0] if cand else None)
        if r:
            cand.remove(r)
            if not cand: del pt_ui[(tab, ch)]
        pt = r["pt"] if r else ""
        h = hash_de(r) if r else ""
        # (ignora diferença só de espaço/quebra de linha no fim)
        igual = not h or h in (md5(en), md5(en.rstrip()), md5(en.rstrip() + "\n"))
        sit = "falta" if (en.strip() and not pt) else ("mudou" if pt and not igual else "ok")
        cont[sit] = cont.get(sit, 0) + 1
        saida.append((tab, ch, i, en, pt, sit))
    for (tab, ch), rs in pt_ui.items():
        for r in rs:
            saida.append((tab, ch, r.get("id", ""), "", r["pt"], "sobrando"))
            cont["sobrando"] = cont.get("sobrando", 0) + 1
    escrever(os.path.join(a.saida, "interface.csv"), ["tabela", "chave", "id", "en", "pt", "situacao"], saida)
    print("interface:", cont)

    pt_dl = {r["id"]: r["pt"] for r in ler_csv(os.path.join(a.traducao, "dialogos.csv"))}
    cont, saida = {}, []
    for k, fala, en in dl:
        pt = pt_dl.pop(k, "")
        sit = "falta" if (en.strip() and not pt) else "ok"
        cont[sit] = cont.get(sit, 0) + 1
        saida.append((k, fala, en, pt, sit))
    for k, pt in pt_dl.items():
        saida.append((k, "", "", pt, "sobrando"))
        cont["sobrando"] = cont.get("sobrando", 0) + 1
    escrever(os.path.join(a.saida, "dialogos.csv"), ["id", "personagem", "en", "pt", "situacao"], saida)
    print("diálogos:", cont)
    print("Pronto. Arquivos em", a.saida)


if __name__ == "__main__":
    main()
