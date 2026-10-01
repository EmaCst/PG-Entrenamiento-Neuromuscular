# Primera version estereoscopica para telefono

## Objetivo

La primera version dividio la pantalla del telefono en dos vistas: ojo izquierdo
y ojo derecho. Las dos camaras conservaron la configuracion de la camara
principal y se separaron 0.064 m para producir profundidad estereoscopica dentro
del visor.

## Configuracion automatica

1. Seleccionar `PG RA > Crear todas las escenas para telefono`.
2. Abrir la escena generada `Assets/Scenes/MenuTelefono.unity`.
3. Ejecutar la escena y seleccionar un ejercicio individual o la sesion combinada.
4. Autorizar el uso de la camara cuando el sistema lo solicite.
5. Comprobar que aparezcan dos vistas y una cruz de alineacion en cada mitad.

El generador creo las escenas `TelefonoManos`, `TelefonoPies`,
`TelefonoCorrer`, `SesionCombinada` y `MenuTelefono`. Tambien las agrego a los
perfiles de compilacion en el orden requerido por el menu.

El componente `PhoneStereoRig` creo las camaras `PhoneStereoLeftEye` y
`PhoneStereoRightEye` durante la ejecucion. La camara original permanecio como
referencia para los otros componentes, pero dejo de renderizar directamente.

La escena integrada incorporo el ejecutor `HandLandmarkerRunner` de MediaPipe,
oculto la interfaz de demostracion del paquete y reutilizo la textura de la
camara como fondo. `HandTrackingBridge` recibio los landmarks del modelo y
`MediaPipeHandInteraction` convirtio la punta del dedo indice en rayos de
interaccion con los objetivos.

`TelefonoPies` utilizo una fuente de camara independiente, el modelo ONNX
mediante Sentis y tres zonas de contacto. `TelefonoCorrer` preparo AR Foundation
y ARCore para utilizar la posicion del telefono como referencia espacial dentro
del area calibrada.

## Sesion combinada

La escena `SesionCombinada` presento tres parametros antes de iniciar:

- cantidad de repeticiones del ciclo completo;
- segundos asignados individualmente a manos, pies y correr;
- segundos de descanso entre ejercicios.

Cada repeticion ejecuto, en orden, manos, pies y correr. Al finalizar el tiempo
de un ejercicio se invoco `StopExercise`, se mostro el descanso y se cargo la
siguiente escena. Los controladores individuales se iniciaron automaticamente
cuando la escena se ejecuto fuera de la sesion combinada.

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
2. Activar ARCore en `Project Settings > XR Plug-in Management > Android`.
3. Mantener la orientacion horizontal.
4. Crear una compilacion de desarrollo con `MenuTelefono` como primera escena.
5. Instalarla en el telefono y autorizar el acceso a la camara.
6. Ajustar la distancia interpupilar entre 0.058 y 0.070 m si la imagen se
   percibe doble o incomoda.
7. Desactivar la guia de alineacion despues de validar la posicion de ambas
   vistas.

## Limite de esta primera version

La division estereoscopica cubrio la escena tridimensional. Los elementos de
interfaz configurados como `Screen Space Overlay` continuaron ocupando la
pantalla completa. Para la siguiente version se debieron trasladar los
indicadores importantes a objetos tridimensionales o duplicarlos mediante dos
Canvas configurados como `Screen Space Camera`, uno para cada ojo.
