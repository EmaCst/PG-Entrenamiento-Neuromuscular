# Referencia de prueba de 30 cm

Usa la plantilla entregada de cuatro hojas Carta, impresa al 100 %, sin ajustar a página. Une las hojas sin huecos y con cinta por detrás, y recorta después el círculo exterior. Diámetro exterior: 30 cm; borde negro: 12 mm; interior blanco: 27.6 cm. No agregues texto ni dibujos al interior.

El detector usa ahora una cuadrícula de 320×320 del mismo RGB preparado para YOLO y tolera blancos menos iluminados (promedio mínimo 0.55 y canal mínimo 0.40). Conserva los filtros de forma, de borde negro y la exigencia de tres detecciones consistentes. La búsqueda del borde admite la incertidumbre de muestreo de círculos pequeños.

La posición del aro se actualiza al preparar la imagen, antes de esperar a YOLO, para reducir el retraso visible al mover la cámara. Los resultados de pies conservan la marca temporal de la imagen que realmente procesó el modelo. La proyección por ojo y la corrección del visor se mantienen.

Validación offline: la foto original produce una referencia; círculos sintéticos de 24, 36 y 60 píxeles con blanco sombreado y borde negro pasan los criterios; un cuadrado blanco con borde negro se rechaza. Esto valida los criterios de imagen mediante un cálculo equivalente, no una ejecución del código C# en Unity.

Pendiente: compilar y probar en Android, medir FPS y distancia útil desde la posición normal del visor, con ambas extremidades visibles. No se garantiza una distancia en metros. Se conserva el seguimiento visual y la posición temporal durante 0.8 s de ocultación; todavía no es un anclaje 3D.
