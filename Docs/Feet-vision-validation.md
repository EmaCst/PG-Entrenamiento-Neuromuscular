# Alfombrilla y contacto de pies: revisión Android

## Cambios

- Mantiene la cámara, el modelo YOLO, su entrada 640 × 640, el objetivo de 10 inferencias/s y una sola captura en procesamiento. No añade otro modelo, librería nativa ni dependencia de ARCore.
- Reduce la máscara a 320 × 320 conservando los mínimos de bloques de 2 × 2: un borde oscuro fino ya no se salta sistemáticamente al muestrear. Usa un umbral de exposición calculado del histograma, validación del contorno a resolución de entrada y ajuste de elipse por momentos para perspectiva.
- El detector y su memoria de trabajo se reutilizan. La búsqueda corre en un único trabajo de ThreadPool por captura y se solapa con la inferencia. Sólo lee el buffer capturado; no accede a cámaras, GameObjects o el worker desde ese hilo. El siguiente frame no reutiliza el buffer hasta terminar ambos trabajos.
- La programación de capas conserva el máximo configurado y cede cuando supera un presupuesto orientativo de 2 ms. No es un límite duro: una llamada individual puede tardar más, y no limita por sí sola el tiempo de GPU.
- NMS obtiene los dos pies con dos pasadas lineales y una lista reutilizada, sin ordenar ni crear una lista de miles de candidatos. Conserva la selección voraz original: no corta los candidatos con menor puntuación que podrían corresponder al segundo pie. El readback del tensor sigue siendo asíncrono; DownloadToArray aún asigna el array de salida. No se promete GC cero.
- Publica pies y alfombrilla juntos, con el mismo instante de captura. Rechaza contactos si el resultado tiene más de 350 ms de antigüedad.
- Tres observaciones coherentes establecen la referencia. Una pérdida puede conservarla como máximo 350 ms y sólo si una comparación dispersa del fondo aporta evidencia de cámara quieta; un fondo sin textura no permite esa conservación. Un cambio brusco requiere localizarla de nuevo. Es una referencia en imagen, no un ancla espacial.
- Mantiene las identidades izquierda/derecha por proximidad a las observaciones anteriores. Requiere ver ambos pies para establecerlas; una detección única ambigua no se adjudica por cruzar la mitad de la pantalla. No equivale a clasificación anatómica; con pies cruzados/oclusiones prolongadas hay que volver a mostrar ambos separados.
- Cada objetivo exige dos imágenes con el pie fuera del área ampliada, seguidas de al menos dos imágenes de contacto y 60 ms de permanencia. Usa el interior de la caja del pie contra la elipse, no sólo un punto. Un pie ya apoyado o una imagen repetida no generan nuevos aciertos.
- El tiempo del objetivo sólo acumula intervalos de observación utilizables; las pérdidas de referencia no generan fallos. La reacción guardada es ese tiempo observable activo. La duración general del set sigue su reloj configurado, incluso durante una pérdida de visión.
- Permanece el modo de una alfombrilla. El diámetro en centímetros es informativo: no se falsea para aumentar el alcance, ni se calcula profundidad con él.

## Verificación realizada aquí

No había Unity, compilador C# ni Android disponibles. **No se ejecutaron EditMode, una build ni un perfil de FPS del teléfono.**

Se ejecutó una traducción mecánica a C++ del núcleo matemático (sin la integración de Unity):

- seis imágenes sintéticas: disco pequeño con borde fino, poca luz, elipse inclinada, disco grande, baldosa cuadrada y superficie uniforme; todos dieron el resultado esperado;
- la foto original del usuario de la alfombrilla blanca, reducida a distintas escalas dentro de una imagen de prueba: localización correcta a escalas 1, 0.75, 0.5 y 0.375; a 0.25 se rechazó la imagen por falta de evidencia, en lugar de aceptar una baldosa. Esto **no demuestra una distancia real en metros**;
- estados de entrada/salida, imagen duplicada, permanencia, pérdida de observación, identidad al cruzar el centro de la pantalla y caducidad de referencia.

La foto de la pantalla con un círculo rojo ya dibujado no se utiliza como evidencia de detección de cámara: sus overlays alteran los píxeles originales.

`FeetVisionTests.cs` deja los casos de regresión en EditMode y referencia `Praxen.FeetVision`. Deben ejecutarse en Unity antes de considerar cerrada la validación de la build.

## Prueba pendiente en Android

1. Importar cambios, ejecutar EditMode y generar APK con el mismo dispositivo, resolución y ajustes usados antes.
2. Mostrar ambos pies y la alfombrilla descubierta. Probar iluminación normal/sombra y aumentar distancia progresivamente. Anotar dónde deja de localizarla; no atribuir una distancia a la simulación por escalado.
3. Entrar/salir con el pie solicitado, mantenerlo apoyado y cruzar el centro de la imagen. Un apoyo sostenido no debe contar repetidamente.
4. Tapar la alfombrilla y mover la cabeza: debe pedir reencuadrar cuando no pueda sostener una referencia fiable. Nunca debe sumar aciertos sobre una posición antigua indefinidamente.
5. Comparar con la APK anterior en un set de varios minutos: FPS y percentil 95 del tiempo de frame, latencia de captura (`ObservationLatencySeconds`), GC y temperatura. Revisar los marcadores `Praxen.Feet.FindMat`, `PrepareInput`, `ScheduleLayers` y `DecodeOutput`; FindMat corre en un hilo de trabajo. Comprobar también Android con fallback CPU si está disponible.

La carga queda acotada y se evita bloquear el render con la búsqueda, pero sólo el perfil del teléfono permite afirmar que no empeoraron FPS/temperatura. La resolución óptica, el desenfoque y un borde que ya no ocupa píxeles siguen limitando el alcance; una única cámara tampoco demuestra presión/contacto físico con el suelo.

Referencias de las APIs conservadas:
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/webcamtexture/getpixels32
- https://docs.unity.cn/Packages/com.unity.ai.inference%402.6/manual/split-inference-over-multiple-frames.html
- https://docs.unity.cn/Packages/com.unity.ai.inference%402.5/manual/read-output-async.html
