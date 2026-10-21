# -*- coding: utf-8 -*-
"""Generate GameFramework-style .bytes data tables (length-prefixed rows)."""
import struct
import os

ROOT = r"d:\UnityProject\GameFramework-26.8\Assets\MainGame\DataTables\Bytes"


def write_7bit(value: int) -> bytes:
    out = bytearray()
    v = value & 0xFFFFFFFF
    while v >= 0x80:
        out.append((v & 0x7F) | 0x80)
        v >>= 7
    out.append(v)
    return bytes(out)


def write_string(s: str) -> bytes:
    raw = s.encode("utf-8")
    return write_7bit(len(raw)) + raw


def write_bool(b: bool) -> bytes:
    return b"\x01" if b else b"\x00"


def write_table(path: str, rows: list):
    with open(path, "wb") as f:
        for row in rows:
            f.write(write_7bit(len(row)))
            f.write(row)
    print("Wrote", path, "rows=", len(rows))


os.makedirs(ROOT, exist_ok=True)

# UIForm: Id, Name, AssetName, UIGroupName, AllowMultiInstance, PauseCoveredUIForm
ui_rows = []
for row in [
    (101, "主界面设置", "SettingForm", "Popup", False, False),
    (109, "对话框", "DialogForm", "Popup", True, True),
]:
    rid, name, asset, group, multi, pause = row
    body = write_7bit(rid) + write_string(name) + write_string(asset) + write_string(group) + write_bool(multi) + write_bool(pause)
    ui_rows.append(body)
write_table(os.path.join(ROOT, "UIForm.bytes"), ui_rows)

# Scene: Id, Name, AssetName
scene_rows = []
for row in [
    (1, "入口场景", "GameEntryScene"),
    (2, "菜单场景", "Menu"),
]:
    rid, name, asset = row
    body = write_7bit(rid) + write_string(name) + write_string(asset)
    scene_rows.append(body)
write_table(os.path.join(ROOT, "Scene.bytes"), scene_rows)

# Entity: keep empty table (no rows) or one EmptyEntity - check Entity.txt columns
# Id, AssetName, ... need to read Entity.txt header
print("Done UIForm+Scene")
