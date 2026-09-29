# Ejercicio de correr y cambio de direccion

## Funcionamiento

El fisioterapeuta delimita una zona segura mediante cuatro esquinas, en este orden: suroeste, noroeste, noreste y sureste. El sistema genera objetivos sobre el suelo, conserva un margen de 0.5 m con respecto a los bordes y evita recorridos menores a 1 m o mayores a 3 m cuando el espacio lo permite. La distancia maxima comienza en 1.5 m y aumenta 0.5 m cada cinco objetivos alcanzados hasta llegar a 3 m.

El objetivo se considera alcanzado cuando la posicion horizontal del visor queda a 0.6 m o menos. La altura de la cabeza no interviene. Cada llegada genera un nuevo objetivo y almacena direccion, distancia solicitada, tiempo de respuesta, distancia recorrida y posicion final.

## Montaje rapido en Unity

1. Abra la escena donde se probara el ejercicio.
2. Compruebe que la camara principal tenga la etiqueta `MainCamera`.
3. Agregue un `Collider` al suelo de prueba.
4. Seleccione `PG RA > Crear ejercicio de correr en escena`.
5. Conecte los textos y la flecha opcionales en `RunningExerciseManager`.
6. En `SessionManager`, seleccione `Exercise Type = Running` y asigne `Current Running Exercise` y `Running Stats`.
7. Ejecute la escena y marque las cuatro esquinas sobre el suelo.

Para una prueba inmediata sin marcar esquinas, agregue `RunningExerciseDemoSetup` al mismo objeto, asigne el calibrador y la camara, y active `Calibrate On Start`.

## Integracion posterior con RA

`Tracked Head` acepta cualquier `Transform`. Cuando AR Foundation este configurado, se asignara la camara del `XROrigin`. La logica del ejercicio no depende directamente del SDK y no necesita modificarse para ese cambio.

La calibracion actual usa un rayo contra un suelo con `Collider`. Al conectar AR Foundation, el punto obtenido por `ARRaycastManager` se puede entregar directamente a `RunningAreaCalibrator.SetNextCorner`.
