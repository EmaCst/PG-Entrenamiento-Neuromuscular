# Primera version estereoscopica para telefono

## Objetivo

La primera version dividio la pantalla del telefono en dos vistas: ojo izquierdo
y ojo derecho. Las dos camaras conservaron la configuracion de la camara
principal y se separaron 0.064 m para producir profundidad estereoscopica dentro
del visor.

## Configuracion automatica

1. Abrir la escena que se utilizara en el telefono.
2. Confirmar que la camara principal tenga la etiqueta `MainCamera`.
3. Seleccionar `PG RA > Configurar vista estereoscopica para telefono`.
4. Ejecutar la escena en formato horizontal.
5. Comprobar que aparezcan dos vistas y una cruz de alineacion en cada mitad.

El componente `PhoneStereoRig` creo las camaras `PhoneStereoLeftEye` y
`PhoneStereoRightEye` durante la ejecucion. La camara original permanecio como
referencia para los otros componentes, pero dejo de renderizar directamente.

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
