using System.Security.Cryptography;
using System.Text;

namespace Shared.Utils.Criptografia
{
    public class Criptografia
    {
        #region Public
        public static string Encriptar(string str)
        {
            string senha = "";
                senha = Criptografa(str);

            return senha;
        }

        public static string Desencripitar(string str)
        {
            return Descriptografa(str);
        }
        #endregion

        #region Private
        private static string Criptografa_Permanentemente(string senha)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(senha);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
        private static string Criptografa(string senha)
        {
            byte[] textoBytes = Encoding.UTF8.GetBytes(senha);
            return Convert.ToBase64String(textoBytes);
        }

        private static string Descriptografa(string senha)
        {
            byte[] textoBytes = Convert.FromBase64String(senha);
            return Encoding.UTF8.GetString(textoBytes);
        }
        #endregion
    }

    public static class CriptografiaUtils
    {
        #region Hash de Senha (Unidirecional)

        public static string GerarHashSenha(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha))
                throw new ArgumentException("Senha não pode ser vazia", nameof(senha));

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(senha);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool ValidarSenha(string senha, string hashArmazenado)
        {
            if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hashArmazenado))
                return false;

            var hashSenha = GerarHashSenha(senha);
            return hashSenha == hashArmazenado;
        }

        #endregion

        #region Geração de Tokens

        public static string GerarTokenAleatorio(int tamanho = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[tamanho];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string GerarTokenNumerico(int digitos = 6)
        {
            if (digitos < 4 || digitos > 10)
                throw new ArgumentException("Número de dígitos deve estar entre 4 e 10", nameof(digitos));

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var numero = BitConverter.ToUInt32(bytes, 0);

            var min = (int)Math.Pow(10, digitos - 1);
            var max = (int)Math.Pow(10, digitos) - 1;

            return ((numero % (max - min + 1)) + min).ToString();
        }

        #endregion
    }
}
