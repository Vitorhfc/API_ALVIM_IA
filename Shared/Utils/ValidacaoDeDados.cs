
namespace Shared.Utils
{
    public static class ValidacaoDeDados
    {
        #region Chamadas
        public static bool ValidarCpf(string cpf)
        {
            return IsValidCpf(cpf);
        }

        public static bool ValidarCNPJ(string cnpj)
        {
            return IsValidCNPJ(cnpj);
        }

        public static bool ValidaSenha(string senha)
        {
            return IsValidSenha(senha);
        }

        public static bool ValidarEmail(string email)
        {
            return IsValidEmail(email);
        }

        public static bool ValidarTelefoneCelular(string celular)
        {
            return IsValidCelular(celular);
        }

        public static bool ValidarSenha(string senha)
        {
            return IsValidSenha(senha);
        }

        public static bool ValidarPlanoPagamento(int planoPagamento)
        {
            return IsValidPlanoPagamento(planoPagamento);
        }

        #endregion

        #region Validações
        #region EMAIL
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            try
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region CPF
        private static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return false;

            cpf = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");

            if (cpf.Length != 11) return false;

            if (cpf.All(c => c == cpf[0])) return false;

            var multiplicador1 = new int[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            var multiplicador2 = new int[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCpf += digito;
            soma = 0;

            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            digito += resto.ToString();

            return cpf.EndsWith(digito);
        }
        #endregion

        #region CNPJ 
        private static bool IsValidCNPJ(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            cnpj = cnpj.Replace(".", "").Replace("/", "").Replace("-", "").Replace(" ", "");

            if (cnpj.Length != 14)
                return false;

            if (cnpj.All(c => c == cnpj[0]))
                return false;

            int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            string digito = resto.ToString();
            tempCnpj += digito;
            soma = 0;

            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;
            digito += resto.ToString();

            return cnpj.EndsWith(digito);
        }
        #endregion

        #region NUMERO TELEFONE
        private static bool IsValidCelular(string celular)
        {
            if (string.IsNullOrEmpty(celular)) return false;

            celular = celular.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");

            var celularRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{10,11}$");
            return celularRegex.IsMatch(celular);
        }
        #endregion

        #region SENHA
        private static bool IsValidSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha) || senha.Length < 8) return false;

            bool temMaiuscula = senha.Any(char.IsUpper);
            bool temMinuscula = senha.Any(char.IsLower);
            bool temNumero = senha.Any(char.IsDigit);
            bool temEspecial = senha.Any(c => !char.IsLetterOrDigit(c));

            return temMaiuscula && temMinuscula && temNumero && temEspecial;
        }
        #endregion

        #region PLANO_PAGAMENTO
        private static bool IsValidPlanoPagamento(int planoPagamento)
        {
            return planoPagamento >= 1 && planoPagamento <= 3;
        }
        #endregion
        #endregion
    }
}
