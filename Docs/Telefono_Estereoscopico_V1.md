# Primera version estereoscopica para telefono

## Objetivo

La primera version dividio la pantalla del telefono en dos vistas: ojo izquierdo
y ojo derecho. Las dos camaras conservaron la configuracion de la camara
principal y se separaron 0.064 m para producir profundidad estereoscopica dentro
del visor.

## Configuracion automatica

1. Seleccionar `PG RA > Crear escena integrada MediaPipe + telefono`.
2. Abrir la escena generada `Assets/Scenes/TelefonoMediaPipe.unity`.
3. Ejecutar la escena y autorizar el uso de la camara.
4. Comprobar que el video aparezca detras de los objetivos tridimensionales.
5. Comprobar que aparezcan dos vistas y una cruz de alineacion en cada mitad.

El componente `PhoneStereoRig` creo las camaras `PhoneStereoLeftEye` y
`PhoneStereoRightEye` durante la ejecucion. La camara original permanecio como
referencia para los otros componentes, pero dejo de renderizar directamente.

La escena integrada incorporo el ejecutor `HandLandmarkerRunner` de MediaPipe,
oculto la interfaz de demostracion del paquete y reutilizo la textura de la
camara como fondo. `HandTrackingBridge` recibio los landmarks del modelo y
`MediaPipeHandInteraction` convirtio la punta del dedo indice en rayos de
interaccion con los objetivos.

## Parametros

- `Interpupillary Distance`: separacion entre los ojos. El valor inicial fue
  0.064 m.
- `Center Gap`: espacio central entre ambas vistas.
- `Use Toe In`: giro opcional de las camaras hacia un punto de convergencia.
  Se dejo desactivado para utilizar camaras paralelas.
- `Convergence Distance`: distancia usada solamente cuando `Use Toe In` esta
  activo.
- `Show Alignment Guide`: muestra la division central y dos cruces para revisar
  la alineacion antes de colocar el telefono en el visor.

## Prueba en Android

1. Cambiar la plataforma a Android desde Build Profiles.
2. Mantener la orientacion horizontal.
3. Crear una compilacion de desarrollo.
4. Instalarla en el telefono y colocarlo dentro del visor.
5. Ajustar la distancia interpupilar entre 0.058 y 0.070 m si la imagen se
   percibe doble o incomoda.
6. Desactivar la guia de alineacion despues de validar la posicion de ambas
   vistas.

## Limite de esta primera version

La division estereoscopica cubrio la escena tridimensional. Los elementos de
interfaz configurados como `Screen Space Overlay` continuaron ocupando la
pantalla completa. Para la siguiente version se debieron trasladar los
indicadores importantes a objetos tridimensionales o duplicarlos mediante dos
Canvas configurados como `Screen Space Camera`, uno para cada ojo.
