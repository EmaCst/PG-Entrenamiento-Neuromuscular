# Praxen: gafas y cámara frontal

Rama de integración: feature/praxen-modo-dual. No se modifican natacion ni la rama
original de gafas. El menú sigue siendo vertical para configurar en el teléfono.

## Selector

En Configura tu ejercicio y Configura tu circuito aparece el switch Con gafas /
Sin gafas. Se guarda en PlayerPrefs y se aplica al iniciar. No se cambia de cámara
ni de detector durante un set. Sets, duración, descanso y dificultad máxima siguen
compartidos; 0 s sigue desactivando etapas del circuito.

| Ejercicio | Sin gafas | Con gafas |
| --- | --- | --- |
| Manos | TelefonoManos | GafasManos |
| Pies | TelefonoPies | GafasPies |
| Cambios de dirección | TelefonoCorrer | GafasCorrer |

Las escenas Gafas son copias de las escenas originales del commit
0f8a988962c9e4e99cc9582aa4b76149d14fcbdb (última versión anterior a Natación), con
GUIDs nuevos, conservando sus objetos, referencias y scripts. No se sustituyeron
los controladores ni las escenas Pose. Las seis escenas están en Build Settings.
Las escenas de gafas no necesitan el antiguo arranque automático de ARCore:
manos usa MediaPipe Hands, pies YOLO/alfombrilla y dirección cámara/giroscopio.

Al cargar una escena para gafas se configura horizontal y cámara trasera; las
escenas Pose y el menú vuelven a vertical. WebCamSource selecciona explícitamente
la cámara trasera al volver de Pose, evitando reutilizar el dispositivo frontal
que se conserva en el proveedor. En gafas siguen la presentación estereoscópica,
la preparación/descanso duplicados y el aviso de quitarse los lentes al terminar.

## Planes, temporizadores y resultados

PraxenSceneRoutes mantiene IDs canónicos Telefono* para planes y captura. Solo el
loader decide la escena física. El SessionManager viejo también normaliza esos
nombres para permanecer desactivado en Gafas*: no arranca su reloj serializado de
60 s encima del ExerciseSetRunner/CombinedExerciseSession.

Se conserva el mismo login y cola de nube. Las métricas nuevas de gafas llevan
mediapipe_hand_screen_contact_v1, reference_mat_foot_contact_v1 o
gyroscope_turn_response. Pose conserva sus etiquetas. El historial filtra el modo
seleccionado y no combina esos métodos. Los resultados antiguos sin etiqueta no
se reclasifican automáticamente, porque algunos pueden proceder de las primeras
pruebas de Pose. No se modificó la API ni la base de datos.

## Objetivo de rendimiento

PraxenPerformance fija Application.targetFrameRate=30 y vSyncCount=0, también al
cambiar de escena. En móvil selecciona el perfil Mobile, sin sombras en tiempo
real, MSAA ni reflejos dinámicos. El URP móvil conserva renderScale=0.8 y desactiva
HDR y sombras principales. No reduce la resolución de los modelos ni la precisión
del detector de alfombrillas para aparentar un aumento de FPS.

Pose solicita hasta 20 Hz y mantiene una sola inferencia en vuelo; la imagen se dibuja a ritmo del
render. Pies conserva 10 Hz, lectura asíncrona y planificación por lotes con un
presupuesto aproximado de 2 ms de programación por frame. Solo se ejecuta el
detector perteneciente a la escena activa. No se cargan ambos modos a la vez.

30 FPS es un objetivo configurado, NO un mínimo garantizado. Cámara, CPU/GPU,
calentamiento y el modelo pueden impedirlo. Falta medir el tiempo de frame real
en Android/Profiler (33.3 ms por frame), preferiblemente con Development Build
apagado para la medición de rendimiento y un perfil de diagnóstico aparte.

## Verificación pendiente en dispositivo

Se añadieron pruebas EditMode de enrutamiento de los tres ejercicios, normalización,
conservación del circuito y exclusión de pantallas compartidas. No hay Unity ni
compilador C# en este entorno: no se ejecutaron EditMode, compilación o pruebas de
cámara/render. Revisión estática de escenas, rutas, GUIDs y ramas compartidas.

