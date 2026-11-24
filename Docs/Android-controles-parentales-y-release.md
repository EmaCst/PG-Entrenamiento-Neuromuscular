# Compatibilidad Android y controles parentales

Rama de trabajo: `fix/android-family-link-compatibility`
Base: commit de la PR #21 (`6b9bcf6`).

## Hallazgos confirmados

- `ProjectSettings/ProjectSettings.asset` usa el identificador de plantilla
  `com.UnityTechnologies.com.unity.template.urpblank`. Debe definirse un
  identificador definitivo antes de publicar; no se cambia todavía porque debe
  ser aprobado por el propietario del producto y conservarse permanentemente.
- `AndroidKeystoreName` está vacío y `androidUseCustomKeystore` está en 0.
  El repositorio no contiene una keystore de release. No se debe crear ni
  versionar una clave privada o contraseña dentro del repositorio.
- `Assets/XR/Settings/ARCoreSettings.asset` tenía
  `m_Requirement: 0`. En el enum de Unity, 0 = Required y 1 = Optional.
- Las escenas principales revisadas de Praxen (visor y teléfono) no contienen
  componentes serializados `ARSession`, `ARCameraManager` o
  `ARPlaneManager`. El código de ejercicios usa `WebCamTexture` y MediaPipe.
  Se mantiene instalado el paquete ARCore/AR Foundation por ahora; no se elimina
  una dependencia hasta verificar todas las rutas y prefabs.
- `AndroidPreferredInstallLocation` estaba en 1 (PreferExternal). Se cambió a
  0 (Auto), para dejar la decisión al sistema operativo.
- `androidAppCategory: 3` corresponde a Game. No se cambia: la categoría no
  equivale a una clasificación de edad ni a la calificación IARC de Google Play.

## Cambios aplicados en esta rama

1. `AndroidPreferredInstallLocation: 0` (Auto).
2. `ARCoreSettings.m_Requirement: 1` (Optional). Esto evita declarar que la
   compatibilidad con ARCore es requisito de instalación. No desinstala ARCore
   ni demuestra que sea la causa del aviso de Family Link.

## Pendiente antes de una distribución de release

- Aprobar un application ID definitivo (por ejemplo, un identificador bajo un
  dominio que el propietario controle). No usar el identificador de plantilla.
- Crear una keystore de release fuera del repositorio, respaldarla de forma
  segura y configurar su ruta/alias en Unity. Guardar contraseñas en un gestor
  de secretos, nunca en Git, capturas ni archivos compartidos.
- Firmar todos los APK/AAB que se distribuyan con la misma clave de release.
  Una actualización debe conservar el mismo application ID y certificado.
- Definir y fijar el Target API según los requisitos vigentes de Google Play
  cuando se vaya a publicar; no fijarlo por conjetura durante esta corrección.
- Completar el cuestionario de clasificación de contenido de Play Console si
  se distribuye por Google Play. El valor de categoría Android no reemplaza
  esa clasificación.

## Pruebas necesarias

- Abrir el proyecto con la versión de Unity configurada y revisar XR Plug-in
  Management / ARCore.
- Generar un APK de prueba y revisar su manifest final con `aapt2 dump badging`
  o herramientas equivalentes: application ID, uses-feature, permisos y SDK.
- Verificar firma con `apksigner verify --print-certs`.
- Instalar/actualizar en un dispositivo supervisado con Family Link, con cámara
  autorizada y denegada, sin desactivar la supervisión.
- Registrar el texto exacto de cualquier bloqueo y el resultado de
  `adb install -r` si falla.
- Probar los modos de visor y teléfono, porque el cambio de requisito ARCore no
  sustituye pruebas funcionales.

## Alcance y limitaciones

Estos cambios son una corrección de configuración basada en la inspección del
repositorio y la documentación de Unity. No prueban por sí mismos que el bloqueo
reportado por la familia se haya resuelto. En este entorno no se dispone de
Unity Editor, APK generado ni dispositivo Android supervisado; la validación
nativa queda pendiente.
