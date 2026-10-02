# Evidencias visuales del desarrollo

Esta carpeta reune capturas seleccionadas para acompañar la documentación del
proyecto. Se conservaron los archivos originales de imagen y se les asignaron
nombres que indican su contenido. Las capturas muestran etapas distintas del
desarrollo; por eso se especifica cuándo corresponden a un prototipo anterior o
a una prueba preliminar.

| Archivo | Qué muestra | Cómo debe interpretarse |
| --- | --- | --- |
| `01_prototipo_prueba_manos.png` | Prototipo de manos con puntos de referencia, etiqueta `Left` y contador de puntos. | Evidencia de la prueba inicial de visión e interacción; no es una captura de la versión Android final. |
| `02_landmarks_mano_mediapipe_unity.png` | Escena de demostración de MediaPipe Hands con landmarks dibujados sobre la mano. | Validación visual del seguimiento de landmarks en Unity. Es una escena de muestra del paquete, no la interfaz final del ejercicio. |
| `03_manos_escena_estereoscopica.png` | Escena del ejercicio de manos en Unity, dividida en dos vistas, con las dos manos y estado de detección. | Evidencia de integración en la escena estereoscópica; el mensaje informa que se detectan dos manos. |
| `04_prueba_ejercicio_pies.png` | Ejercicio de pies con imagen de cámara, objetivos y contadores. | Registro de una prueba en la que hubo aciertos y fallos; no representa una prueba final en Android. |
| `05_prototipo_landmarks_pose.png` | Escena de ejemplo de MediaPipe Pose con landmarks del cuerpo. | Prototipo exploratorio de pose. No corresponde al detector YOLO11/Sentis usado por la implementación actual de pies ni demuestra la calibración de carrera. |
| `06_android_lista_escenas.png` | Build Profiles con Android seleccionado y las cinco escenas incluidas, con `MenuTelefono` primero. | Estado de configuración previo al cambio de plataforma: Windows aún aparece como `Active`. |
| `07_android_opciones_compilacion.png` | Opciones de Android, `Build App Bundle` desmarcado y `Development Build` desactivado. | Captura complementaria del perfil de compilación Android. |
| `08_android_cambio_plataforma.png` | Unity muestra el progreso de importación después de iniciar el cambio de plataforma. | Proceso en curso; hay que esperar a que termine antes de compilar. No es una captura de un APK generado. |
| `09_advertencia_input_android.png` | Unity advierte que `Active Input Handling` está configurado como `Both` al compilar para Android. | Captura anterior a la corrección: el proyecto ahora se configura con el sistema nuevo y la calibración de carrera usa ese sistema. |
| `10_unity_no_detecta_dispositivo.png` | Unity muestra `No Android devices connected` al intentar ejecutar la compilación en el teléfono. | El teléfono puede estar conectado físicamente, pero aún no autorizado o visible mediante ADB. |
| `11_gradle_construyendo_android.png` | Unity muestra `Building Gradle project` y la tarea `extractDeepLinksDebug`. | La compilación todavía está en curso; esta captura no confirma que el APK se haya terminado de generar. |
| `12_permiso_camara_android_concedido.jpg` | Ajustes de Android muestran que NeuromuscularAR tiene concedido el permiso de cámara. | Confirma que el usuario sí concedió el permiso del sistema. |
| `13_error_acceso_camara_mediapipe.jpg` | La escena informa cero manos y la consola muestra `InvalidOperationException: Not permitted to access cameras` desde `WebCamSource.Play`. | Evidencia del fallo observado pese al permiso concedido; se corrigió la espera/comprobación del permiso y falta validar el APK reconstruido en el teléfono. |

## Evidencia que falta capturar

- Pantalla `MenuTelefono` con selección de ejercicio y configuración de la
  sesión combinada.
- Ejercicio de manos ya integrado mostrando landmarks y la corrección de
  lateralidad/eje en la compilación del teléfono.
- Ejercicio de pies de la versión actual en Android, con una detección correcta
  y sus contadores visibles.
- Calibración de las cuatro esquinas del área de carrera y una llegada correcta
  al objetivo después de calibrar.
- Resumen final de una sesión combinada.
- Build Profile después de que Android quede marcado como plataforma `Active`,
  y el APK instalado ejecutándose en el teléfono.

Las pantallas vacías del ejercicio de carrera, las ventanas de error de Unity y
las pantallas de descarga/entrenamiento del dataset no se incluyeron como
evidencia de funcionamiento. Se pueden conservar aparte si luego se documenta
el diagnóstico de esos problemas.
