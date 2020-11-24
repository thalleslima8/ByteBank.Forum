using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ByteBank.Forum.App_Start.Identity
{
    public class SenhaValidador : IIdentityValidator<string>
    {
        public int TamanhoRequerido { get; set; }
        public bool ObrigatorioCaracteresLowerCase { get; set; }
        public bool ObrigatorioCaracteresUpperCase { get; set; }
        public bool ObrigatorioDigitos { get; set; }
        


        public async Task<IdentityResult> ValidateAsync(string item)
        {
            var erros = new List<string>();

            if (!VerificaTamanhoRequerido(item))
                erros.Add($"A senha deve conter no mínimo {TamanhoRequerido} caracteres.");

            if(ObrigatorioCaracteresLowerCase && !VerificaCaracteresLowerCase(item))
                erros.Add($"A senha deve conter no mínimo uma letra minúscula.");

            if (ObrigatorioCaracteresUpperCase && !VerificaCaracteresUpperCase(item))
                erros.Add($"A senha deve conter no mínimo uma letra maíuscula.");

            if (ObrigatorioDigitos && !VerificaDigito(item))
                erros.Add($"A senha deve conter no mínimo um dígito.");

            if (erros.Any())
                return IdentityResult.Failed(erros.ToArray());

            return IdentityResult.Success;
        }

        private bool VerificaTamanhoRequerido(string senha) =>
            senha?.Length >= TamanhoRequerido;

        private bool VerificaCaracteresLowerCase(string senha) =>
            senha.Any(char.IsLower);
        
        private bool VerificaCaracteresUpperCase(string senha) =>
            senha.Any(char.IsUpper);

        private bool VerificaDigito(string senha) =>
            senha.Any(char.IsDigit);

    }
}