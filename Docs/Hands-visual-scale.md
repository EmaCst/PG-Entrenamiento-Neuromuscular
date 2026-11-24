# Escala visual del ejercicio de manos

- `MediaPipeStereoBackground.videoScale = 0.82`: muestra el mismo vídeo con un tamaño lineal 18 % menor respecto a las pelotas. Aparecen márgenes; no amplía el campo óptico, no selecciona otra lente y no deforma sólo las landmarks.
- Vídeo, dibujo y rayos de contacto comparten la transformación del plano de cámara para cada ojo. La corrección final del visor sigue aplicándose al conjunto.
- `landmarkContactRadius = 0.02` en lugar de 0.15: reduce la tolerancia invisible. Las 21 landmarks y todas las conexiones siguen participando. No exige cerrar el puño ni usar únicamente el índice.
- Líneas de 1.5 px y puntos de 4 px, con transparencia. El overlay ya no depende de activar el panel de diagnóstico. Mantiene azul izquierda y rojo derecha.
- Escena TelefonoManos y generador del editor configurados explícitamente. No modifica la cámara de pies/correr ni los ajustes globales del visor. No añade modelos, texturas de cámara, readbacks ni pasos de render.

Validación local: revisión de referencias y sintaxis estructural de la escena; comprobación numérica de proyección por ojo con escala, IPD y convergencia. No se compiló Unity ni se probó APK.

En el teléfono verificar: líneas sobre los dedos con ambas manos, correspondencia visual/contacto en ambos ojos, acceso a las nueve pelotas, comodidad del margen y dificultad con mano abierta. Si el tamaño aún no convence, el control `videoScale` permite ajustar entre 0.7 y 1 sin separar la imagen del contacto. Abrir la mano todavía cuenta cuando una parte visible toca realmente una pelota; esta revisión ajusta escala y tolerancia, no cambia esa regla.
