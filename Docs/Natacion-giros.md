# Natación: piques y giros con Pose (issue #7)

TelefonoCorrer conserva su nombre interno y GUID para no romper el menú/build; su
contenido ahora es cámara de selfies + NatacionTurnsExercise + RunningExerciseStats.
No carga AR, suelo, estereoscopía ni giroscopio. Una persona por teléfono fijo.

## Flujo

1. Encuadrar cabeza, hombros, cadera y tobillos; permanecer de frente un segundo.
2. Cuenta atrás de 5 s gestionada por el plan de sets/circuito existente.
3. Hacer piques en el sitio. Tras 0.5 s de posición frontal, esperar aleatoriamente
   3, 4 o 5 segundos (misma probabilidad); mostrar izquierda azul/derecha roja (50/50).
4. Girar hombros Y cadera hacia la indicación. Volver de frente para otra indicación.
   Si se abandona la posición frontal durante la espera, esta vuelve a empezar.
5. Descansos, sets y captura de resultados utilizan los gestores existentes.

La espera no depende de la dificultad. El tiempo de respuesta empieza en la señal:
3 s en Inicial, baja 0.25 s por nivel hasta 2 s, sin superar la dificultad máxima
configurada. Cinco aciertos consecutivos suben un nivel, tres fallos lo bajan.
No se reproduce el audio antiguo que pedía correr. Las nuevas voces se integrarán
cuando estén disponibles; la cuenta atrás y los avisos generales permanecen.

## Medición y seguimiento

Se reutilizan los 33 world landmarks de la misma inferencia Pose de manos/pies;
no se abre otro detector ni cámara. Los buffers están preasignados, se copian bajo
el mismo lock y se evalúa una sola vez cada captura. Se mantiene el límite de
15 inferencias/s y una inferencia en vuelo. Falta medir FPS reales en Android.

La orientación se estima con atan2(z,x) del eje derecha-izquierda de hombros y
cadera, relativa al encuadre frontal. No se invierte el espacio de detección al
mostrar el vídeo como espejo. El umbral inicial es 40 grados estimados en ambos
ejes, concordancia de 20 grados y confirmación durante al menos 0.15 s / 3 muestras.
No representa una medición clínica del ángulo ni verifica por sí solo los piques.
Cabeza/brazos no se usan para calcular el giro. El umbral y la respuesta necesitan
validación práctica con la fisioterapeuta. No se exige un giro completo de 90°.

La pérdida de confianza/encuadre, una captura con más de 0.35 s o la pausa del
set interrumpen el intento exactamente una vez, sin sumar fallo. La pérdida de
seguimiento pausa también el reloj del set. Un giro contrario o incompleto con
seguimiento válido termina en fallo al agotarse la respuesta. No se puntúan
capturas anteriores a la señal ni duplicadas. Para mover el teléfono, Recalibrar.

## Resultados

Se conserva running_direction_change / runningExercises para compatibilidad.
measurementMode = pose_body_turn_response distingue esta versión del giroscopio.
El tiempo registrado incluye ejecución y confirmación del giro; movementOnsetSeconds
es la primera estimación >10° en ambos ejes, o -1 si no hubo inicio detectable.
Interrupciones, fallos y aciertos son contadores separados. No se cambió la API.
Los informes históricos que agreguen solo por exerciseType todavía pueden mezclar
versiones; deberán filtrar measurementMode antes de comparar sus métricas.

## Verificación

Hay pruebas EditMode de dirección, giro parcial, confirmación, timestamps,
interrupción única y espera 3–5 s tras volver de frente. No se ejecutaron aquí:
el entorno de edición no dispone de Unity ni compilador C#.

En Unity: Test Runner > EditMode > NatacionTurnAttemptTests. En Android:

- Probar TelefonoCorrer y después el circuito y dos sets individuales.
- Confirmar que izquierda anatómica es azul y derecha roja en la imagen espejo.
- Cabeza/brazos solos no deben contar; hombros y cadera juntos sí.
- Cronometrar la espera DESPUÉS de volver de frente; nunca debe ser 1 s.
- Tapar cámara/salir del encuadre: reloj pausado, una interrupción, cero fallos.
- Volver de frente: nueva espera completa, sin indicación inmediata.
- Verificar cámara visible en descanso, estadísticas acumuladas y envío pendiente.
- Repetir manos/pies para comprobar que la copia de world landmarks no los altera.

3–5 s es una decisión inicial de ritmo, no una garantía de ausencia de mareo.
Si aparece, detener la actividad y descansar; validar ritmo y amplitud con la fisio.
