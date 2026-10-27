# Natación: cámara de selfies y manos (primer incremento)

Trabajo exclusivo de la rama natacion. Relacionado con #4 y #5; requieren validación en Android antes de cerrarse.

## Incluido

- TelefonoManos conserva su GUID y entrada en Build Settings, pero usa una única vista de la cámara frontal.
- Teléfono fijo, una persona por dispositivo. No se usa el visor ni se inicia XR automáticamente.
- Una fuente Pose compartible, modelo Full existente, una inferencia pendiente como máximo, 15 solicitudes/s y entrada de 640 píxeles en su lado mayor. Sin máscaras de segmentación.
- Presentación espejada; entrada de inferencia sin espejo. Landmarks y contactos usan el mismo encuadre y relación de aspecto que el vídeo.
- Calibración de cuerpo completo y tablero 3x3 fijo tras calibrar. Solo se dibuja el objetivo activo; puntos pequeños en las muñecas ayudan a comprobar alineación.
- Contacto de la palma aproximado por segmentos de Pose desde muñeca a dedos; no se utiliza todo el antebrazo. Dos observaciones consecutivas para confirmar.
- Preparación y descanso en una sola vista sobre la cámara. El set espera la calibración y su cronómetro se detiene si se pierde el seguimiento.
- Cambio grande de posición/pérdida prolongada pide recalibrar. Es una comprobación geométrica, no identificación de personas.
- Sets, descansos, dificultad máxima y formato de resultados existente se conservan. No se modificó la API.

## Cómo probar

1. Actualizar y abrir la rama natacion; esperar importación y revisar Console.
2. Compilar Android con MenuTelefono como entrada. Mantener TelefonoManos y CuentaRegresiva en las escenas de build. No regenerar TelefonoManos con el constructor antiguo de escenas VR.
3. Seleccionar Manos individual: 2 sets de 30 s, descanso de 10 s, dificultad máxima Básico.
4. Apoyar el teléfono en vertical con la cámara de selfies hacia la persona. Encuadrar cabeza, manos y pies. Mantenerse de frente para calibrar.
5. Verificar imagen derecha y alineación de puntos de muñeca al mover cada mano. Azul = izquierda anatómica; rojo = derecha.
6. Tocar con la mano indicada y comprobar que la otra no puntúa; no deben aparecer el tablero ni esferas inactivas.
7. Salir del encuadre: no deben contarse fallos ni consumirse segundos. Volver y recalibrar cuando se solicite.
8. Comprobar descanso con vídeo, cuenta centrada, segundo set y resultados acumulados.
9. Repetir entrada/salida del ejercicio, denegar/conceder permiso y suspender/reanudar la aplicación.
10. Probar con otra persona al fondo y varios teléfonos: no se garantiza identidad si otra persona ocupa el mismo encuadre. Evitar solapamientos y registrar intercambios de seguimiento.

## Validación y límites

Incluidos tests EditMode de contacto segmento/círculo (centro del segmento, extensión y segmento degenerado). No se ejecutaron: este entorno no dispone de Unity ni compilador C#.

Pendiente compilar en Unity y medir en Android: orientación frontal, precisión/latencia de contacto, estabilidad, FPS y calentamiento durante una sesión completa. El límite de inferencia no garantiza un número de FPS.

Pies con Pose se incorporó posteriormente: ver Natacion-pies.md. Giros por Pose, ajustes generales de menús/audio y validación global siguen en #7–#9. Correr conserva la implementación anterior. El formato de resultados aún no distingue estadísticamente la variante de cámara fija frente a VR; revisar esa diferenciación en #8 antes de usar datos de esta rama en comparaciones de entrenamiento.

## Segundo issue del plan: manos (#5), revisión de contacto

Se separó el estado de cada intento para impedir doble conteo y rechazar observaciones repetidas, anteriores al estímulo o fuera de orden. El acierto requiere dos observaciones nuevas de contacto con la mano solicitada. Al perder fiabilidad se suspende el intento y se descarta el primer contacto de recuperación.

El controlador exige al menos dos segmentos de palma fiables; rechaza segmentos que superan el 45% del ancho calibrado de hombros. La tolerancia de contacto se mantiene en 6% de dicho ancho, adicional al radio del objetivo. Son parámetros iniciales que deben ajustarse en Android, no medidas físicas validadas.

Antes del siguiente objetivo exige ambas manos fiables. Durante el objetivo exige la mano indicada. En ambos casos el cronómetro del set y el registro por dificultad se pausan si falta esa evidencia. No se eligen posiciones que ya estén ocupadas por la mano solicitada. La calibración ahora comprueba estabilidad respecto a un punto fijo, evitando calibrar mientras la persona deriva lentamente.

Se añadieron 10 tests EditMode del estado del intento: mano contraria, mano derecha, acierto único, frames repetidos, datos no fiables, interrupción, vencimiento único, contacto tardío, suspensión y observaciones fuera de orden. No ejecutados en este entorno por falta de Unity/compilador C#. Siguen pendientes la compilación y las pruebas de alcance, precisión y FPS en Android; #5 no se considera validado hasta realizarlas.

