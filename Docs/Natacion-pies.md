# Natación: pies con Pose — issue #6 (tercero del plan)

## Implementación

TelefonoPies usa NatacionPoseSource y NatacionFeetExercise en vista única. La escena no contiene FootDetectorSentis, FootCameraSource, FootExerciseManager, alfombrillas, AR ni rig estereoscópico. Solo se ejecuta una inferencia Pose, con la configuración compartida de manos (15 solicitudes/s como máximo).

Se calibra de frente, con cabeza, hombros, caderas y ambos pies visibles, separados y quietos durante un segundo. Los objetivos se fijan a los lados de la posición inicial de los pies. Distancia lateral: 0,65 veces el ancho de hombros; radio: 0,22 veces ese ancho, corrigiendo la relación de aspecto. Son proporciones visuales, no centímetros ni coordenadas físicas del suelo. Si los objetivos no caben, se solicita ajustar el encuadre; no se recortan ni se mueven con el pie durante el intento.

Dos zonas: azul = pie izquierdo; roja = derecho. Se utiliza el segmento talón–punta del pie solicitado; también se exige fiabilidad del tobillo y del cuerpo. No se estima presión, apoyo real en el suelo ni cadencia de piques. Se indica mantener los piques entre estímulos, según las instrucciones de entrenamiento.

Ambos pies deben salir de las zonas antes del siguiente estímulo. Durante cada intento se requieren dos observaciones nuevas fuera de la zona y luego dos dentro. Existe un margen de salida mayor que el de entrada para evitar oscilaciones por ruido. Un intento produce como máximo un acierto o fallo. Los datos repetidos, antiguos o no fiables no puntúan; perder seguimiento pausa intento y set. Una pérdida prolongada o cambio importante de posición exige recalibrar.

## Tiempo y dificultad

Valores iniciales de prueba, editables en NatacionFeetExercise:
- Inicial: 1,80 s (antes 2,50 s).
- Básico: 1,55 s.
- Intermedio: 1,30 s.
- Avanzado: 1,05 s.
- Intensivo: 0,80 s.
- Espera mínima entre objetivos: 0,30 s, además de retirar los pies de las zonas.

Cinco aciertos consecutivos suben un nivel y tres fallos lo reducen. No se supera la dificultad máxima elegida. No son valores validados todavía con la fisioterapeuta.

## Integración

INatacionExercise comparte disponibilidad/seguimiento con los gestores de sets y circuitos. Se conserva la cuenta previa, cámara durante descanso, cantidades de sets, resultados acumulados y el contrato FootExerciseStats existente. No se cambia la API. El circuito puede alternar manos y pies; Correr todavía conserva su implementación anterior.

La diferenciación de datos de Pose frente a los históricos de alfombrillas sigue pendiente en #8. No comparar directamente ambas variantes como si fueran la misma medición. El audio existente "feet" se conserva como presentación del ejercicio; falta revisar/grabar instrucciones específicas para los piques con teléfono fijo en #8.

## Validación

Se añadieron 10 tests EditMode de NatacionFootAttempt: salida/entrada, permanencia dentro, frames repetidos, fiabilidad, recuperación, huecos de observación, vencimiento único, contacto tardío y reinicio de intento.

Revisión estática realizada de referencias de escena, GUID, interfaz compartida y ausencia de dependencias de alfombrillas/YOLO en la escena. No se dispone de Unity ni compilador C# aquí: los tests y la build Android quedan pendientes.

Prueba práctica:
1. Individual → Pies, 2 sets de 30 s y descanso de 10 s; máximo Básico.
2. Cámara de selfies, teléfono fijo, cuerpo completo y espacio lateral visible.
3. Calibrar y comprobar que las zonas quedan fuera de la posición inicial de los pies.
4. Hacer piques, alcanzar el estímulo con el pie indicado y volver al centro. Probar el pie contrario: no debe puntuar.
5. Dejar el pie en una zona: no debe generar múltiples aciertos ni otro estímulo hasta retirarlo.
6. Ocultar un pie o salir del encuadre: no sumar fallos ni gastar tiempo del set. Recuperar/recalibrar.
7. Verificar segundo set, pausa con cámara, resultados y circuito manos + pies con Correr desactivado.
8. Medir FPS/latencia y calentamiento en Android; ajustar tiempos y alcance con pruebas reales. El límite de inferencia no garantiza FPS.
