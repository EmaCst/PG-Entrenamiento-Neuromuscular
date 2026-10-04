# Paquete nativo de MediaPipe

Antes de abrir Unity por primera vez, desde la raíz del proyecto ejecuta:

```powershell
py Tools/prepare_dependencies.py
```

En macOS/Linux utiliza `python3` en lugar de `py`. Se requiere Python 3.8 o posterior.
El script reutiliza el paquete oficial 0.16.3 de la carpeta Downloads si existe;
en otro caso descarga la misma versión desde la publicación oficial de Homuler.
Comprueba el SHA-256 publicado antes de colocar el archivo en esta carpeta.
El paquete contiene las bibliotecas nativas necesarias; una dependencia Git del
código fuente de MediaPipe no sustituye este paquete compilado.

También puedes indicar una ubicación existente:

```powershell
py Tools/prepare_dependencies.py --source "C:\ruta\com.github.homuler.mediapipe-0.16.3.tgz"
```

Unity resuelve `file:../ThirdParty/com.github.homuler.mediapipe-0.16.3.tgz`
desde `Packages/manifest.json`. El archivo binario de aproximadamente 290 MB
no se incluye en Git. Al copiar el proyecto a otra computadora ejecuta el script
antes de abrirlo. Las siguientes aperturas pueden funcionar sin descargarlo otra vez.
