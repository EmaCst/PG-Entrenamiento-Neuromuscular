# Praxen — Resultados al terminar el entrenamiento

## Experiencia

Al completar el **último set** de un ejercicio individual o la **última etapa** del circuito, Praxen:

1. Detiene la interacción y captura los resultados que realmente generaron los ejercicios.
2. Los guarda en la cola local de PraxenCloud y solicita la sincronización en segundo plano si el atleta ha iniciado sesión.
3. Muestra **5 segundos de aviso para quitarse los lentes** usando la pantalla binocular existente de `CuentaRegresiva`, oscureciendo la cámara de fondo. La cuenta es real, no depende de `Time.timeScale`, y el audio de números sigue funcionando.
4. Abre `MenuTelefono` **en vista normal de teléfono**, directamente en la nueva página **Resultados**. No se requiere conexión de red, inicio de sesión ni espera del servidor para ver esta sesión.
5. Muestra la duración y una tarjeta por ejercicio que realmente se ejecutó. El único botón principal vuelve al menú de entrenamiento.

### Tarjetas y métricas

- **Manos:** aciertos, precisión calculada a partir de aciertos y fallos, cantidad de fallos y reacción media.
- **Pies:** aciertos, precisión, fallos y reacción media.
- **Correr:** giros completados, número de indicaciones, fallos y respuesta media.
- **Circuito:** aparecen solo las tarjetas de ejercicios ejecutados; los resultados repetidos en varias vueltas se consolidan por tipo.
- Si una métrica no existe (por ejemplo, no hubo aciertos para calcular la reacción), no se inventa un cero: se indica que no hay tiempo registrado.
- La precisión no se interpreta como «mejoría histórica»: esta página solo resume la sesión acabada. Las gráficas históricas se obtendrán de los endpoints `GET /api/resultados/me/*` en un flujo posterior.

La duración es el intervalo real entre `startedAt` y `endedAt`, no la suma de los tiempos configurados, para incluir el tiempo real transcurrido durante la sesión.

### Diseño y componentes

Se aprovecha `TrainingTransitionScreen.Wait(...)` para el aviso de 5 s, sin añadir nuevas escenas de VR ni nuevas cámaras. El resumen se construye a partir del mismo objeto `SessionResultData` que se pasa a `PraxenCloud.Submit`, mediante `PraxenCompletedTraining`. Este objeto solo se comparte en memoria entre el ejercicio y `MenuTelefono`; se consume **una vez** y no se mantiene al regresar al inicio o a una sesión distinta.

Archivos implicados:
- `Assets/Scripts/Managers/StandaloneExerciseStarter.cs`: ejercicio individual.
- `Assets/Scripts/Managers/CombinedExerciseSession.cs`: circuito.
- `Assets/Scripts/Managers/TrainingTransitionScreen.cs`: overlay y cuenta binocular.
- `Assets/Scripts/Managers/PraxenCompletedTraining.cs`: resumen local de la sesión finalizada.
- `Assets/Scripts/UI/PhoneExerciseMenu.cs`: presentación normal de resultados.

No se han alterado ni la API de guardado `POST /api/sesiones` ni las estadísticas originales que Unity ya envía.

## Pruebas manuales necesarias en Unity y en un Android

1. Ejecuta **manos**: 1 set corto. Tras finalizar, comprueba la indicación en **ambos ojos** durante cinco segundos, la transición a una pantalla **única (no estéreo)** y que los aciertos y fallos coincidan con los resultados enviados.
2. Repite con **pies**, incluyendo un entrenamiento sin aciertos. En la tarjeta no debe aparecer un tiempo inventado.
3. Repite con **correr**, verificando giros completados, indicaciones, fallos y respuesta media.
4. Ejecuta un **circuito con tres ejercicios y varias vueltas**. No debe aparecer una página al acabar cada etapa, solo al terminar el circuito completo. Debe mostrar las tres tarjetas con resultados acumulados.
5. Ejecuta un circuito en el que una disciplina tenga duración 0. La tarjeta correspondiente **no debe aparecer**.
6. Completa una sesión **sin conexión a internet**. Los resultados deben aparecer al cabo de cinco segundos; la cola local debe conservarlos para sincronizar más tarde (si ya había iniciado sesión).
7. Comprueba una sesión **de invitado**: debe indicar que sus resultados no están asociados a una cuenta; no afirmará que ya se han sincronizado.
8. Pulsa **Volver al inicio**, inicia otro entrenamiento y comprueba que los datos anteriores no se mezclen ni se presenten de nuevo.
9. Cancela o abandona el entrenamiento **antes de completarlo**: no debe mostrarse la pantalla de resultados finales.
10. Repite en teléfono real con ajustes de visor diferentes para confirmar lectura binocular de la cuenta y pantalla final de una sola imagen.

### Estado de validación

Esta rama incorpora la implementación, pero **no se ha compilado ni probado el APK/Unity Player en este entorno**. Hay que ejecutar la prueba visual y comprobar los números con el dispositivo Android antes de fusionar o distribuir una nueva versión.
