# Correr: contorno caminando, sin toques

## Flujo

Se completa también la conexión trackingStateInput del TrackedPoseDriver cuando estaba vacía, manteniendo los controles de posición y rotación existentes.

1. Esperar seguimiento AR estable durante un segundo. Detectar suelo no es obligatorio.
2. Crear un ancla de AR de la sesión actual. El suelo detectado cerca ayuda a estimar altura; de lo contrario se usa altura del visor estimada en 1,6 m (ajustable en RunningAreaCalibrator).
3. Reproducir el audio existente de caminar para delimitar. Muestrear el recorrido cada 20 cm y dibujar una línea y pared translúcida de 65 cm de altura, como hijos del ancla.
4. Volver a menos de 60 cm del inicio tras recorrer al menos 6 m. El contorno debe tener al menos 4 m², no cruzarse y admitir objetivos con margen interior de 50 cm. Un contorno inválido se reinicia tras mostrar el motivo durante tres segundos.
5. Mirar el botón CONFIRMAR ÁREA durante dos segundos continuos. Se confirma y comienza la preparación habitual de cinco segundos; no se usan toques ni ratón.
6. Los objetivos se colocan dentro del polígono, incluyendo formas cóncavas, y se vinculan al mismo ancla. La pérdida de seguimiento impide registrar llegadas y distancia; los saltos durante delimitación pausan la captura y piden volver al último punto, conservando el recorrido.

Los datos quedan en memoria y desaparecen al salir de la escena. Cada entrada a correr delimita de nuevo (también cada etapa de correr del circuito actual, que recarga las escenas). No hay persistencia entre sesiones. No se modificó el contrato de resultados de la API.

## Validación Android pendiente

- Piso que no produce planos: con SessionTracking y servicio de anclas activo se puede delimitar igual.
- Caminar un rectángulo de unos 3 × 3 m: pared sigue el recorrido y no sigue la cabeza al girar.
- Volver al inicio: mirar confirmación, mirar fuera reinicia el tiempo de mirada; dos segundos continuos confirman.
- Caminar un contorno cruzado/estrecho: debe pedir repetir en lugar de generar objetivos fuera.
- Perder seguimiento: no añadir puntos ni contabilizar llegadas; si se caminó durante la pérdida, volver al extremo de la línea y continuar.
- Objetivos y barrera permanecen juntos ante ajustes del ancla.
- Sets, descansos, resultados y voz siguen funcionando. Salir y entrar descarta el mapa previo.

Se añadieron pruebas de geometría para cuadrados, orientación invertida, concavidad, margen, cruces y áreas estrechas. No se ejecutó Unity ni se generó APK en este entorno.

## Corrección de componentes y espera del ancla

Se sustituyeron el marcador de inicio, el botón de confirmación y los objetivos de correr por mallas sin colliders. Ya no usan CreatePrimitive, que intentaba añadir colliders que pueden faltar por stripping en Android. El texto de confirmación crea explícitamente sus componentes requeridos.

La sesión AR y el ancla ahora se diagnostican por separado. Si el ancla pierde seguimiento se conserva el recorrido y se espera su recuperación; no se acepta un ancla sin seguimiento para simular una calibración válida. No se pudo confirmar el nombre completo del componente a partir de la captura, ni probar la APK aquí.

## Reanudación sin borrar el área

La captura ya no borra el recorrido por una espera de ocho segundos del ancla ni por saltos o movimiento durante una pérdida de seguimiento. Compara cámara y puntos en coordenadas del mismo ancla. Al recuperar seguimiento exige volver a menos de 45 cm del último punto si hubo discontinuidad; así no dibuja una línea atravesando el área con posiciones desconocidas. Los contornos geométricamente inválidos o un ancla destruida siguen mostrando un motivo específico antes de reiniciar.

Prueba pendiente en Android: caminar parte de un contorno de 3 × 4 m, provocar pérdida de seguimiento y comprobar que la línea permanece. Recuperar seguimiento, volver a su extremo y completar el área. Verificar también que un desplazamiento grande del origen AR no se confunda con movimiento si la posición relativa al ancla no cambia.

## Referencia del mapa

El mapa ahora se coloca bajo TrackablesParent del XROrigin, con posición inicial fija en el espacio de la sesión. No depende de crear ni seguir un ARAnchor individual y nunca es hijo de la cámara. Cámara, línea y objetivos utilizan la misma referencia de la sesión. La detección de planos sigue siendo opcional para estimar altura. AR puede tener deriva; no se ofrece persistencia fuera de la sesión ni se registran muestras si ARSession no tiene seguimiento.

Esta sección reemplaza la dependencia de ARAnchor y su temporizador descrita en versiones previas.
