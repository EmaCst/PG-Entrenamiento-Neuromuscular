# Rama de integracion de los tres ejercicios

La rama `feature/integracion-tres-ejercicios` contiene:

1. Ejercicio de manos con MediaPipe Hands.
2. Ejercicio de pies con YOLO11 y Sentis.
3. Ejercicio de carrera y cambio de direccion.

## Configuracion del ejercicio de pies

1. Abra el proyecto y espere a que Unity instale `com.unity.sentis` 2.1.2.
2. Espere a que Unity importe `Assets/Models/foot_detector_best.onnx`.
3. Abra una escena con una camara etiquetada como `MainCamera`.
4. Ejecute `PG RA > Crear ejercicio de pies en escena`.
5. En `SessionManager`, seleccione `Exercise Type = Feet`.
6. Asigne `Current Foot Exercise` y `Foot Stats`.
7. Conecte un `RawImage` a `FootCameraSource` si desea mostrar la camara como fondo.
8. Oriente la camara hacia los tres objetivos y ajuste sus posiciones sobre el suelo.

## Flujo del detector

`FootCameraSource` obtiene la imagen de la camara posterior. `FootDetectorSentis` corrige la rotacion reportada por Android, estira la imagen a 640 x 640 y crea un tensor NCHW normalizado entre 0 y 1. El modelo produce 8,400 candidatos con cinco valores: centro X, centro Y, ancho, alto y confianza de la clase `pie`.

El detector descarta resultados con confianza menor a 0.40, aplica supresion no maxima con IoU de 0.50 y conserva como maximo dos pies. Las detecciones se ordenan horizontalmente para obtener una aproximacion del pie izquierdo y derecho.

`FootExerciseManager` conserva la ultima deteccion durante 0.20 segundos. Este intervalo cubre las perdidas breves observadas en el video de validacion sin mantener un pie inexistente durante demasiado tiempo.

## Interaccion

Los tres objetivos son objetos tridimensionales. Cada objetivo proyecta sus limites hacia el viewport de la camara. El contacto se produce cuando el punto inferior central de la caja del pie requerido entra en el area proyectada del objetivo activo.

El pie izquierdo se representa con azul y el derecho con rojo. La vida inicial del objetivo es 2.5 segundos. Cinco aciertos consecutivos reducen el tiempo 0.25 segundos y tres fallos consecutivos lo aumentan nuevamente, con un minimo de 0.75 segundos.

## Datos registrados

Por cada objetivo se guarda el indice, pie requerido, acierto o fallo, tiempo de reaccion y confianza del detector. Al terminar se calculan aciertos, fallos, promedio y mejor tiempo de reaccion. Los resultados se incorporan al JSON general de la sesion.

## Prueba recomendada

Primero pruebe el ejercicio dentro del Editor con la camara del equipo. Despues genere una compilacion Android y compruebe la orientacion de la imagen. Si las cajas aparecen invertidas con respecto al usuario, ajuste `Flip Horizontal` o `Flip Vertical` en `FootDetectorSentis`.

## Pruebas realizadas: ejercicio de manos en el telefono

### Observaciones iniciales

Durante la prueba manual del ejercicio en el telefono, la camara estaba disponible y MediaPipe detectaba las manos, pero la representacion y la interaccion no coincidían correctamente con el movimiento observado:

- Solo se mostraba un punto por mano, sin el esqueleto completo de landmarks. Esto dificultaba comprobar visualmente que MediaPipe estuviera siguiendo la mano.
- La mano izquierda real aparecia identificada como `Right` y la derecha como `Left`.
- Al levantar la mano, el marcador correspondiente se desplazaba hacia abajo; el eje vertical estaba invertido.

Estas observaciones describen errores de orientacion y visualizacion de la salida del seguimiento; por si solas no indicaban que el modelo de MediaPipe hubiera dejado de detectar las manos.

### Correccion aplicada

Se mantuvo el detector de MediaPipe y se corrigio el tratamiento de su resultado:

1. `HandLandmarkerRunner` transmite al puente las transformaciones horizontal y vertical aplicadas a la imagen de entrada.
2. `HandTrackingBridge` conserva la etiqueta de lateralidad y las 21 landmarks de cada mano, ademas de los indicadores de espejo de la fuente.
3. `MediaPipeHandInteraction` ajusta las coordenadas para la imagen mostrada, corrige la inversion vertical observada y asigna la mano segun la transformacion horizontal.
4. Se dibujo el esqueleto de la mano sobre la imagen de la camara y se añadieron indicadores de diagnostico para mostrar cuantas manos se detectan y si el indice alcanza un objetivo.
5. La proyeccion de interaccion se adapta a la camara del ojo izquierdo del rig estereoscopico y busca el objetivo interactuable alcanzado mas cercano.

### Verificacion y resultado reportado

Se volvio a probar el ejercicio despues de los cambios. El usuario confirmo que el ejercicio de manos ya funcionaba correctamente y dejo el pulido visual para una iteracion posterior. La comprobacion fue cualitativa; en esta prueba no se registraron metricas numericas ni un conteo de repeticiones.
