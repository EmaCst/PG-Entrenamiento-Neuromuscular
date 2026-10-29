using System.Text;

public static class PraxenPasswordPolicy
{
    // Keep aligned with API-Praxen validateNewPassword and change-password.
    public static string Error(string current,string next,string confirmation)
    {
        if(string.IsNullOrEmpty(current))return "Escribe la contraseña temporal que te entregó tu entrenador.";
        if(string.IsNullOrEmpty(next) || next.Length<8 || Encoding.UTF8.GetByteCount(next)>72)
            return "Usa al menos 8 caracteres y como máximo 72 bytes para la contraseña nueva.";
        if(next==current || next=="praxen01")return "Elige una contraseña diferente de la temporal.";
        if(next!=confirmation)return "Las contraseñas nuevas no coinciden.";
        return null;
    }
}
