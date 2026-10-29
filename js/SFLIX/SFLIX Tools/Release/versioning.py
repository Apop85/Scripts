"""Versionsnummern von STEFFLIX.

Ab 2.5.0 verwendet STEFFLIX Versionen im Format x.y.z wie der Updater. Installationen bis 2.04
lesen die Version aber als Zahl (parseFloat bzw. double). Darum steht in version.js zuerst die
alte Zahl und danach der neue Text:

    var version = 2.05;          // alte Zahl, für Installationen bis 2.04
    var versionName = "2.5.0";

Umrechnung: alte Zahl = x + y/100 + z/10000 (2.5.0 -> 2.05, 2.5.1 -> 2.0501, 2.10.0 -> 2.10).
Die Reihenfolge bleibt so erhalten, solange y und z kleiner als 100 sind.
Alte Nummern (bis 2.04) zurück: zwei Nachkommastellen sind y, eine dritte ist z
(2.04 -> 2.4.0, 1.323 -> 1.32.3), vier Nachkommastellen sind y und z (2.0501 -> 2.5.1).
"""
import re

VERSION_NAME_PATTERN = r"^\d+\.\d+\.\d+$"


def version_name(text):
    """Version als x.y.z, egal ob alte Zahl ("2.04") oder schon x.y.z."""
    text = str(text).strip()
    parts = text.split(".")
    if len(parts) >= 3:
        return ".".join(str(int(part)) for part in parts[:3])
    major = int(parts[0] or 0)
    decimals = parts[1] if len(parts) > 1 else ""
    if len(decimals) <= 2:
        minor, patch = int(decimals.ljust(2, "0") or 0), 0
    elif len(decimals) == 3:
        minor, patch = int(decimals[:2]), int(decimals[2])
    else:
        minor, patch = int(decimals[:2]), int(decimals[2:4])
    return f"{major}.{minor}.{patch}"


def legacy_number(name):
    """Alte Zahl als Text für eine Version x.y.z: "2.5.0" -> "2.05", "2.5.1" -> "2.0501"."""
    major, minor, patch = (int(part) for part in version_name(name).split("."))
    if minor > 99 or patch > 99:
        raise ValueError(f"Version {name}: y und z müssen kleiner als 100 sein")
    return f"{major}.{minor:02d}" + (f"{patch:02d}" if patch else "")


def version_key(name):
    """Sortierschlüssel (x, y, z)."""
    return tuple(int(part) for part in version_name(name).split("."))


def read_version_js(text):
    """(versionName, alte Zahl) aus dem Inhalt von version.js. versionName fehlt bis 2.04."""
    legacy = re.search(r"\bversion\s*=\s*([\d.]+)", text)
    name = re.search(r"\bversionName\s*=\s*[\"']([\d.]+)[\"']", text)
    legacy_text = legacy.group(1) if legacy else None
    if name:
        return version_name(name.group(1)), legacy_text
    return (version_name(legacy_text) if legacy_text else None), legacy_text


def version_js_text(name):
    """Inhalt von version.js für eine Version x.y.z."""
    return (
        "// Alte Zahl zuerst: STEFFLIX und Updater bis 2.04 lesen nur diese Zeile\n"
        f"var version = {legacy_number(name)};\n"
        f"var versionName = \"{version_name(name)}\";\n"
    )


if __name__ == "__main__":
    # Selbsttest
    assert version_name("2.04") == "2.4.0"
    assert version_name("2.00") == "2.0.0"
    assert version_name("1.323") == "1.32.3"
    assert version_name("1.101") == "1.10.1"
    assert version_name("1.09") == "1.9.0"
    assert version_name("2.0501") == "2.5.1"
    assert version_name("2.5.0") == "2.5.0"
    assert legacy_number("2.5.0") == "2.05"
    assert legacy_number("2.5.1") == "2.0501"
    assert legacy_number("2.10.0") == "2.10"
    assert legacy_number("2.4.0") == "2.04"
    assert float(legacy_number("2.9.99")) < float(legacy_number("2.10.0"))
    for name in ["2.5.0", "2.5.1", "2.10.0", "2.10.3", "3.0.0"]:
        assert version_name(legacy_number(name)) == name, name
    print("OK")
