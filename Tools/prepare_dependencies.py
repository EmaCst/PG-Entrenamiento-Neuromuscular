#!/usr/bin/env python3
"""Prepare the official MediaPipe binary package before opening Unity."""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import sys
import urllib.request

VERSION = "0.16.3"
PACKAGE = f"com.github.homuler.mediapipe-{VERSION}.tgz"
URL = f"https://github.com/homuler/MediaPipeUnityPlugin/releases/download/v{VERSION}/{PACKAGE}"
SHA256 = "cc3e77a219e0b99618ae3be64c31a566197deedc69c1e136acf52d65d7cf2e79"
ROOT = Path(__file__).resolve().parents[1]


def checksum(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def prepare(source=None):
    destination = ROOT / "ThirdParty" / PACKAGE
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and checksum(destination) == SHA256:
        print(f"MediaPipe {VERSION} listo: {destination}")
        return destination

    if source is None:
        candidate = Path.home() / "Downloads" / PACKAGE
        if candidate.is_file():
            source = candidate

    temporary = destination.with_suffix(".tgz.part")
    try:
        if source is not None:
            source = Path(source).expanduser().resolve()
            if not source.is_file():
                raise ValueError(f"No existe el paquete: {source}")
            print(f"Reutilizando {source}")
            shutil.copyfile(source, temporary)
        else:
            print(f"Descargando MediaPipe {VERSION} (290 MB)...", flush=True)
            request = urllib.request.Request(URL, headers={"User-Agent": "NeuromuscularAR-Setup"})
            with urllib.request.urlopen(request, timeout=60) as response, temporary.open("wb") as output:
                shutil.copyfileobj(response, output)

        if checksum(temporary) != SHA256:
            raise ValueError("El SHA-256 no coincide con el paquete oficial. No se instalará este archivo.")
        os.replace(temporary, destination)
    finally:
        temporary.unlink(missing_ok=True)

    print(f"MediaPipe {VERSION} listo. Ya puedes abrir el proyecto en Unity.")
    return destination


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, help="Ruta del .tgz oficial descargado previamente")
    arguments = parser.parse_args()
    try:
        prepare(arguments.source)
    except (OSError, ValueError) as error:
        print(f"No se pudo preparar MediaPipe: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
