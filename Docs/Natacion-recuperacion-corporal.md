# Recuperación corporal: tres segundos continuos

Se sustituye el bloqueo tras un segundo y el reinicio por cada frame incompleto.
La fuente de Pose conserva la fecha de su última nueva detección corporal válida.
Solo se declara cuerpo perdido tras 3 segundos continuos sin otra detección. Un
nuevo frame con cuerpo reinicia el plazo; imágenes vacías, duplicadas y frames
solo dibujados no lo hacen. La confianza de cada mano/pie sigue siendo necesaria
para puntuar; un cuerpo detectado no convierte un landmark dudoso en contacto.

## Preparación

El primer inicio sigue esperando calibración y encuadre. Una vez iniciados los
cinco segundos, PreparationReady conserva esa preparación durante pérdidas breves
o landmarks parciales, mientras exista una detección corporal en los últimos 3 s.
Se usa tanto dentro de la cuenta como al comprobar su final (individual/circuito).
Así no se corta «cinco» ni se vuelve a lanzar por cada oscilación de IsReady.

Si pasan 3 s sin cuerpo, la preparación vuelve al encuadre una vez y se recupera
la calibración automáticamente al regresar de frente. Después se repiten cinco
segundos completos. La calibración manual sigue disponible para mover el teléfono.

## Ejercicio

Se mantienen referencias/objetivos durante pérdidas cortas. Los contactos y sus
relojes solo consumen observaciones nuevas y fiables; el reloj del set puede
pausarse durante datos inválidos. No se puntúan posiciones antiguas durante la
tolerancia. Manos/pies no quedan bloqueados hasta pulsar un botón: tras una pérdida
larga se descarta el objetivo pendiente y se calibra de nuevo sin borrar métricas.
Un salto puntual de la estimación de cadera ya no bloquea permanentemente manos.
Cambios de posición reales siguen suspendiendo el contacto hasta volver o recalibrar.

En giros, una interrupción breve suspende la respuesta sin sumar fallo/interrupción;
al volver se descarta la primera muestra y se exige confirmación nueva. Se excluye
ese intervalo del tiempo de respuesta. Con pérdida larga se registra una sola
interrupción y se recalibra. El aviso tracking_lost también espera el plazo de 3 s
sin cuerpo; no se dispara por una mano momentáneamente oculta.

## Verificación

Añadidos EditMode de umbral exacto de 3 s, reinicio con cada frame, rechazo de
fechas viejas/no finitas, detección intermitente con una sola emisión de cinco,
pérdida prolongada y pausa/reanudación de un giro sin falso fallo. Actualizada la
expectativa de un hueco breve entre capturas del giro.
Revisión estática de los tres controladores y ambos runners. No hay Unity/compilador
C# aquí: aún falta ejecutar EditMode y validar Android.

En el teléfono: oscilar encuadre durante la preparación sin salir 3 s, tapar cámara
2 s y volver, taparla 4 s y volver; comprobar no tartamudeo de cuenta, recuperación
sin botón y estadísticas conservadas. Repetir individual/circuito y descanso.
