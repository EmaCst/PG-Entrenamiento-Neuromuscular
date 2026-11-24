# Correr: cambios de dirección por giroscopio

La escena TelefonoCorrer utiliza cámara trasera normal (el mismo fondo de cámara estéreo que pies) y sensores del teléfono. No contiene ARSession, ARCameraManager, XROrigin, calibrador, barrera ni objetivos físicos. No necesita detectar suelo ni delimitar área. Se elimina el comprobador de compilación que obligaba a habilitar ARCore para correr; los paquetes y ajustes XR existentes no se eliminan del proyecto.

## Comportamiento

- Esperar cámara y datos reales del giroscopio; instrucciones duplicadas para el visor.
- Preparación habitual de cinco segundos; mantiene sets, descansos y circuito.
- Indicar aleatoriamente izquierda o derecha. Primer prototipo: 90°, tolerancia 15°, estabilidad 0,15 s. Parámetros ajustables en RunningExerciseManager.
- Cada indicación toma una referencia angular nueva. Integrar muestras del giroscopio con sus marcas de tiempo, proyectando velocidad angular sobre la referencia de gravedad. No se calcula posición ni se usa brújula.
- Completar solo al alcanzar el ángulo en el sentido indicado y mantenerlo estable. Un giro breve atravesando la tolerancia, girar al lado opuesto o quedarse quieto no cuenta.
- El nivel elegido modifica únicamente la ventana de respuesta (8 a 4 s). La separación después de resolver un giro (completado o agotado) se sortea entre 3, 4, 5, 6 y 7 segundos, con un 20 % de probabilidad por valor, independiente de la dificultad. Se permite repetir tiempos consecutivos. Se sortea una sola vez al resolver la indicación; no en cada fotograma. Durante una pérdida de seguimiento se conserva ese valor y se pospone la siguiente indicación hasta recuperar los sensores. Sin configuración del menú, la ventana de respuesta es de 6 s. Antes de emitir otra indicación se esperan además 0,25 s sin giro apreciable de cabeza. La primera indicación de cada set conserva el inicio habitual tras la preparación.
- Si se interrumpen los sensores o se suspende la app, cancelar esa indicación sin contarla como acierto ni timeout. Reiniciar referencia para el siguiente aviso. Las pausas de set no emiten indicaciones.

## Resultados

Se conserva exerciseType=running_direction_change y se añade measurementMode=gyroscope_turn_response. Métricas: cuesIssued, completedTurns, misses (timeouts), interruptedTurns, averageResponseSeconds y bestResponseSeconds (solo completados). Cada intento registra setIndex, dirección, ángulo pedido y alcanzado, tolerancia, outcome y elapsedSeconds. movementOnsetSeconds es el primer movimiento de al menos 5°; vale -1 si no se detectó. No se envían área, metros ni posiciones inventados.

El tiempo principal va desde emitir la indicación visual hasta confirmar el giro estable. Mide orientación de la cabeza/teléfono; no acredita distancia recorrida ni giro del cuerpo. La API actual guarda métricas JSON y el nuevo formato pasó su validador local sin modificar el backend.

## Audios pendientes

Se reservan Resources/PraxenVoices/turn_left, turn_right y turn_intro. Mientras no existan, la indicación visual funciona sin voz de dirección. No se reproduce el antiguo aviso de correr que menciona un área delimitada ni los audios de suelo/recorrido. Continúan música, contador, descansos y final de entrenamiento.

## Validación

- En Android confirmar imagen y contador sin pasos de suelo/área.
- Girar físicamente a derecha e izquierda según la indicación y comprobar que progreso aumenta hacia 90°. Si el dispositivo invierte el signo, ajustar invertYaw en RunningGyroSource y repetir ambas direcciones.
- Verificar que el movimiento contrario y quedarse quieto no cuentan; girar y mantener la orientación sí cuenta una vez.
- Probar dos sets con descanso y un circuito; no debe haber indicaciones durante preparación/descanso, ni duplicar voces entre ojos.
- Salir temporalmente de la app y regresar: intento interrumpido y referencia angular nueva.
- Comprobar resultados de giros en Render y que no contienen métricas de distancia del ejercicio anterior.

Se añadieron ocho pruebas de la lógica de respuesta: giro correcto, dirección opuesta, paso breve por tolerancia, izquierda, paso por 360°, timeout, interrupción y no duplicar completados. Unity no está disponible en el entorno de edición: no se ejecutaron esas pruebas ni se compiló APK; faltan signo del sensor, precisión temporal y comportamiento físico en el teléfono.
