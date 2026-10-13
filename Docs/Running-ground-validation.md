# Validación de suelo para correr

Se añade la configuración XR de Android, con ARCoreLoader existente, inicio automático y referencia desde EditorBuildSettings. Un preprocesador comprueba antes de compilar Android que el proveedor y el inicio automático estén activos; no reemplaza configuraciones locales existentes.

El calibrador asegura que ARPlaneManager y ARRaycastManager estén habilitados, pide planos horizontales y considera suelo usable solo un plano horizontal hacia arriba con seguimiento. Mantiene el raycast dentro del polígono real: no inventa suelo ni amplía el área para ocultar errores.

La pantalla distingue: proveedor ausente, AR no compatible, sesión sin seguimiento, detector de planos inactivo, cero planos o toque fuera del suelo detectado. Diagnóstico en build de desarrollo: estado AR, loader, frames, planos, detector y motivo de pérdida de seguimiento.

## Comprobación en teléfono

- Abrir correr: debe aparecer ARCoreLoader y después SessionTracking.
- Apuntar al suelo moviendo lentamente el teléfono: planos mayor que cero y Suelo detectado.
- Tocar cuatro esquinas dentro de la superficie reconocida: marcas y límite visible; después instrucciones y contador.
- Un toque fuera del plano debe indicar que ese punto no está dentro del suelo detectado.
- Si sigue bloqueado, capturar el diagnóstico inferior para separar proveedor, seguimiento y detección.

No se ejecutó Unity ni un dispositivo Android en el entorno de edición. La falta de configuración en Git está comprobada; no se asegura que fuera la única causa de la APK anterior.
