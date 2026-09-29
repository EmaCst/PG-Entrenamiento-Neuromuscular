# Integracion del ejercicio de manos con MediaPipe

## Escena integrada

La escena `Tests` contiene el ejercicio y la escena `Hand Landmark Detection`
contiene la captura de video. Para unirlas sin perder referencias:

1. Abrir `Hand Landmark Detection`.
2. Abrir `Tests` de forma aditiva (`Open Scene Additive`).
3. Mover desde `Tests` hacia `Hand Landmark Detection` estos objetos raiz:
   - `Exercise`
   - `UI`
   - `SessionManager`
4. Guardar el resultado como `Assets/Scenes/HandExercise.unity`.
5. No copiar las dos camaras, luces ni sistemas de eventos. La escena de
   MediaPipe ya contiene esos objetos.

La camara de `Hand Landmark Detection` ya tiene el componente
`MediaPipeHandInteraction` y su campo `Video Rect` apunta al area donde se
muestra la camara.

## Prueba rapida

1. Ejecutar `HandExercise`.
2. Verificar que MediaPipe dibuje los puntos de una o dos manos.
3. Acercar el dedo indice a la esfera activa.
4. Confirmar que el marcador aumente solamente al usar la mano indicada por
   el color.

Si el dedo se mueve horizontalmente al lado contrario, activar
`Flip Horizontal` en `MediaPipeHandInteraction`. Si la posicion es correcta
pero Unity confunde izquierda y derecha, activar `Swap Hand Labels`.

El componente `MouseInteraction` se mantiene como respaldo: clic izquierdo
simula la mano izquierda y clic derecho simula la derecha.
