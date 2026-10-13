# Correr: contorno caminando, sin toques

## Flujo

Se completa también la conexión trackingStateInput del TrackedPoseDriver cuando estaba vacía, manteniendo los controles de posición y rotación existentes.

1. Esperar seguimiento AR estable durante un segundo. Detectar suelo no es obligatorio.
2. Crear un ancla de AR de la sesión actual. El suelo detectado cerca ayuda a estimar altura; de lo contrario se usa altura del visor estimada en 1,6 m (ajustable en RunningAreaCalibrator).
3. Reproducir el audio existente de caminar para delimitar. Muestrear el recorrido cada 20 cm y dibujar una línea y pared translúcida de 65 cm de altura, como hijos del ancla.
4. Volver a menos de 60 cm del inicio tras recorrer al menos 6 m. El contorno debe tener al menos 4 m², no cruzarse y admitir objetivos con margen interior de 50 cm. Un contorno inválido se reinicia tras mostrar el motivo durante tres segundos.
5. Mirar el botón CONFIRMAR ÁREA durante dos segundos continuos. Se confirma y comienza la preparación habitual de cinco segundos; no se usan toques ni ratón.
6. Los objetivos se colocan dentro del polígono, incluyendo formas cóncavas, y se vinculan al mismo ancla. La pérdida de seguimiento impide registrar llegadas y distancia; los saltos durante delimitación fuerzan repetir el recorrido.

Los datos quedan en memoria y desaparecen al salir de la escena. Cada entrada a correr delimita de nuevo (también cada etapa de correr del circuito actual, que recarga las escenas). No hay persistencia entre sesiones. No se modificó el contrato de resultados de la API.

## Validación Android pendiente

- Piso que no produce planos: con SessionTracking y servicio de anclas activo se puede delimitar igual.
- Caminar un rectángulo de unos 3 × 3 m: pared sigue el recorrido y no sigue la cabeza al girar.
- Volver al inicio: mirar confirmación, mirar fuera reinicia el tiempo de mirada; dos segundos continuos confirman.
- Caminar un contorno cruzado/estrecho: debe pedir repetir en lugar de generar objetivos fuera.
- Perder seguimiento: no añadir puntos ni contabilizar llegadas; si se caminó demasiado durante la pérdida, repetir recorrido.
- Objetivos y barrera permanecen juntos ante ajustes del ancla.
- Sets, descansos, resultados y voz siguen funcionando. Salir y entrar descarta el mapa previo.

Se añadieron pruebas de geometría para cuadrados, orientación invertida, concavidad, margen, cruces y áreas estrechas. No se ejecutó Unity ni se generó APK en este entorno.
