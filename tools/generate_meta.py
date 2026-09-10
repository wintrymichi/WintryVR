#!/usr/bin/env python3
"""Generates deterministic Unity .meta files for every asset under Assets/ that lacks one.

GUIDs are derived from the asset path (MD5) so scene references to scripts stay stable across
fresh checkouts. Run from the repository root:  python3 tools/generate_meta.py
"""
import hashlib, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
ASSETS = os.path.join(ROOT, "Assets")

def guid_for(rel_path: str) -> str:
    return hashlib.md5(("wintryvr:" + rel_path.replace("\\", "/")).encode("utf-8")).hexdigest()

def importer_block(path: str) -> str:
    ext = os.path.splitext(path)[1].lower()
    if os.path.isdir(path):
        return "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if ext == ".cs":
        return ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n"
                "  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    if ext in (".ttf", ".otf"):
        # A font written with the DefaultImporter is not imported as a Font at all, so Resources.Load<Font>
        # comes back null and every label silently falls back to the built-in face.
        return "TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n  fontNames: []\n  fallbackFontReferences: []\n  customCharacters: \n  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n  shouldRoundAdvanceValue: 1\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if ext == ".shader":
        return "ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if ext == ".asmdef":
        return "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if ext in (".json", ".txt", ".xml", ".md"):
        return "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    return "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

def main():
    created = 0
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        entries = [os.path.join(dirpath, d) for d in dirnames] + [os.path.join(dirpath, f) for f in filenames if not f.endswith(".meta")]
        for path in entries:
            meta = path + ".meta"
            if os.path.exists(meta):
                continue
            rel = os.path.relpath(path, ROOT)
            with open(meta, "w", newline="\n") as fh:
                fh.write("fileFormatVersion: 2\nguid: %s\n%s" % (guid_for(rel), importer_block(path)))
            created += 1
    print("created %d .meta file(s)" % created)
    if len(sys.argv) > 1:
        for p in sys.argv[1:]:
            print(p, guid_for(p))

if __name__ == "__main__":
    main()