- Build & Run desde MenuTelefono. Cada uno de los tres individuales en ambos modos.
- Circuito de dos vueltas en cada modo, con los tres activos y con uno a 0 s.
- Cambiar Sin gafas → Con gafas → Sin gafas sin cerrar la app: cámara correcta,
  orientación correcta, sin cámara duplicada ni temporizador antiguo.
- Descanso con vídeo y UI por ojo en gafas; UI única en frontal. Salida al menú.
- Login, estadísticas acumuladas, sincronización y separación del historial.
- Medir FPS reales, especialmente pies/YOLO en gafas y Pose en cámara frontal.


## Seguimiento frontal tolerante a interrupciones

Los tres ejercicios conservan la calibración hasta pulsar Recalibrar o abandonar
la escena. El tiempo del set sigue corriendo si falta una observación; los contactos
y giros solamente se confirman con observaciones nuevas y fiables. El contador
previo requiere calibración inicial, pero no se reinicia por pérdidas posteriores.

Manos calibra con hombros, cadera y muñecas, sin exigir cabeza ni pies. Durante
la interacción basta la muñeca solicitada; los segmentos de palma disponibles
siguen contando, con su límite de tamaño, y dos contactos fiables consecutivos confirman el acierto. El objetivo permanece visible aunque falte la mano. Pies necesita
el encuadre corporal al calibrar; una vez calibrado comprueba el pie solicitado,
sin bloquearlo por hombros/cadera u ocultación del otro pie. Antes de activar otro
objetivo todavía exige que ambos pies se hayan retirado de las zonas. Los círculos
y flechas no se ocultan por un solo fotograma inválido. Las pérdidas de hasta
medio segundo no borran el progreso de la calibración inicial, pero no aportan
tiempo de estabilidad ni contactos. No hay nuevas inferencias ni asignaciones por
fotograma en estos cambios.

Validar en Android: tapar brevemente una mano y luego ambos pies en manos;
el objetivo debe seguir visible y el reloj avanzar sin aciertos mientras no haya
datos. Volver al encuadre tras más de tres segundos debe recuperar el contacto
sin recalibración automática. En pies, probar apoyo del pie solicitado mientras
se ocultan los hombros; no aceptar pies inventados ni objetivos ya ocupados.
En giros, perder seguimiento no debe contabilizar un giro ni borrar la referencia.
Comprobar también Recalibrar, descanso y siguiente set. Esta revisión es estática;
no se ha ejecutado Unity ni medido precisión o FPS en un teléfono.


## Respuesta de manos y latencia de Pose

Manos exige dos contactos fiables consecutivos en observaciones nuevas, sin una
espera mínima adicional de 120 ms. Una pérdida o recuperación inicia otra pareja. Se siguen rechazando datos
anteriores a la activación, repetidos, fuera de orden o no fiables; cada objetivo
solo puede puntuar una vez. El margen añadido a muñeca/palma pasa de 0.06 a 0.16
anchos de hombros (misma escala de imagen), manteniendo el tamaño visual de la
esfera. Este margen también se usa para evitar generar objetivos sobre la mano.

Las tres escenas frontales solicitan 20 inferencias/s en lugar de 15. Se conserva
el modelo Full, la resolución y una sola petición en curso, sin cola de imágenes.
La fuente publica resultados en Update con orden -100, antes de los controladores,
y reutiliza la actualización de vídeo en vez de realizar un segundo Blit desde la
corrutina de inferencia. No se añade predicción de posiciones ni contactos sobre
puntos interpolados. 20 Hz es un límite solicitado, no una frecuencia medida ni
una garantía de mayor precisión. Verificar latencia, FPS y calentamiento en Android.

Pruebas EditMode de NatacionHandAttempt actualizadas para dos contactos consecutivos,
mano incorrecta, recuperación, fotogramas repetidos/desordenados y vencimiento.
No ejecutadas en este entorno sin Unity/compilador C#.
