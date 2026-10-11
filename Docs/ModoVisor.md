# Prueba de presentación para el visor de Praxen

Rama: `feature/visor-calibrable`, creada desde staging. No requiere regenerar escenas: PhoneStereoRig añade el componente de presentación al iniciar.

En el menú principal, abre **Ajustar visor**. Valores iniciales de prueba: tamaño 85 %, separación y altura 0, curvatura 0.10. Se guardan en el teléfono y se comparten entre manos, pies y correr. Desactivar el modo devuelve la presentación original; restablecer recupera esos valores iniciales.

## Calibración en el teléfono

1. Centra el teléfono en el visor. Guarda los ajustes y abre manos.
2. Ajusta el tamaño para ver los objetivos exteriores con comodidad. Ajusta separación y altura para centrar cada vista en su lente.
3. Ajusta la curvatura poco a poco, observando líneas rectas del entorno a través de las lentes. Los valores iniciales son aproximados; no constituyen un perfil óptico medido del visor.
4. Comprueba que las landmarks y sus conexiones siguen sobre la mano, y que tocar una esfera con la mano correcta cuenta una sola vez. Comprueba también las esferas exteriores.
5. Comprueba los indicadores de pies y compara su fluidez con el modo desactivado durante un ejercicio de igual duración.
6. En correr, delimita las cuatro esquinas tocando la imagen visible; tocar los márgenes negros debe ignorarse. Prueba ambos ojos.
7. En un circuito, comprueba los descansos, la cuenta atrás y el botón de regreso al finalizar.

## Implementación y verificación

Después de dibujar cámaras y UI, se copia el fotograma completo a una RenderTexture reutilizada y se aplica una pasada de GPU con escala, desplazamiento y deformación radial por ojo. No se descarga la imagen a CPU ni se guarda una captura en disco. La textura se recrea solo al cambiar la resolución o perderse su recurso.

Los detectores y contactos siguen trabajando sobre sus coordenadas originales. Cámara, objetivos, landmarks y HUD se transforman juntos al presentar el fotograma. Los toques de calibración de correr y los controles IMGUI del circuito usan la misma transformación inversa; los márgenes no activan controles.

La separación de centros es un ajuste de presentación, independiente del IPD virtual de PhoneStereoRig. El tamaño reduce la imagen visible en el visor y no modifica el campo óptico que captura el teléfono. Se conserva la resolución de entrada del detector de pies y la proyección de ARCore.

Marcador del Profiler: `Praxen.Viewer.Present`. La copia y la pasada adicional tienen coste de GPU, que hay que medir en el dispositivo. En el Editor, mantén activa la pestaña Game para que avance WaitForEndOfFrame.

Revisado: fórmula de presentación y de conversión de toques, límites por ojo, reutilización y liberación de recursos, integración desde PhoneStereoRig. Pendiente: compilación en Unity, orientación/color en cada API gráfica, calibración óptica y medición de FPS en Android.
